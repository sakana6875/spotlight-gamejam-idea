using System.Collections.Generic;
using NUnit.Framework;
using Spotlight.Demos.Demo1.Application;
using Spotlight.Demos.Demo1.Domain;
using Spotlight.Demos.Demo1.Domain.Chart;
using Spotlight.Demos.Demo1.Domain.Rhythm;

namespace Spotlight.Tests.EditMode
{
    public sealed class DemoOneRunControllerTests
    {
        private Demo1EventBus _eventBus;
        private List<NoteJudgedEvent> _judgedEvents;
        private DemoOneCompletedEvent? _completedEvent;
        private DemoOneRunController _controller;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new Demo1EventBus();
            _judgedEvents = new List<NoteJudgedEvent>();
            _completedEvent = null;
            _eventBus.Subscribe<NoteJudgedEvent>(_judgedEvents.Add);
            _eventBus.Subscribe<DemoOneCompletedEvent>(eventData => _completedEvent = eventData);
            _controller = MakeController(_eventBus);
        }

        [Test]
        public void Tick_ZeroDelta_ConsumesNoteAsPerfect()
        {
            _controller.Tick(1f, true);

            Assert.AreEqual(1, _judgedEvents.Count);
            Assert.AreEqual(0, _judgedEvents[0].NoteIndex);
            Assert.AreEqual(JudgeResult.Perfect, _judgedEvents[0].Result);
            Assert.AreEqual(1, _controller.State.PerfectCount);
            Assert.AreEqual(100, _controller.State.Score);
            Assert.AreEqual(1, _controller.State.CurrentNoteIndex);
        }

        [Test]
        public void Tick_PerfectThenGood_AggregatesCorrectly()
        {
            _controller.Tick(1f, true);
            _controller.Tick(2.080f, true);

            Assert.AreEqual(1, _controller.State.PerfectCount);
            Assert.AreEqual(1, _controller.State.GoodCount);
            Assert.AreEqual(160, _controller.State.Score);
            Assert.AreEqual(2, _controller.State.CurrentNoteIndex);
        }

        [Test]
        public void Tick_NoPulse_NoteAutoMissesAfterGoodWindow()
        {
            _controller.Tick(1.130f, false);

            Assert.AreEqual(1, _judgedEvents.Count);
            Assert.AreEqual(JudgeResult.Miss, _judgedEvents[0].Result);
            Assert.AreEqual(1, _controller.State.MissCount);
            Assert.AreEqual(20, _controller.State.Score);
        }

        [Test]
        public void Tick_PulseTooEarly_DoesNotConsumeNote()
        {
            _controller.Tick(0.5f, true);

            Assert.AreEqual(0, _judgedEvents.Count);
            Assert.AreEqual(0, _controller.State.CurrentNoteIndex);
            Assert.AreEqual(0, _controller.State.MissCount);
        }

        [Test]
        public void Tick_PulseAfterAllNotesIgnored()
        {
            _controller.Tick(1f, true);
            _controller.Tick(2f, true);
            int beforeCount = _judgedEvents.Count;

            _controller.Tick(2.5f, true);

            Assert.AreEqual(beforeCount, _judgedEvents.Count);
        }

        [Test]
        public void Tick_AllNotesResolved_ThenEndBuffer_TriggersCompleted()
        {
            _controller.Tick(1f, true);
            _controller.Tick(2f, true);

            _controller.Tick(3.99f, false);
            Assert.IsNull(_completedEvent);
            Assert.IsFalse(_controller.State.IsEnded);

            _controller.Tick(4f, false);
            Assert.IsNotNull(_completedEvent);
            Assert.IsTrue(_controller.State.IsEnded);
        }

        [Test]
        public void Tick_AllMiss_FinalHeightIsLerpBetweenMinAndReachable()
        {
            _controller.Tick(1.13f, false);
            _controller.Tick(2.13f, false);
            _controller.Tick(4f, false);

            DemoOneCompletedEvent completed = _completedEvent.Value;
            Assert.AreEqual(40, completed.Score);
            Assert.AreEqual(200, completed.MaxScore);
            Assert.AreEqual(272, completed.FinalHeight);
            Assert.AreEqual(1000, completed.PerfectHeight);
            Assert.AreEqual(2, completed.MissCount);
        }

        [Test]
        public void Tick_AllPerfect_FinalHeightEqualsBaseHeight()
        {
            _controller.Tick(1f, true);
            _controller.Tick(2f, true);
            _controller.Tick(4f, false);

            DemoOneCompletedEvent completed = _completedEvent.Value;
            Assert.AreEqual(1000, completed.FinalHeight);
            Assert.AreEqual(1000, completed.PerfectHeight);
            Assert.AreEqual(2, completed.PerfectCount);
        }

        [Test]
        public void Restart_ResetsState()
        {
            _controller.Tick(1f, true);
            _controller.Tick(2.080f, true);

            _controller.Restart();

            Assert.AreEqual(0, _controller.State.Score);
            Assert.AreEqual(0, _controller.State.PerfectCount);
            Assert.AreEqual(0, _controller.State.GoodCount);
            Assert.AreEqual(0, _controller.State.MissCount);
            Assert.AreEqual(0, _controller.State.CurrentNoteIndex);
            Assert.IsFalse(_controller.State.IsEnded);
            Assert.AreEqual(200, _controller.State.MaxScore);
        }

        [Test]
        public void Tick_AfterEnded_IsNoOp()
        {
            _controller.Tick(1f, true);
            _controller.Tick(2f, true);
            _controller.Tick(4f, false);
            int judgedCount = _judgedEvents.Count;
            DemoOneCompletedEvent? completed = _completedEvent;

            _controller.Tick(5f, true);

            Assert.AreEqual(judgedCount, _judgedEvents.Count);
            Assert.AreEqual(completed, _completedEvent);
        }

        private static DemoOneRunController MakeController(Demo1EventBus eventBus)
        {
            ChartData chart = new ChartData(
                "test_song",
                120f,
                0f,
                new[]
                {
                    new NoteData(1f, 0),
                    new NoteData(2f, 1)
                });
            RhythmJudgeEngine judgeEngine = new RhythmJudgeEngine(new JudgeWindow(0.060f, 0.120f));
            HeightCalculator heightCalculator = new HeightCalculator(
                new HeightRuleConfig(1000f, 100f, 10f, 20f));
            return new DemoOneRunController(chart, judgeEngine, heightCalculator, eventBus);
        }
    }
}
