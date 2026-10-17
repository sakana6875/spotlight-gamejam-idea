using System;
using UnityEngine;
using Spotlight.Application.Services;
using Spotlight.Demos.Demo1.Domain;
using Spotlight.Demos.Demo1.Domain.Chart;
using Spotlight.Demos.Demo1.Domain.Rhythm;
using Spotlight.Demos.Demo1.Domain.Score;

namespace Spotlight.Demos.Demo1.Application
{
    /// <summary>
    /// Demo1 单局协调器。负责推进 note、判定、累计得分，并在结束时发布结算事件。
    /// 不继承 MonoBehaviour，不依赖具体 Unity 场景对象；所有外部能力通过接口注入。
    /// </summary>
    public sealed class DemoOneRunController : IDemoOneRunController
    {
        /// <summary>音乐结束后等待多久才触发结算。给玩家留出听觉与视觉的收尾缓冲。</summary>
        private const float EndBufferSeconds = 2.0f;

        private readonly ChartData _chart;
        private readonly RhythmJudgeEngine _judgeEngine;
        private readonly HeightCalculator _heightCalculator;
        private readonly IEventBus _eventBus;
        private readonly DemoOneRunState _state;

        public DemoOneRunController(
            ChartData chart,
            RhythmJudgeEngine judgeEngine,
            HeightCalculator heightCalculator,
            IEventBus eventBus)
        {
            _chart = chart ?? throw new ArgumentNullException(nameof(chart));
            _judgeEngine = judgeEngine ?? throw new ArgumentNullException(nameof(judgeEngine));
            _heightCalculator = heightCalculator ?? throw new ArgumentNullException(nameof(heightCalculator));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _state = new DemoOneRunState(ScoreCalculator.CalcMaxScore(chart.Notes.Length));
        }

        public DemoOneRunState State => _state;

        public void Tick(float songTimeSeconds, bool pulsePressed)
        {
            if (_state.IsEnded)
            {
                return;
            }

            // 先处理输入：一次按键最多消耗一个 note。
            // 若输入早于 Good 窗口左边界，视为误触，忽略且不消耗 note。
            if (pulsePressed && _state.CurrentNoteIndex < _chart.Notes.Length)
            {
                var note = _chart.Notes[_state.CurrentNoteIndex];
                float delta = songTimeSeconds - note.Time;
                if (!_judgeEngine.IsTooEarly(delta))
                {
                    var result = _judgeEngine.Judge(delta);
                    ApplyResult(note.Index, result, songTimeSeconds);
                }
            }

            // 再处理超时：一次 Tick 可能连续超时多个 note（例如玩家长时间不按）。
            while (_state.CurrentNoteIndex < _chart.Notes.Length)
            {
                var note = _chart.Notes[_state.CurrentNoteIndex];
                if (!_judgeEngine.IsExpired(songTimeSeconds, note.Time))
                {
                    break;
                }
                ApplyResult(note.Index, JudgeResult.Miss, songTimeSeconds);
            }

            // 结束条件：所有 note 已处理，且已越过最后一个 note 时间 + 缓冲。
            if (_state.CurrentNoteIndex >= _chart.Notes.Length
                && songTimeSeconds >= _chart.LastNoteTime + EndBufferSeconds)
            {
                FinishRun();
            }
        }

        public void Restart()
        {
            _state.Reset();
        }

        /// <summary>累计一次判定结果并发布 NoteJudgedEvent。</summary>
        private void ApplyResult(int noteIndex, JudgeResult result, float songTimeSeconds)
        {
            _state.ApplyJudge(result);
            _state.AdvanceNote();
            _eventBus.Publish(new NoteJudgedEvent(noteIndex, result, songTimeSeconds));
        }

        /// <summary>计算最终高度、标记结束并发布 DemoOneCompletedEvent。</summary>
        private void FinishRun()
        {
            _state.MarkEnded();

            int finalHeight = Mathf.RoundToInt(
                _heightCalculator.CalcFinalHeight(
                    _state.Score, _state.MaxScore, _state.GoodCount, _state.MissCount));
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