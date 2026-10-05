using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spotlight.Adapters.Session;

namespace Spotlight.Tests.EditMode
{
    public sealed class InMemoryEventBusTests
    {
        [Test]
        public void Publish_NotifiesHandlersSynchronouslyInSubscriptionOrder()
        {
            InMemoryEventBus eventBus = new InMemoryEventBus();
            List<int> calls = new List<int>();
            bool isPublishing = false;

            eventBus.Subscribe<SimpleEvent>(_ =>
            {
                Assert.That(isPublishing, Is.True);
                calls.Add(1);
            });
            eventBus.Subscribe<SimpleEvent>(_ => calls.Add(2));

            isPublishing = true;
            eventBus.Publish(new SimpleEvent());
            isPublishing = false;

            Assert.That(calls, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void Publish_DoesNotNotifyHandlersForDifferentEventType()
        {
            InMemoryEventBus eventBus = new InMemoryEventBus();
            int receivedCount = 0;

            eventBus.Subscribe<SimpleEvent>(_ => receivedCount++);
            eventBus.Publish(new OtherEvent());

            Assert.That(receivedCount, Is.EqualTo(0));
        }

        [Test]
        public void Subscribe_SameHandlerTwice_NotifiesOnlyOnce()
        {
            InMemoryEventBus eventBus = new InMemoryEventBus();
            int receivedCount = 0;
            Action<SimpleEvent> handler = _ => receivedCount++;

            eventBus.Subscribe(handler);
            eventBus.Subscribe(handler);
            eventBus.Publish(new SimpleEvent());

            Assert.That(receivedCount, Is.EqualTo(1));
        }

        [Test]
        public void Unsubscribe_StopsFutureNotifications()
        {
            InMemoryEventBus eventBus = new InMemoryEventBus();
            int receivedCount = 0;
            Action<SimpleEvent> handler = _ => receivedCount++;

            eventBus.Subscribe(handler);
            eventBus.Publish(new SimpleEvent());
            eventBus.Unsubscribe(handler);
            eventBus.Publish(new SimpleEvent());

            Assert.That(receivedCount, Is.EqualTo(1));
        }

        [Test]
        public void Publish_HandlerUnsubscribesDuringCallback_UsesStableSnapshot()
        {
            InMemoryEventBus eventBus = new InMemoryEventBus();
            int firstCount = 0;
            int secondCount = 0;
            Action<SimpleEvent> firstHandler = null;
            firstHandler = _ =>
            {
                firstCount++;
                eventBus.Unsubscribe(firstHandler);
            };
            Action<SimpleEvent> secondHandler = _ => secondCount++;

            eventBus.Subscribe(firstHandler);
            eventBus.Subscribe(secondHandler);
            eventBus.Publish(new SimpleEvent());
            eventBus.Publish(new SimpleEvent());

            Assert.That(firstCount, Is.EqualTo(1));
            Assert.That(secondCount, Is.EqualTo(2));
        }

        [Test]
        public void Publish_WhenHandlerThrows_PropagatesAndStopsCurrentPublish()
        {
            InMemoryEventBus eventBus = new InMemoryEventBus();
            int handlerAfterFailureCount = 0;

            eventBus.Subscribe<SimpleEvent>(_ => throw new InvalidOperationException("测试处理器失败"));
            eventBus.Subscribe<SimpleEvent>(_ => handlerAfterFailureCount++);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => eventBus.Publish(new SimpleEvent()));

            Assert.That(exception.Message, Is.EqualTo("测试处理器失败"));
            Assert.That(handlerAfterFailureCount, Is.EqualTo(0));
        }

        [Test]
        public void Subscribe_NullHandler_ThrowsArgumentNullException()
        {
            InMemoryEventBus eventBus = new InMemoryEventBus();
            Action<SimpleEvent> handler = null;

            Assert.Throws<ArgumentNullException>(() => eventBus.Subscribe(handler));
        }

        [Test]
        public void Publish_WithoutSubscribersReturnsNormally()
        {
            InMemoryEventBus eventBus = new InMemoryEventBus();

            Assert.DoesNotThrow(() => eventBus.Publish(new SimpleEvent()));
            eventBus.Unsubscribe<SimpleEvent>(null);
        }

        private readonly struct SimpleEvent
        {
        }

        private readonly struct OtherEvent
        {
        }
    }
}
