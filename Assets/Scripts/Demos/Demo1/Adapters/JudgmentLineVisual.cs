using System.Collections;
using UnityEngine;
using Spotlight.Demos.Demo1.Application;

namespace Spotlight.Demos.Demo1.Adapters
{
    /// <summary>
    /// 在判定发生后短暂闪烁判定线，不参与判定计算。
    /// </summary>
    public sealed class JudgmentLineVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _lineRenderer;
        [SerializeField] private float _flashSeconds = 0.1f;
        [SerializeField] private Color _flashColor = Color.white;

        private Demo1EventBus _eventBus;
        private Color _baseColor;
        private Coroutine _current;

        /// <summary>
        /// 订阅本局判定事实。
        /// </summary>
        public void Initialize(Demo1EventBus eventBus)
        {
            _eventBus = eventBus;
            if (_lineRenderer != null)
            {
                _baseColor = _lineRenderer.color;
            }

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
            if (_lineRenderer == null)
            {
                return;
            }

            if (_current != null)
            {
                StopCoroutine(_current);
            }

            _current = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            _lineRenderer.color = _flashColor;
            yield return new WaitForSeconds(_flashSeconds);
            _lineRenderer.color = _baseColor;
            _current = null;
        }
    }
}
