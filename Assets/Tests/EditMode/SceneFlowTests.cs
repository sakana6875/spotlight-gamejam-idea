using NUnit.Framework;
using Spotlight.Adapters.Scene;
using Spotlight.Application.Services.Scene;

namespace Spotlight.Tests.EditMode
{
    public sealed class SceneFlowTests
    {
        [Test]
        public void LoadScene_WhenSampleSceneIsAvailableReturnsSuccess()
        {
            RecordingSceneFlow sceneFlow = new RecordingSceneFlow(SceneCatalog.CreateDefault());

            SceneLoadResult result = sceneFlow.LoadScene(SceneId.SampleScene);

            Assert.That(result.Code, Is.EqualTo(SceneLoadResultCode.Success));
            Assert.That(sceneFlow.CurrentSceneId, Is.EqualTo(SceneId.SampleScene));
        }

        [Test]
        public void LoadScene_WhenSceneIsNotAvailableReturnsExplicitFailure()
        {
            RecordingSceneFlow sceneFlow = new RecordingSceneFlow(SceneCatalog.CreateDefault());

            SceneLoadResult result = sceneFlow.LoadMenu();

            Assert.That(result.Code, Is.EqualTo(SceneLoadResultCode.NotAvailable));
            Assert.That(sceneFlow.CurrentSceneId, Is.Null);
        }

        [Test]
        public void LoadScene_WhenSceneIsNotRegisteredReturnsExplicitFailure()
        {
            SceneCatalog catalog = new SceneCatalog(new[]
            {
                new SceneDefinition(SceneId.SampleScene, "SampleScene", true)
            });
            RecordingSceneFlow sceneFlow = new RecordingSceneFlow(catalog);

            SceneLoadResult result = sceneFlow.LoadScene(SceneId.Menu);

            Assert.That(result.Code, Is.EqualTo(SceneLoadResultCode.UnknownScene));
        }

        [Test]
        public void EnterDemoRecordsDemoAndEntryMode()
        {
            RecordingSceneFlow sceneFlow = new RecordingSceneFlow(CreateAvailableCatalog());

            SceneLoadResult result = sceneFlow.EnterDemo(DemoId.Demo2, DemoEntryMode.Continue);

            Assert.That(result.Code, Is.EqualTo(SceneLoadResultCode.Success));
            Assert.That(result.DemoId, Is.EqualTo(DemoId.Demo2));
            Assert.That(sceneFlow.LastDemoId, Is.EqualTo(DemoId.Demo2));
            Assert.That(sceneFlow.LastEntryMode, Is.EqualTo(DemoEntryMode.Continue));
        }

        [Test]
        public void RestartDemoUsesReplayMode()
        {
            RecordingSceneFlow sceneFlow = new RecordingSceneFlow(CreateAvailableCatalog());

            SceneLoadResult result = sceneFlow.RestartDemo(DemoId.Demo1);

            Assert.That(result.Code, Is.EqualTo(SceneLoadResultCode.Success));
            Assert.That(sceneFlow.LastEntryMode, Is.EqualTo(DemoEntryMode.Replay));
        }

        [Test]
        public void LoadScene_WhenAlreadyActiveReturnsExplicitFailure()
        {
            RecordingSceneFlow sceneFlow = new RecordingSceneFlow(SceneCatalog.CreateDefault());
            sceneFlow.LoadScene(SceneId.SampleScene);

            SceneLoadResult result = sceneFlow.LoadScene(SceneId.SampleScene);

            Assert.That(result.Code, Is.EqualTo(SceneLoadResultCode.AlreadyActive));
        }

        private static SceneCatalog CreateAvailableCatalog()
        {
            return new SceneCatalog(new[]
            {
                new SceneDefinition(SceneId.SampleScene, "SampleScene", true),
                new SceneDefinition(SceneId.Demo1, "Demo1", true),
                new SceneDefinition(SceneId.Demo2, "Demo2", true),
                new SceneDefinition(SceneId.Demo3, "Demo3", true),
                new SceneDefinition(SceneId.ChaosDemo, "ChaosDemo", true)
            });
        }
    }
}
