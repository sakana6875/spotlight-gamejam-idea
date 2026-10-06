using System.Collections.Generic;
using UnityEngine;
using Spotlight.Application.Services;
using Spotlight.Application.Services.Dialogue;
using Spotlight.Application.Services.Scene;
using Spotlight.Domain.Save;
using Spotlight.Domain.Story;

namespace Spotlight.Composition
{
    /// <summary>
    /// 只验证 SampleScene 中组合根、服务端口和事件总线的连通性。
    /// 正式游戏逻辑由后续功能实现提供。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class SessionSmokeEntry : MonoBehaviour
    {
        private SessionRoot _sessionRoot;
        private IEventBus _eventBus;
        private bool _isSubscribed;

        public bool IsCompleted { get; private set; }
        public bool IsFailed { get; private set; }
        public bool ReceivedSessionInitializedEvent { get; private set; }
        public bool SceneFlowSucceeded { get; private set; }
        public bool SaveSucceeded { get; private set; }
        public bool ProgressSucceeded { get; private set; }
        public bool AudioSucceeded { get; private set; }
        public bool InputSucceeded { get; private set; }
        public bool DialogueUnavailable { get; private set; }
        public string FailureMessage { get; private set; }

        private void Awake()
        {
            _sessionRoot = GetComponent<SessionRoot>();
            if (_sessionRoot == null)
            {
                Fail("ArchitectureSmoke 缺少同一 GameObject 上的 SessionRoot；场景：" + gameObject.scene.name);
                return;
            }

            SessionInitializationResult initializationResult = _sessionRoot.Initialize();
            if (!initializationResult.IsSuccess)
            {
                Fail("SessionRoot 初始化失败：" + initializationResult.ErrorMessage);
                return;
            }

            SessionServices services = _sessionRoot.Services;
            _eventBus = services.EventBus;
            _eventBus.Subscribe<SessionInitializedEvent>(OnSessionInitialized);
            _isSubscribed = true;
            _eventBus.Publish(new SessionInitializedEvent());

            RunServiceChecks(services);
            IsCompleted = !IsFailed
                && ReceivedSessionInitializedEvent
                && SceneFlowSucceeded
                && SaveSucceeded
                && ProgressSucceeded
                && AudioSucceeded
                && InputSucceeded
                && DialogueUnavailable;
        }

        private void RunServiceChecks(SessionServices services)
        {
            SceneLoadResult sceneResult = services.SceneFlow.LoadScene(SceneId.SampleScene);
            if (!sceneResult.IsSuccess)
            {
                Fail("场景流程烟测失败：" + sceneResult.Code);
            }
            else
            {
                SceneFlowSucceeded = true;
            }

            SaveSnapshot snapshot = new SaveSnapshot(
                new CheckpointData("smoke_checkpoint", "SampleScene", "Architecture smoke", 0f, 0f, 0f),
                new Dictionary<string, string>(),
                0L,
                0);
            SaveResult saveResult = services.SaveService.SaveCheckpoint(snapshot);
            if (!saveResult.IsSuccess)
            {
                Fail("存档烟测失败：" + saveResult.Code);
            }
            else
            {
                SaveSucceeded = true;
            }

            ProgressResult progressResult = services.ProgressService.UnlockDemo("demo1");
            if (!progressResult.Succeeded)
            {
                Fail("进度烟测失败");
            }
            else
            {
                ProgressSucceeded = true;
            }

            AudioServiceResult audioResult = services.AudioService.SetVolume("SFX", 0.5f);
            if (!audioResult.IsSuccess)
            {
                Fail("音频烟测失败：" + audioResult.Code);
            }
            else
            {
                AudioSucceeded = true;
            }

            InputServiceResult inputResult = services.InputService.RegisterAction("Interact");
            if (!inputResult.IsSuccess)
            {
                Fail("输入烟测失败：" + inputResult.Code);
            }
            else
            {
                InputSucceeded = true;
            }

            DialogueResult dialogueResult = services.DialogueService.StartDialogue(
                new DialogueRequest(new ContentId("smoke_dialogue"), false));
            if (dialogueResult.Code != DialogueResultCode.Unavailable)
            {
                Fail("对话烟测未返回预期的 Unavailable：" + dialogueResult.Code);
            }
            else
            {
                DialogueUnavailable = true;
            }
        }

        private void OnSessionInitialized(SessionInitializedEvent eventData)
        {
            ReceivedSessionInitializedEvent = true;
            Debug.Log("ArchitectureSmoke 收到 SessionInitializedEvent。", this);
        }

        private void Fail(string message)
        {
            IsFailed = true;
            FailureMessage = message;
            Debug.LogError(message, this);
        }

        private void OnDestroy()
        {
            if (_isSubscribed && _eventBus != null)
            {
                _eventBus.Unsubscribe<SessionInitializedEvent>(OnSessionInitialized);
                _isSubscribed = false;
            }
        }
    }
}
