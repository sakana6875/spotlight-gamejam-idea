using NUnit.Framework;
using Spotlight.Demos.Demo2;

namespace Spotlight.Tests.EditMode
{
    public sealed class Demo2CameraFrameTests
    {
        [Test]
        public void InsideWindow_StaysPut()
        {
            Demo2CameraFrame.ClampCenter(1f, -1f, -16f, 16f, -9f, 9f, 8f, 5f, out float x, out float y);
            Assert.That(x, Is.EqualTo(1f));
            Assert.That(y, Is.EqualTo(-1f));
        }

        [Test]
        public void PastEdge_KeepsTheViewInside()
        {
            Demo2CameraFrame.ClampCenter(20f, -20f, -16f, 16f, -9f, 9f, 8f, 5f, out float x, out float y);
            Assert.That(x, Is.EqualTo(8f));
            Assert.That(y, Is.EqualTo(-4f));
        }

        [Test]
        public void ViewLargerThanWindow_LocksToCenter()
        {
            float x = Demo2CameraFrame.ClampAxis(4f, -2f, 2f, 5f);
            Assert.That(x, Is.EqualTo(0f));
        }
    }
}
