using NUnit.Framework;
using Spotlight.Features.DemoOne.Domain;

namespace Spotlight.Tests.EditMode
{
    public class HeightCalculatorTests
    {
        private HeightCalculator _calc;

        [SetUp]
        public void SetUp()
        {
            // 与 spec 锁定的一致：Base=1000, Min=100, Good扣10, Miss扣20
            var config = new HeightRuleConfig(1000f, 100f, 10f, 20f);
            _calc = new HeightCalculator(config);
        }

        [Test]
        public void CalcMaxReachable_AllPerfect_IsBaseHeight()
        {
            Assert.AreEqual(1000f, _calc.CalcMaxReachable(0, 0), 0.001f);
        }

        [Test]
        public void CalcMaxReachable_WithGoodAndMiss_DeductsCorrectly()
        {
            // 5 Good, 2 Miss => 1000 - 50 - 40 = 910
            Assert.AreEqual(910f, _calc.CalcMaxReachable(5, 2), 0.001f);
        }

        [Test]
        public void CalcMaxReachable_ExtremeDeduction_ClampsToMin()
        {
            // 1000 Good => 1000 - 10000 = -9000，应被钳制到 MinHeight
            Assert.AreEqual(100f, _calc.CalcMaxReachable(1000, 0), 0.001f);
        }

        [Test]
        public void CalcFinalHeight_AllPerfect_IsBaseHeight()
        {
            // 10 notes, 全 Perfect => score=1000, maxScore=1000, ratio=1 => BaseHeight
            Assert.AreEqual(1000f, _calc.CalcFinalHeight(1000, 1000, 0, 0), 0.001f);
        }

        [Test]
        public void CalcFinalHeight_AllMiss_IsLerpBetweenMinAndReachable()
        {
            // 10 notes, 全 Miss: score=200, maxScore=1000, good=0, miss=10
            // maxReachable = 1000 - 0 - 200 = 800
            // ratio = 0.2 => Lerp(100, 800, 0.2) = 240
            Assert.AreEqual(240f, _calc.CalcFinalHeight(200, 1000, 0, 10), 0.001f);
        }

        [Test]
        public void CalcFinalHeight_ZeroMaxScore_ReturnsMinHeight()
        {
            Assert.AreEqual(100f, _calc.CalcFinalHeight(0, 0, 0, 0), 0.001f);
        }

        [Test]
        public void PerfectHeight_IsBaseHeight()
        {
            Assert.AreEqual(1000f, _calc.PerfectHeight, 0.001f);
        }
    }
}