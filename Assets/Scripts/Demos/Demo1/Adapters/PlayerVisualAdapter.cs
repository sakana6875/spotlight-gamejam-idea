using System.Collections;
using UnityEngine;
using Spotlight.Demos.Demo1.Application;
using Spotlight.Demos.Demo1.Domain.Rhythm;

namespace Spotlight.Demos.Demo1.Adapters
{
    /// <summary>
    /// 根据判定事实播放泉水位移、颜色和粒子反馈，不参与判定计算。
    /// </summary>
    public sealed class PlayerVisualAdapter : MonoBehaviour
    {
        private static readonly Color PerfectColor = new Color(1f, 1f, 0.85f);
        private static readonly Color GoodColor = new Color(0.55f, 0.85f, 1f);
        private static readonly Color MissColor = new Color(1f, 0.5f, 0.5f);

        [SerializeField] private Transform _springBody;
        [SerializeField] private SpriteRenderer _springRenderer;
        [SerializeField] private NoteFeedbackParticlePool _particlePool;
        [SerializeField] private float _riseDistance = 0.5f;
        [SerializeField] private float _horizontalDistance = 0.3f;
        [SerializeField] private float _animationSeconds = 0.25f;

        private Demo1EventBus _eventBus;
        private Coroutine _currentAnimation;

        /// <summary>
        /// 订阅本局判定事实。
        /// </summary>
        public void Initialize(Demo1EventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<NoteJudgedEvent>(OnNoteJudged);
        }

        private void OnDestroy()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<NoteJudgedEvent>(OnNoteJudged);
            }
        }

        private void OnNoteJudged(NoteJudgedEvent eventData)
        {
            if (_springBody == null)
            {
                return;
            }

            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }

            if (_particlePool != null)
            {
                _particlePool.Play(_springBody.position, eventData.Result);
            }

            _currentAnimation = StartCoroutine(AnimateByResult(eventData.Result));
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
                    yield return AnimateSegment(
                        start,
                        start + new Vector3(-_horizontalDistance, 0f, 0f));
                    start = _springBody.localPosition;
                    end = start + new Vector3(0f, _riseDistance, 0f);
                    break;
                default:
                    yield return AnimateSegment(
                        start,
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
            float elapsed = 0f;
            while (elapsed < _animationSeconds)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _animationSeconds);
                _springBody.localPosition = Vector3.Lerp(from, to, progress);
                yield return null;
            }

            _springBody.localPosition = to;
        }

        private static Color GetFlashColor(JudgeResult result)
        {
            switch (result)
            {
                case JudgeResult.Perfect:
                    return PerfectColor;
                case JudgeResult.Good:
                    return GoodColor;
                default:
                    return MissColor;
            }
        }
    }
}
