using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Spotlight.Demos.Demo1.Application;
using Spotlight.Demos.Demo1.Domain.Rhythm;

namespace Spotlight.Demos.Demo1.Adapters
{
    /// <summary>
    /// 展示 Demo1 HUD、判定反馈和结算，并把返回点击交给场景流程。
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

        private Demo1EventBus _eventBus;
        private DemoOneRunState _state;
        private Action _onReturn;
        private float _popupTimer;

        /// <summary>
        /// 绑定单局事实、可读状态和返回操作。
        /// </summary>
        public void Initialize(Demo1EventBus eventBus, DemoOneRunState state, Action onReturn)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _onReturn = onReturn ?? throw new ArgumentNullException(nameof(onReturn));

            if (_resultPanel != null)
            {
                _resultPanel.SetActive(false);
            }

            if (_judgmentPopupText != null)
            {
                _judgmentPopupText.text = string.Empty;
            }

            if (_returnButton != null)
            {
                _returnButton.onClick.AddListener(OnReturnClicked);
            }

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
            if (_judgmentPopupText == null || _popupTimer <= 0f)
            {
                return;
            }

            _popupTimer -= Time.deltaTime;
            if (_popupTimer <= 0f)
            {
                _judgmentPopupText.text = string.Empty;
            }
        }

        private void OnNoteJudged(NoteJudgedEvent eventData)
        {
            RefreshScore();
            if (_judgmentPopupText != null)
            {
                _judgmentPopupText.text = LabelOf(eventData.Result);
                _popupTimer = _popupSeconds;
            }
        }

        private void OnCompleted(DemoOneCompletedEvent eventData)
        {
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(true);
            }

            if (_finalHeightText != null)
            {
                _finalHeightText.text = $"{eventData.FinalHeight} m";
            }

            if (_perfectHeightText != null)
            {
                _perfectHeightText.text = $"{eventData.PerfectHeight} m";
            }

            if (_resultScoreText != null)
            {
                _resultScoreText.text = $"{eventData.Score} / {eventData.MaxScore}";
            }

            if (_resultCountsText != null)
            {
                _resultCountsText.text =
                    $"Perfect {eventData.PerfectCount}  Good {eventData.GoodCount}  Miss {eventData.MissCount}";
            }
        }

        private void RefreshScore()
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"{_state.Score} / {_state.MaxScore}";
            }
        }

        private void OnReturnClicked()
        {
            _onReturn.Invoke();
        }

        private static string LabelOf(JudgeResult result)
        {
            switch (result)
            {
                case JudgeResult.Perfect:
                    return "PERFECT";
                case JudgeResult.Good:
                    return "GOOD";
                default:
                    return "MISS";
            }
        }
    }
}
