using System.Collections;
using UnityEngine;
using Spotlight.Application.Services;
using Spotlight.Features.DemoOne.Application;

namespace Spotlight.Features.DemoOne.Adapters
{
    /// <summary>
    /// 判定线的闪光反馈。订阅 NoteJudgedEvent，判定发生时短暂提高亮度。
    /// 不改变判定线位置，也不参与判定计算。
    /// </summary>
    public sealed class JudgmentLineVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _lineRenderer;
        [SerializeField] private float _flashSeconds = 0.1f;
        [SerializeField] private Color _flashColor = Color.white;

        private IEventBus _eventBus;
        private Color _baseColor;
        private Coroutine _current;

        public void Initialize(IEventBus eventBus)
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

        private void OnNoteJudged(NoteJudgedEvent evt)
        {
            if (_lineRenderer == null) return;
            if (_current != null) StopCoroutine(_current);
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