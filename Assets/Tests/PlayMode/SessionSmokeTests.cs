using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Spotlight.Bootstrap;
using Spotlight.Demos.Demo1;
using Spotlight.Scene;

namespace Spotlight.Tests.PlayMode
{
    public sealed class SessionSmokeTests
    {
        [UnityTest]
        public IEnumerator SampleScene_InitializesRequiredSessionBoundaries()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            while (!loadOperation.isDone)
            {
                yield return null;
            }

            GameObject smokeObject = GameObject.Find("ArchitectureSmoke");
            Assert.That(smokeObject, Is.Not.Null);
            SessionSmokeEntry smokeEntry = smokeObject.GetComponent<SessionSmokeEntry>();
            SessionRoot sessionRoot = smokeObject.GetComponent<SessionRoot>();
            Assert.That(smokeEntry, Is.Not.Null);
            Assert.That(sessionRoot, Is.Not.Null);

            int frameCount = 0;
            while (!smokeEntry.IsCompleted && !smokeEntry.IsFailed && frameCount < 10)
            {
                frameCount++;
                yield return null;
            }

            Assert.That(smokeEntry.IsFailed, Is.False, smokeEntry.FailureMessage);
            Assert.That(smokeEntry.SceneLoaderAvailable, Is.True);
            Assert.That(smokeEntry.SaveSucceeded, Is.True);
            Assert.That(smokeEntry.ProgressSucceeded, Is.True);
            Assert.That(smokeEntry.IsCompleted, Is.True);
            Assert.That(sessionRoot.IsInitialized, Is.True);

            Object.Destroy(smokeObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Demo1_LoadsInitializesAndReturnsToSampleScene()
        {
            AsyncOperation sampleLoad = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            while (!sampleLoad.isDone)
            {
                yield return null;
            }

            SessionRoot sessionRoot = Object.FindFirstObjectByType<SessionRoot>();
            Assert.That(sessionRoot, Is.Not.Null);
            Assert.That(sessionRoot.Initialize(), Is.True);

            SceneLoadResult loadResult = sessionRoot.SceneLoader.LoadScene(SceneCatalog.Demo1);
            Assert.That(loadResult, Is.EqualTo(SceneLoadResult.Started));

            float deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != "demo1"
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("demo1"));

            Demo1SceneEntry sceneEntry = Object.FindFirstObjectByType<Demo1SceneEntry>();
            Assert.That(sceneEntry, Is.Not.Null);

            deadline = Time.realtimeSinceStartup + 10f;
            while (!sceneEntry.IsInitialized && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(sceneEntry.IsInitialized, Is.True);

            Demo1GameplayAssembler gameplayAssembler =
                Object.FindFirstObjectByType<Demo1GameplayAssembler>();
            Assert.That(gameplayAssembler, Is.Not.Null);
            Assert.That(gameplayAssembler.IsInitialized, Is.True);

            deadline = Time.realtimeSinceStartup + 2f;
            while (!sessionRoot.AudioManager.IsMusicPlaying
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(sessionRoot.AudioManager.IsMusicPlaying, Is.True);

            Demo1FlowController flowController = Object.FindFirstObjectByType<Demo1FlowController>();
            Assert.That(flowController, Is.Not.Null);
            Assert.That(flowController.ReturnToSampleScene(), Is.EqualTo(SceneLoadResult.Started));

            deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != "SampleScene"
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("SampleScene"));
            SessionRoot[] roots = Object.FindObjectsByType<SessionRoot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Assert.That(roots, Has.Length.EqualTo(1));
            Assert.That(roots[0], Is.SameAs(sessionRoot));
        }
    }
}
