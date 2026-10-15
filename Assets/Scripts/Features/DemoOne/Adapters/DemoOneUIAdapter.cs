using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Spotlight.Application.Services;
using Spotlight.Features.DemoOne.Application;
using Spotlight.Features.DemoOne.Domain.Rhythm;

namespace Spotlight.Features.DemoOne.Adapters
{
    /// <summary>
    /// Demo1 的 HUD 与结算面板。
    /// 订阅 NoteJudgedEvent 更新判定弹字；订阅 DemoOneCompletedEvent 展示结算。
    /// 返回按钮通过注入的 Action 回调，不直接依赖 ISceneFlow 具体实现。
    /// </summary>
    public sealed class DemoOneUIAdapter : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _judgmentPopupText;
        [SerializeField] private float _popupSeconds = 0.4f;

        [Header("Result Panel")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TMP_Text _finalHeightText;
        [SerializeField] private TMP_Text _perfectHeightText;
        [SerializeField] private TMP_Text _resultScoreText;
        [SerializeField] private TMP_Text _resultCountsText;
        [SerializeField] private Button _returnButton;

        private IEventBus _eventBus;
        private DemoOneRunState _state;
        private Action _onReturnToHub;
        private float _popupTimer;

        /// <summary>由 Installer 调用。state 用于 HUD 每帧读分。</summary>
        public void Initialize(IEventBus eventBus, DemoOneRunState state, Action onReturnToHub)
        {
            _eventBus = eventBus;
            _state = state;
            _onReturnToHub = onReturnToHub;

            if (_resultPanel != null) _resultPanel.SetActive(false);
            if (_judgmentPopupText != null) _judgmentPopupText.text = string.Empty;
            if (_returnButton != null) _returnButton.onClick.AddListener(OnReturnClicked);

            _eventBus.Subscribe<NoteJudgedEvent>(OnNoteJudged);
            _eventBus.Subscribe<DemoOneCompletedEvent>(OnCompleted);

            RefreshScore();
        }

        private void OnDestroy()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<NoteJudgedEvent>(OnNoteJudged);
                _eventBus.Unsubscribe<DemoOneCompletedEvent>(OnCompleted);
            }
            if (_returnButton != null)
            {
                _returnButton.onClick.RemoveListener(OnReturnClicked);
            }
        }

        private void Update()
        {
            if (_judgmentPopupText == null) return;
            if (_popupTimer <= 0f) return;
            _popupTimer -= Time.deltaTime;
            if (_popupTimer <= 0f)
            {
                _judgmentPopupText.text = string.Empty;
            }
        }

        private void OnNoteJudged(NoteJudgedEvent evt)
        {
            RefreshScore();
            if (_judgmentPopupText != null)
            {
                _judgmentPopupText.text = LabelOf(evt.Result);
                _popupTimer = _popupSeconds;
            }
        }

        private void OnCompleted(DemoOneCompletedEvent evt)
        {
            if (_resultPanel != null) _resultPanel.SetActive(true);
            if (_finalHeightText != null) _finalHeightText.text = $"{evt.FinalHeight} m";
            if (_perfectHeightText != null) _perfectHeightText.text = $"{evt.PerfectHeight} m";
            if (_resultScoreText != null) _resultScoreText.text = $"{evt.Score} / {evt.MaxScore}";
            if (_resultCountsText != null)
            {
                _resultCountsText.text =
                    $"Perfect {evt.PerfectCount}  Good {evt.GoodCount}  Miss {evt.MissCount}";
            }
        }

        private void RefreshScore()
        {
            if (_scoreText != null && _state != null)
            {
                _scoreText.text = $"{_state.Score} / {_state.MaxScore}";
            }
        }

        private void OnReturnClicked()
        {
            _onReturnToHub?.Invoke();
        }

        private static string LabelOf(JudgeResult result)
        {
            switch (result)
            {
                case JudgeResult.Perfect: return "PERFECT";
                case JudgeResult.Good: return "GOOD";
                default: return "MISS";
            }
        }
    }
}