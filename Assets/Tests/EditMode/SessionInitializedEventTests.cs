using NUnit.Framework;
using Spotlight.Adapters.Session;
using Spotlight.Application.Services;

namespace Spotlight.Tests.EditMode
{
    public sealed class SessionInitializedEventTests
    {
        [Test]
        public void PublishAfterUnsubscribe_DoesNotNotifyHandlerAgain()
        {
            InMemoryEventBus eventBus = new InMemoryEventBus();
            int receivedCount = 0;
            System.Action<SessionInitializedEvent> handler = _ => receivedCount++;

            eventBus.Subscribe(handler);
            eventBus.Publish(new SessionInitializedEvent());
            eventBus.Unsubscribe(handler);
            eventBus.Publish(new SessionInitializedEvent());

            Assert.That(receivedCount, Is.EqualTo(1));
        }
    }
}
