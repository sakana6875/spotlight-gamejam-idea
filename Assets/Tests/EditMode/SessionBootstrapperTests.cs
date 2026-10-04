using NUnit.Framework;
using Spotlight.Adapters.Session;
using Spotlight.Application.Services;
using Spotlight.Composition;

namespace Spotlight.Tests.EditMode
{
    public sealed class SessionBootstrapperTests
    {
        [Test]
        public void Initialize_CreatesAllGlobalServicePorts()
        {
            SessionBootstrapper bootstrapper = new SessionBootstrapper(
                new DefaultSessionServiceFactory());

            SessionInitializationResult result = bootstrapper.Initialize();

            Assert.That(result.Code, Is.EqualTo(SessionInitializationCode.Success));
            Assert.That(result.Services.EventBus, Is.Not.Null);
            Assert.That(result.Services.SceneFlow, Is.Not.Null);
            Assert.That(result.Services.SaveService, Is.Not.Null);
            Assert.That(result.Services.ProgressService, Is.Not.Null);
            Assert.That(result.Services.AudioService, Is.Not.Null);
            Assert.That(result.Services.InputService, Is.Not.Null);
            Assert.That(result.Services.DialogueService, Is.Not.Null);
        }

        [Test]
        public void Initialize_TwiceReturnsExistingServiceSet()
        {
            SessionBootstrapper bootstrapper = new SessionBootstrapper(
                new DefaultSessionServiceFactory());

            SessionInitializationResult first = bootstrapper.Initialize();
            SessionInitializationResult second = bootstrapper.Initialize();

            Assert.That(second.Code, Is.EqualTo(SessionInitializationCode.AlreadyInitialized));
            Assert.That(second.Services, Is.SameAs(first.Services));
        }

        [Test]
        public void Initialize_WhenFactoryThrowsReturnsFailureReason()
        {
            SessionBootstrapper bootstrapper = new SessionBootstrapper(
                new ThrowingServiceFactory());

            SessionInitializationResult result = bootstrapper.Initialize();

            Assert.That(result.Code, Is.EqualTo(SessionInitializationCode.Failed));
            Assert.That(result.ErrorMessage, Does.Contain("测试服务创建失败"));
            Assert.That(bootstrapper.IsInitialized, Is.False);
        }

        [Test]
        public void EventBus_PublishesAndUnsubscribesTypedHandlers()
        {
            InMemoryEventBus eventBus = new InMemoryEventBus();
            int receivedCount = 0;
            System.Action<TestEvent> handler = _ => receivedCount++;

            eventBus.Subscribe(handler);
            eventBus.Publish(new TestEvent());
            eventBus.Unsubscribe(handler);
            eventBus.Publish(new TestEvent());

            Assert.That(receivedCount, Is.EqualTo(1));
        }

        private sealed class ThrowingServiceFactory : ISessionServiceFactory
        {
            public SessionServices Create()
            {
                throw new System.InvalidOperationException("测试服务创建失败");
            }
        }

        private readonly struct TestEvent
        {
        }
    }
}
