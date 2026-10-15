using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Spotlight.Application.Services;
using Spotlight.Features.DemoOne.Application;
using Spotlight.Features.DemoOne.Domain;
using Spotlight.Features.DemoOne.Domain.Chart;
using Spotlight.Features.DemoOne.Domain.Rhythm;

namespace Spotlight.Tests.EditMode
{
    public class DemoOneRunControllerTests
    {
        /// <summary>记录所有已发布事件的测试替身。</summary>
        private sealed class FakeEventBus : IEventBus
        {
            public List<object> Published { get; } = new List<object>();

            public void Publish<TEvent>(TEvent eventData) => Published.Add(eventData);
            public void Subscribe<TEvent>(Action<TEvent> handler) { }
            public void Unsubscribe<TEvent>(Action<TEvent> handler) { }
        }

        private FakeEventBus _bus;
        private DemoOneRunController _controller;

        [SetUp]
        public void SetUp()
        {
            _bus = new FakeEventBus();
            _controller = MakeController(_bus);
        }

        /// <summary>测试用谱面：2 个 note，分别在 1.0s 和 2.0s。MaxScore = 200。</summary>
        private static ChartData MakeChart()
        {
            return new ChartData(
                songId: "test_song",
                bpm: 120f,
                offset: 0f,
                notes: new[]
                {
                    new NoteData(1.0f, 0),
                    new NoteData(2.0f, 1)
                });
        }

        private static DemoOneRunController MakeController(IEventBus bus)
        {
            var chart = MakeChart();
            var judgeEngine = new RhythmJudgeEngine(new JudgeWindow(0.060f, 0.120f));
            var heightCalc = new HeightCalculator(new HeightRuleConfig(1000f, 100f, 10f, 20f));
            return new DemoOneRunController(chart, judgeEngine, heightCalc, bus);
        }

        private List<NoteJudgedEvent> JudgedEvents()
        {
            return _bus.Published.OfType<NoteJudgedEvent>().ToList();
        }

        private DemoOneCompletedEvent? CompletedEvent()
        {
            return _bus.Published.OfType<DemoOneCompletedEvent>().Cast<DemoOneCompletedEvent?>().FirstOrDefault();
        }

        [Test]
        public void Tick_ZeroDelta_ConsumesNoteAsPerfect()
        {
            _controller.Tick(1.0f, pulsePressed: true);

            var judged = JudgedEvents();
            Assert.AreEqual(1, judged.Count);
            Assert.AreEqual(0, judged[0].NoteIndex);
            Assert.AreEqual(JudgeResult.Perfect, judged[0].Result);
            Assert.AreEqual(1, _controller.State.PerfectCount);
            Assert.AreEqual(100, _controller.State.Score);
            Assert.AreEqual(1, _controller.State.CurrentNoteIndex);
        }

        [Test]
        public void Tick_PerfectThenGood_AggregatesCorrectly()
        {
            // note0 at 1.0s：delta = 0 → Perfect
            _controller.Tick(1.0f, pulsePressed: true);
            // note1 at 2.0s：delta = 0.080 → Good
            _controller.Tick(2.080f, pulsePressed: true);

            Assert.AreEqual(1, _controller.State.PerfectCount);
            Assert.AreEqual(1, _controller.State.GoodCount);
            Assert.AreEqual(160, _controller.State.Score);
            Assert.AreEqual(2, _controller.State.CurrentNoteIndex);
        }

        [Test]
        public void Tick_NoPulse_NoteAutoMissesAfterGoodWindow()
        {
            // note0 at 1.0s，tick 到 1.13s 超时
            _controller.Tick(1.130f, pulsePressed: false);

            var judged = JudgedEvents();
            Assert.AreEqual(1, judged.Count);
            Assert.AreEqual(JudgeResult.Miss, judged[0].Result);
            Assert.AreEqual(1, _controller.State.MissCount);
            Assert.AreEqual(20, _controller.State.Score);
        }

