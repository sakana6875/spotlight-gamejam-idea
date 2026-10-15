using System.Collections;
using UnityEngine;
using Spotlight.Application.Services;
using Spotlight.Features.DemoOne.Application;
using Spotlight.Features.DemoOne.Domain.Rhythm;

namespace Spotlight.Features.DemoOne.Adapters
{
    /// <summary>
    /// 泉水视觉表现。订阅 NoteJudgedEvent，根据判定结果驱动方块移动、变色和粒子。
    /// 每次判定触发一段短动画：Perfect 直上，Good 左折再上，Miss 右折再上。
    /// 动画时长不影响判定，判定完全由 Controller 的时间轴驱动。
    /// </summary>
    public sealed class PlayerVisualAdapter : MonoBehaviour
    {
        [SerializeField] private Transform _springBody;
        [SerializeField] private SpriteRenderer _springRenderer;
        [SerializeField] private NoteFeedbackParticlePool _particlePool;
        [SerializeField] private float _riseDistance = 0.5f;
        [SerializeField] private float _horizontalDistance = 0.3f;
        [SerializeField] private float _animationSeconds = 0.25f;

        private IEventBus _eventBus;
        private Vector3 _baseLocalPosition;
        private Coroutine _currentAnimation;

        private static readonly Color PerfectColor = new Color(1f, 1f, 0.85f);
        private static readonly Color GoodColor = new Color(0.55f, 0.85f, 1f);
        private static readonly Color MissColor = new Color(1f, 0.5f, 0.5f);

        /// <summary>由 Installer 调用，注入事件总线并订阅。</summary>
        public void Initialize(IEventBus eventBus)
        {
            _eventBus = eventBus;
            _baseLocalPosition = _springBody.localPosition;
            _eventBus.Subscribe<NoteJudgedEvent>(OnNoteJudged);
        }

        private void OnDestroy()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<NoteJudgedEvent>(OnNoteJudged);
            }
        }

        private void OnNoteJudged(NoteJudgedEvent evt)
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }

            if (_particlePool != null)
            {
                _particlePool.Play(_springBody.position, evt.Result);
            }

            _currentAnimation = StartCoroutine(AnimateByResult(evt.Result));
        }

        private IEnumerator AnimateByResult(JudgeResult result)
        {
            Vector3 start = _springBody.localPosition;
            Vector3 end;

            switch (result)
            {
                case JudgeResult.Perfect:
                    end = start + new Vector3(0f, _riseDistance, 0f);
                    break;
                case JudgeResult.Good:
                    // 向左折一次，再向上。
                    yield return AnimateSegment(start,
                        start + new Vector3(-_horizontalDistance, 0f, 0f));
                    start = _springBody.localPosition;
                    end = start + new Vector3(0f, _riseDistance, 0f);
                    break;
                default:
                    // Miss：向右折一次，再向上，且高度低于 Perfect。
                    yield return AnimateSegment(start,
                        start + new Vector3(_horizontalDistance, 0f, 0f));
                    start = _springBody.localPosition;
                    end = start + new Vector3(0f, _riseDistance * 0.5f, 0f);
                    break;
            }

            if (_springRenderer != null)
            {
                _springRenderer.color = GetFlashColor(result);
            }

            yield return AnimateSegment(start, end);
            _currentAnimation = null;
        }

        private IEnumerator AnimateSegment(Vector3 from, Vector3 to)
        {
            float t = 0f;
            while (t < _animationSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / _animationSeconds);
                _springBody.localPosition = Vector3.Lerp(from, to, k);
                yield return null;
            }
            _springBody.localPosition = to;
        }

        private static Color GetFlashColor(JudgeResult result)
        {
            switch (result)
            {
                case JudgeResult.Perfect: return PerfectColor;
                case JudgeResult.Good: return GoodColor;
                default: return MissColor;
            }
        }
    }
}