using NUnit.Framework;
using Spotlight.Demos.Demo1.Domain.Rhythm;
using Spotlight.Demos.Demo1.Domain.Score;

namespace Spotlight.Tests.EditMode
{
    public class ScoreCalculatorTests
    {
        [Test]
        public void CalcNoteScore_Perfect_Returns100()
        {
            Assert.AreEqual(100, ScoreCalculator.CalcNoteScore(JudgeResult.Perfect));
        }

        [Test]
        public void CalcNoteScore_Good_Returns60()
        {
            Assert.AreEqual(60, ScoreCalculator.CalcNoteScore(JudgeResult.Good));
        }

        [Test]
        public void CalcNoteScore_Miss_Returns20()
        {
            Assert.AreEqual(20, ScoreCalculator.CalcNoteScore(JudgeResult.Miss));
        }

        [Test]
        public void CalcMaxScore_ZeroNotes_ReturnsZero()
        {
            Assert.AreEqual(0, ScoreCalculator.CalcMaxScore(0));
        }

        [Test]
        public void CalcMaxScore_TenNotes_Returns1000()
        {
            Assert.AreEqual(1000, ScoreCalculator.CalcMaxScore(10));
        }
    }
}