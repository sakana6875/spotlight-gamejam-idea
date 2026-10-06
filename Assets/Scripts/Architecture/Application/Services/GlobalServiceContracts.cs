using System;
using Spotlight.Domain.Save;
using Spotlight.Domain.Story;
using Spotlight.Application.Services.Dialogue;
using Spotlight.Application.Services.Scene;
namespace Spotlight.Application.Services
{
    /// <summary>
    /// 同步发布已发生事实的进程内事件总线。
    /// </summary>
    public interface IEventBus
    {
        void Publish<TEvent>(TEvent eventData);
        void Subscribe<TEvent>(Action<TEvent> handler);
        void Unsubscribe<TEvent>(Action<TEvent> handler);
    }


    /// <summary>
    /// 提供音频设置和播放边界的端口，不暴露 AudioSource。
    /// </summary>
    public interface IAudioService
    {
        AudioServiceResult SetVolume(string channelId, float volume);
        float GetVolume(string channelId);
    }

    /// <summary>
    /// 提供统一输入动作注册边界；具体 Input Actions 适配后续实现。
    /// </summary>
    public interface IInputService
    {
        bool IsActionRegistered(string actionId);
        InputServiceResult RegisterAction(string actionId);
    }

    /// <summary>
    /// 提供稳定内容 ID 对应的对话生命周期边界；不直接操作 UI 或场景对象。
    /// </summary>
    public interface IDialogueService
    {
        bool IsPlaying { get; }
        bool IsMovementLocked { get; }
        DialogueResult StartDialogue(DialogueRequest request);
        DialogueResult Skip();
        DialogueResult Complete();
    }

    /// <summary>
    /// 汇总组合根向场景入口提供的接口依赖，不持有具体实现类型。
    /// </summary>
    public sealed class SessionServices
    {
        public SessionServices(
            IEventBus eventBus,
            ISceneFlow sceneFlow,
            ISaveService saveService,
            IProgressService progressService,
            IAudioService audioService,
            IInputService inputService,
            IDialogueService dialogueService)
        {
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            SceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            SaveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            ProgressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
            AudioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            InputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
            DialogueService = dialogueService ?? throw new ArgumentNullException(nameof(dialogueService));
        }

        public IEventBus EventBus { get; }
        public ISceneFlow SceneFlow { get; }
        public ISaveService SaveService { get; }
        public IProgressService ProgressService { get; }
        public IAudioService AudioService { get; }
        public IInputService InputService { get; }
        public IDialogueService DialogueService { get; }
    }


    public enum AudioServiceResultCode
    {
        Success,
        InvalidChannel,
        InvalidVolume
    }

    public sealed class AudioServiceResult
    {
        private AudioServiceResult(AudioServiceResultCode code)
        {
            Code = code;
        }

        public AudioServiceResultCode Code { get; }
        public bool IsSuccess => Code == AudioServiceResultCode.Success;

        public static AudioServiceResult Succeeded()
        {
            return new AudioServiceResult(AudioServiceResultCode.Success);
        }

        public static AudioServiceResult Failed(AudioServiceResultCode code)
        {
            return new AudioServiceResult(code);
        }
    }

    public enum InputServiceResultCode
    {
        Success,
        InvalidActionId,
        AlreadyRegistered
    }

    public sealed class InputServiceResult
    {
        private InputServiceResult(InputServiceResultCode code)
        {
            Code = code;
        }

        public InputServiceResultCode Code { get; }
        public bool IsSuccess => Code == InputServiceResultCode.Success;

        public static InputServiceResult Succeeded()
        {
            return new InputServiceResult(InputServiceResultCode.Success);
        }

        public static InputServiceResult Failed(InputServiceResultCode code)
        {
            return new InputServiceResult(code);
        }
    }

}
