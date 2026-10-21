using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Spotlight.Scene;

namespace Spotlight.Tests.PlayMode
{
    public sealed class SceneFlowTests
    {
        [Test]
        public void SceneCatalog_MapsCurrentStableSceneIds()
        {
            SceneCatalog catalog = new SceneCatalog();

            Assert.That(catalog.TryGetSceneName(SceneCatalog.Sample, out string sampleSceneName), Is.True);
            Assert.That(sampleSceneName, Is.EqualTo("SampleScene"));
            Assert.That(catalog.TryGetSceneName(SceneCatalog.Demo2, out string demo2SceneName), Is.True);
            Assert.That(demo2SceneName, Is.EqualTo("demo2"));
            Assert.That(catalog.TryGetSceneName(SceneCatalog.Demo2Act1, out string act1SceneName), Is.True);
            Assert.That(act1SceneName, Is.EqualTo("demo2_act1"));
            Assert.That(catalog.TryGetSceneId("demo2_act1", out string act1SceneId), Is.True);
            Assert.That(act1SceneId, Is.EqualTo(SceneCatalog.Demo2Act1));
            Assert.That(catalog.TryGetSceneName(SceneCatalog.Demo2Act2, out string act2SceneName), Is.True);
            Assert.That(act2SceneName, Is.EqualTo("demo2_act2"));
            Assert.That(catalog.TryGetSceneId("demo2_act2", out string act2SceneId), Is.True);
            Assert.That(act2SceneId, Is.EqualTo(SceneCatalog.Demo2Act2));
            Assert.That(catalog.TryGetSceneName(SceneCatalog.Demo3, out string demo3SceneName), Is.True);
            Assert.That(demo3SceneName, Is.EqualTo("Act1"));
            Assert.That(catalog.TryGetSceneId("Act1", out string demo3SceneId), Is.True);
            Assert.That(demo3SceneId, Is.EqualTo(SceneCatalog.Demo3));
        }

        [UnityTest]
        public IEnumerator SceneLoader_ReportsCurrentAndUnknownStableSceneIds()
        {
            if (SceneManager.GetActiveScene().name != "SampleScene")
            {
                AsyncOperation loadOperation = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
                while (!loadOperation.isDone)
                {
                    yield return null;
                }
            }

            SceneLoader sceneLoader = new SceneLoader(new SceneCatalog());
            Assert.That(sceneLoader.LoadScene(SceneCatalog.Sample), Is.EqualTo(SceneLoadResult.AlreadyActive));

            LogAssert.Expect(LogType.Error, "场景加载失败：未登记稳定场景 ID：missing");
            Assert.That(sceneLoader.LoadScene("missing"), Is.EqualTo(SceneLoadResult.UnknownSceneId));
        }
    }
}