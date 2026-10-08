using System.Collections.Generic;
using UnityEngine;
using Spotlight.Bootstrap;
using Spotlight.Domain.Save;

namespace Spotlight.Bootstrap
{
    /// <summary>
    /// 只验证 SampleScene 中组合根和存档边界。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class SessionSmokeEntry : MonoBehaviour
    {
        private SessionRoot _sessionRoot;

        public bool IsCompleted { get; private set; }
        public bool IsFailed { get; private set; }
        public bool SceneLoaderAvailable { get; private set; }
        public bool SaveSucceeded { get; private set; }
        public bool ProgressSucceeded { get; private set; }
        public string FailureMessage { get; private set; }

        private void Awake()
        {
            _sessionRoot = GetComponent<SessionRoot>();
            if (_sessionRoot == null)
            {
                Fail("SessionSmoke 缺少同一 GameObject 上的 SessionRoot；场景：" + gameObject.scene.name);
                return;
            }

            if (!_sessionRoot.Initialize())
            {
                Fail("SessionRoot 初始化失败。");
                return;
            }

            SceneLoaderAvailable = _sessionRoot.SceneLoader != null;
            RunSaveChecks();
            IsCompleted = !IsFailed && SceneLoaderAvailable && SaveSucceeded && ProgressSucceeded;
        }

        private void RunSaveChecks()
        {
            SaveSnapshot snapshot = new SaveSnapshot(
                new CheckpointData("smoke_checkpoint", "SampleScene", "Architecture smoke", 0f, 0f, 0f),
                new Dictionary<string, string>(),
                0L,
                0);
            SaveResult saveResult = _sessionRoot.SaveService.SaveCheckpoint(snapshot);
            if (!saveResult.IsSuccess)
            {
                Fail("存档烟测失败：" + saveResult.Code);
            }
            else
            {
                SaveSucceeded = true;
            }

            ProgressResult progressResult = _sessionRoot.SaveService.UnlockDemo("demo1");
            if (!progressResult.Succeeded)
            {
                Fail("进度烟测失败。");
            }
            else
            {
                ProgressSucceeded = true;
            }
        }

        private void Fail(string message)
        {
            IsFailed = true;
            FailureMessage = message;
            Debug.LogError(message, this);
        }
    }
}
