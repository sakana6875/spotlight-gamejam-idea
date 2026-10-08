using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Spotlight.Bootstrap;

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
    }
}
