using NUnit.Framework;
using Spotlight.Features.DemoOne.Domain.Rhythm;

namespace Spotlight.Tests.EditMode
{
    public class RhythmJudgeEngineTests
    {
        private RhythmJudgeEngine _engine;

        [SetUp]
        public void SetUp()
        {
            // ”Î Demo1 ƒ¨»œ≈‰÷√“ª÷¬£∫Perfect °¿60ms£¨Good °¿120ms
            _engine = new RhythmJudgeEngine(new JudgeWindow(0.060f, 0.120f));
        }

        [Test]
        public void Judge_ZeroDelta_IsPerfect()
        {
            Assert.AreEqual(JudgeResult.Perfect, _engine.Judge(0f));
        }

        [Test]
        public void Judge_AtPerfectBoundary_IsPerfect()
        {
            Assert.AreEqual(JudgeResult.Perfect, _engine.Judge(0.060f));
            Assert.AreEqual(JudgeResult.Perfect, _engine.Judge(-0.060f));
        }

        [Test]
        public void Judge_JustOverPerfectBoundary_IsGood()
        {
            Assert.AreEqual(JudgeResult.Good, _engine.Judge(0.0601f));
            Assert.AreEqual(JudgeResult.Good, _engine.Judge(-0.0601f));
        }

        [Test]
        public void Judge_AtGoodBoundary_IsGood()
        {
            Assert.AreEqual(JudgeResult.Good, _engine.Judge(0.120f));
            Assert.AreEqual(JudgeResult.Good, _engine.Judge(-0.120f));
        }

        [Test]
        public void Judge_OverGoodBoundary_IsMiss()
        {
            Assert.AreEqual(JudgeResult.Miss, _engine.Judge(0.1201f));
            Assert.AreEqual(JudgeResult.Miss, _engine.Judge(-0.1201f));
        }

        [Test]
        public void IsExpired_WithinGoodWindow_IsFalse()
        {
            Assert.IsFalse(_engine.IsExpired(songTimeSeconds: 0.100f, noteTimeSeconds: 0f));
        }

        [Test]
        public void IsExpired_JustOverGoodWindow_IsTrue()
        {
            Assert.IsTrue(_engine.IsExpired(songTimeSeconds: 0.1201f, noteTimeSeconds: 0f));
        }

        [Test]
        public void Constructor_GoodSmallerThanPerfect_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => new JudgeWindow(0.2f, 0.1f));
        }
    }
}