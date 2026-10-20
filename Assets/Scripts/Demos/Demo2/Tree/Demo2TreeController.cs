using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 将树生命周期规则适配到 Unity 时间、占位 Sprite 和攻击者对象。
    /// </summary>
    public sealed class Demo2TreeController : MonoBehaviour
    {
        private const int RequiredGrowthFrameCount = 4;

        [Header("视觉")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Collider2D _treeCollider;
        [SerializeField] private Sprite _saplingSprite;
        [SerializeField] private Sprite[] _growthFrames;
        [SerializeField] private Sprite _matureSprite;
        [SerializeField] private float _sproutingDurationSeconds = 0.8f;
        [SerializeField] private float _growthFrameSeconds = 0.25f;
        [SerializeField] private Vector3 _sproutedScale = Vector3.one;
        [SerializeField] private Color _sproutedColor = new Color(0.25f, 0.7f, 0.2f, 1f);
        [SerializeField] private Vector3 _matureScale = new Vector3(1.5f, 1.5f, 1.5f);

        [Header("规则")]
        [SerializeField] private float _biteLimitSeconds = 20f;

        private readonly HashSet<UnityEngine.Object> _activeAttackers = new HashSet<UnityEngine.Object>();
        private readonly List<UnityEngine.Object> _staleAttackers = new List<UnityEngine.Object>();

        private Demo2TreeStateMachine _stateMachine;
        private Coroutine _animationRoutine;
        private bool _isConfigured;
        private bool _hasReportedBiteStarted;

        /// <summary>
        /// 树首次成功进入发芽阶段的事实通知。
        /// </summary>
        public event Action SproutingStarted;

        /// <summary>
        /// 嫩芽出现动画已经完成的事实通知。
        /// </summary>
        public event Action SproutingCompleted;

        /// <summary>
        /// 累计啃食时间首次达到阈值的事实通知。
        /// </summary>
        public event Action AttackLimitReached;

        /// <summary>
        /// 最终成长动画已经完成的事实通知。
        /// </summary>
        public event Action GrowthCompleted;
        /// <summary>
        /// 第一名攻击者开始啃食的事实。重复登记和后续攻击者不会再次发布。
        /// </summary>
        public event Action BiteStarted;


        /// <summary>
        /// 当前树生命周期阶段。
        /// </summary>
        public Demo2TreePhase Phase => _stateMachine?.Phase ?? Demo2TreePhase.Dormant;

        /// <summary>
        /// 当前累计啃食秒数。
        /// </summary>
        public float AccumulatedBiteSeconds => _stateMachine?.AccumulatedBiteSeconds ?? 0f;


        private void Awake()
        {
            if (_spriteRenderer == null || _visualRoot == null || _treeCollider == null ||
                _saplingSprite == null || _matureSprite == null || _growthFrames == null ||
                _growthFrames.Length != RequiredGrowthFrameCount)
            {
                DisableForInvalidConfiguration("必须配置 SpriteRenderer、树苗、成熟树和4帧成长 Sprite。");
                return;
            }

            for (int index = 0; index < _growthFrames.Length; index++)
            {
                if (_growthFrames[index] == null)
                {
                    DisableForInvalidConfiguration("成长 Sprite 不能包含空帧。");
                    return;
                }
            }

            if (!_spriteRenderer.enabled || _treeCollider.enabled)
            {
                DisableForInvalidConfiguration("Dormant 初始状态必须显示 Sprite 且关闭 Collider2D。");
                return;
            }

            if (!IsPositiveFinite(_sproutingDurationSeconds) || !IsPositiveFinite(_growthFrameSeconds) ||
                !IsPositiveFinite(_biteLimitSeconds))
            {
                DisableForInvalidConfiguration("动画时长和啃食阈值必须是有限正数。");
                return;
            }

            _stateMachine = new Demo2TreeStateMachine(_biteLimitSeconds);
            _isConfigured = true;
        }

        private void Update()
        {
            if (!_isConfigured)
            {
                return;
            }

            RemoveDestroyedAttackers();
            Demo2TreeBiteResult biteResult = _stateMachine.AdvanceBiteTime(
                Time.deltaTime,
                _activeAttackers.Count > 0);
            if (biteResult == Demo2TreeBiteResult.LimitReached)
            {
                AttackLimitReached?.Invoke();
            }
        }

        private void OnDisable()
        {
            _activeAttackers.Clear();
            _staleAttackers.Clear();
            if (_animationRoutine != null)
            {
                StopCoroutine(_animationRoutine);
                _animationRoutine = null;
            }
        }

        /// <summary>
        /// 请求播放一次嫩芽出现动画。
        /// </summary>
        public Demo2TreeTransitionResult BeginSprouting()
        {
            if (!_isConfigured)
            {
                return Demo2TreeTransitionResult.InvalidState;
            }

            Demo2TreeTransitionResult result = _stateMachine.BeginSprouting();
            if (result != Demo2TreeTransitionResult.Succeeded)
            {
                return result;
            }

            _spriteRenderer.sprite = _saplingSprite;
            _treeCollider.enabled = true;
            _animationRoutine = StartCoroutine(PlaySprouting());
            SproutingStarted?.Invoke();
            return result;
        }

        /// <summary>
        /// 请求在结算对话期间播放4帧最终成长动画。
        /// </summary>
        public Demo2TreeTransitionResult BeginGrowth()
        {
            if (!_isConfigured)
            {
                return Demo2TreeTransitionResult.InvalidState;
            }

            Demo2TreeTransitionResult result = _stateMachine.BeginGrowth();
            if (result != Demo2TreeTransitionResult.Succeeded)
            {
                return result;
            }

            _animationRoutine = StartCoroutine(PlayGrowth());
            return result;
        }

        /// <summary>
        /// 登记一个已经开始啃食树苗的攻击者；重复登记不会增加计时速度。
        /// </summary>
        public bool RegisterAttacker(UnityEngine.Object attacker)
        {
            if (!_isConfigured || attacker == null || !_activeAttackers.Add(attacker))
            {
                return false;
            }

            if (!_hasReportedBiteStarted)
            {
                _hasReportedBiteStarted = true;
                BiteStarted?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// 解除一个已经停止啃食树苗的攻击者。
        /// </summary>
        public bool UnregisterAttacker(UnityEngine.Object attacker)
        {
            return attacker != null && _activeAttackers.Remove(attacker);
        }

        private IEnumerator PlaySprouting()
        {
            Vector3 targetScale = _sproutedScale;
            Vector3 startScale = _visualRoot.localScale;
            Color startColor = _spriteRenderer.color;
            Color targetColor = _sproutedColor;
            float elapsed = 0f;

            while (elapsed < _sproutingDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / _sproutingDurationSeconds);
                _visualRoot.localScale = Vector3.Lerp(startScale, targetScale, progress);
                _spriteRenderer.color = Color.Lerp(startColor, targetColor, progress);
                yield return null;
            }

            _visualRoot.localScale = targetScale;
            _spriteRenderer.color = targetColor;
            _animationRoutine = null;
            if (_stateMachine.CompleteSprouting() == Demo2TreeTransitionResult.Succeeded)
            {
                SproutingCompleted?.Invoke();
            }
        }

        private IEnumerator PlayGrowth()
        {
            Vector3 startScale = _visualRoot.localScale;
            Vector3 matureScale = _matureScale;

            for (int index = 0; index < _growthFrames.Length; index++)
            {
                _spriteRenderer.sprite = _growthFrames[index];
                float progress = (index + 1f) / _growthFrames.Length;
                _visualRoot.localScale = Vector3.Lerp(startScale, matureScale, progress);
                yield return WaitForUnscaledSeconds(_growthFrameSeconds);
            }

            _spriteRenderer.sprite = _matureSprite;
            _visualRoot.localScale = matureScale;
            _animationRoutine = null;
            if (_stateMachine.CompleteGrowth() == Demo2TreeTransitionResult.Succeeded)
            {
                GrowthCompleted?.Invoke();
            }
        }

        private static IEnumerator WaitForUnscaledSeconds(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void RemoveDestroyedAttackers()
        {
            foreach (UnityEngine.Object attacker in _activeAttackers)
            {
                if (attacker == null)
                {
                    _staleAttackers.Add(attacker);
                }
            }

            for (int index = 0; index < _staleAttackers.Count; index++)
            {
                _activeAttackers.Remove(_staleAttackers[index]);
            }

            _staleAttackers.Clear();
        }

        private void DisableForInvalidConfiguration(string reason)
        {
            Debug.LogError("Demo2TreeController 配置无效：" + reason, this);
            enabled = false;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
