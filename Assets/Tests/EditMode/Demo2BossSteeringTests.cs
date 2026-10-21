using NUnit.Framework;
using Spotlight.Demos.Demo2;

namespace Spotlight.Tests.EditMode
{
    public sealed class Demo2BossSteeringTests
    {
        [Test]
        public void ClearStep_KeepsBothAxes()
        {
            Demo2BossSteering.ChooseStep(0.4f, -0.2f, false, false, false, out float x, out float y);
            Assert.That(x, Is.EqualTo(0.4f));
            Assert.That(y, Is.EqualTo(-0.2f));
        }

        [Test]
        public void BlockedStep_SlidesOnTheOpenAxis()
        {
            Demo2BossSteering.ChooseStep(0.1f, -0.8f, true, true, false, out float x, out float y);
            Assert.That(x, Is.EqualTo(0f));
            Assert.That(y, Is.EqualTo(-0.8f));
        }

        [Test]
        public void BothAxesBlocked_Stops()
        {
            Demo2BossSteering.ChooseStep(0.3f, 0.3f, true, true, true, out float x, out float y);
            Assert.That(x, Is.EqualTo(0f));
            Assert.That(y, Is.EqualTo(0f));
        }
    }
}
