using System;
using UnityEngine;
using Spotlight.Demos.Demo1.Domain;
using Spotlight.Demos.Demo1.Domain.Chart;
using Spotlight.Demos.Demo1.Domain.Rhythm;
using Spotlight.Demos.Demo1.Domain.Score;

namespace Spotlight.Demos.Demo1.Application
{
    /// <summary>
    /// 推进 Demo1 音符、判定、得分和结算状态。
    /// </summary>
    public sealed class DemoOneRunController : IDemoOneRunController
    {
        private const float EndBufferSeconds = 2f;

        private readonly ChartData _chart;
        private readonly RhythmJudgeEngine _judgeEngine;
        private readonly HeightCalculator _heightCalculator;
        private readonly Demo1EventBus _eventBus;
        private readonly DemoOneRunState _state;

        /// <summary>
        /// 使用确定的谱面、判定规则、高度规则和局部事实总线创建单局控制器。
        /// </summary>
        public DemoOneRunController(
            ChartData chart,
            RhythmJudgeEngine judgeEngine,
            HeightCalculator heightCalculator,
            Demo1EventBus eventBus)
        {
            _chart = chart ?? throw new ArgumentNullException(nameof(chart));
            _judgeEngine = judgeEngine ?? throw new ArgumentNullException(nameof(judgeEngine));
            _heightCalculator = heightCalculator ?? throw new ArgumentNullException(nameof(heightCalculator));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _state = new DemoOneRunState(ScoreCalculator.CalcMaxScore(chart.Notes.Length));
        }

        /// <inheritdoc />
        public DemoOneRunState State => _state;

        /// <inheritdoc />
        public void Tick(float songTimeSeconds, bool pulsePressed)
        {
            if (_state.IsEnded)
            {
                return;
            }

            if (pulsePressed && _state.CurrentNoteIndex < _chart.Notes.Length)
            {
                NoteData note = _chart.Notes[_state.CurrentNoteIndex];
                float delta = songTimeSeconds - note.Time;
                if (!_judgeEngine.IsTooEarly(delta))
                {
                    JudgeResult result = _judgeEngine.Judge(delta);
                    ApplyResult(note.Index, result, songTimeSeconds);
                }
            }

            while (_state.CurrentNoteIndex < _chart.Notes.Length)
            {
                NoteData note = _chart.Notes[_state.CurrentNoteIndex];
                if (!_judgeEngine.IsExpired(songTimeSeconds, note.Time))
                {
                    break;
                }

                ApplyResult(note.Index, JudgeResult.Miss, songTimeSeconds);
            }

            if (_state.CurrentNoteIndex >= _chart.Notes.Length
                && songTimeSeconds >= _chart.LastNoteTime + EndBufferSeconds)
            {
                FinishRun();
            }
        }

        /// <inheritdoc />
        public void Restart()
        {
            _state.Reset();
        }

        private void ApplyResult(int noteIndex, JudgeResult result, float songTimeSeconds)
        {
            _state.ApplyJudge(result);
            _state.AdvanceNote();
            _eventBus.Publish(new NoteJudgedEvent(noteIndex, result, songTimeSeconds));
        }

        private void FinishRun()
        {
            _state.MarkEnded();

            int finalHeight = Mathf.RoundToInt(
                _heightCalculator.CalcFinalHeight(
                    _state.Score,
                    _state.MaxScore,
                    _state.GoodCount,
                    _state.MissCount));
            int perfectHeight = Mathf.RoundToInt(_heightCalculator.PerfectHeight);

            _eventBus.Publish(new DemoOneCompletedEvent(
                finalHeight,
                perfectHeight,
                _state.Score,
                _state.MaxScore,
                _state.PerfectCount,
                _state.GoodCount,
                _state.MissCount));
        }
    }
}
