using NUnit.Framework;
using Spotlight.Domain.Story;

namespace Spotlight.Tests.EditMode
{
    public sealed class Demo2StoryGateTests
    {

        [Test]
        public void AdvanceGates_WaitForTheMatchingDemo2Fact()
        {
            Assert.That(Demo2StoryGates.TryGetAdvanceGate(Demo2StoryGates.PushNarrationGateId, out bool rememberPush), Is.True);
            Assert.That(rememberPush, Is.True);
            Assert.That(Demo2StoryGates.TryGetAdvanceGate(Demo2StoryGates.BiteNarrationGateId, out bool rememberBite), Is.True);
            Assert.That(rememberBite, Is.False);
            Assert.That(Demo2StoryGates.TryGetAdvanceGate(Demo2StoryGates.BossNarrationGateId, out bool rememberBoss), Is.True);
            Assert.That(rememberBoss, Is.False);
            Assert.That(Demo2StoryGates.TryGetAdvanceGate(Demo2StoryGates.Act1ExitLineId, out _), Is.False);
        }
    }
}