        [Test]
        public void Tick_PulseTooEarly_DoesNotConsumeNote()
        {
            // note0 at 1.0s，玩家在 0.5s 按：太早，忽略
            _controller.Tick(0.5f, pulsePressed: true);

            Assert.AreEqual(0, JudgedEvents().Count);
            Assert.AreEqual(0, _controller.State.CurrentNoteIndex);
            Assert.AreEqual(0, _controller.State.MissCount);
        }

        [Test]
        public void Tick_PulseAfterAllNotesIgnored()
        {
            // 处理完两个 note
            _controller.Tick(1.0f, pulsePressed: true);
            _controller.Tick(2.0f, pulsePressed: true);

            // 之后再按不应产生新事件
            int beforeCount = _bus.Published.Count;
            _controller.Tick(2.5f, pulsePressed: true);
            Assert.AreEqual(beforeCount, _bus.Published.Count);
        }

        [Test]
        public void Tick_AllNotesResolved_ThenEndBuffer_TriggersCompleted()
        {
            _controller.Tick(1.0f, pulsePressed: true);
            _controller.Tick(2.0f, pulsePressed: true);

            // 最后一个 note 时间 2.0s + 缓冲 2.0s = 4.0s
            _controller.Tick(3.99f, pulsePressed: false);
            Assert.IsNull(CompletedEvent());
            Assert.IsFalse(_controller.State.IsEnded);

            _controller.Tick(4.0f, pulsePressed: false);
            Assert.IsNotNull(CompletedEvent());
            Assert.IsTrue(_controller.State.IsEnded);
        }

        [Test]
        public void Tick_AllMiss_FinalHeightIsLerpBetweenMinAndReachable()
        {
            // 两个 note 都超时 Miss
            _controller.Tick(1.13f, pulsePressed: false);
            _controller.Tick(2.13f, pulsePressed: false);
            _controller.Tick(4.0f, pulsePressed: false);

            var completed = CompletedEvent().Value;
            // Score = 40, MaxScore = 200, ratio = 0.2
            // maxReachable = 1000 - 0 - 40 = 960
            // finalHeight = Lerp(100, 960, 0.2) = 272
            Assert.AreEqual(40, completed.Score);
            Assert.AreEqual(200, completed.MaxScore);
            Assert.AreEqual(272, completed.FinalHeight);
            Assert.AreEqual(1000, completed.PerfectHeight);
            Assert.AreEqual(2, completed.MissCount);
        }

        [Test]
        public void Tick_AllPerfect_FinalHeightEqualsBaseHeight()
        {
            _controller.Tick(1.0f, pulsePressed: true);
            _controller.Tick(2.0f, pulsePressed: true);
            _controller.Tick(4.0f, pulsePressed: false);

            var completed = CompletedEvent().Value;
            Assert.AreEqual(1000, completed.FinalHeight);
            Assert.AreEqual(1000, completed.PerfectHeight);
            Assert.AreEqual(2, completed.PerfectCount);
        }

        [Test]
        public void Restart_ResetsState()
        {
            _controller.Tick(1.0f, pulsePressed: true);
            _controller.Tick(2.080f, pulsePressed: true);
            Assert.AreEqual(160, _controller.State.Score);

            _controller.Restart();

            Assert.AreEqual(0, _controller.State.Score);
            Assert.AreEqual(0, _controller.State.PerfectCount);
            Assert.AreEqual(0, _controller.State.GoodCount);
            Assert.AreEqual(0, _controller.State.MissCount);
            Assert.AreEqual(0, _controller.State.CurrentNoteIndex);
            Assert.IsFalse(_controller.State.IsEnded);
            Assert.AreEqual(200, _controller.State.MaxScore); // MaxScore 保留
        }

        [Test]
        public void Tick_AfterEnded_IsNoOp()
        {
            _controller.Tick(1.0f, pulsePressed: true);
            _controller.Tick(2.0f, pulsePressed: true);
            _controller.Tick(4.0f, pulsePressed: false);
            Assert.IsTrue(_controller.State.IsEnded);

            int beforeCount = _bus.Published.Count;
            _controller.Tick(5.0f, pulsePressed: true);
            Assert.AreEqual(beforeCount, _bus.Published.Count);
        }
    }
}