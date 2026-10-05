using System;
using Spotlight.Domain.Save;
using Spotlight.Application.Services.Scene;
namespace Spotlight.Application.Services
{
    /// <summary>
    /// 同步发布已发生事实的进程内事件总线。
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 同步通知当前事件类型的订阅者；处理器异常直接传播给调用方。
        /// </summary>
        void Publish<TEvent>(TEvent eventData);

        /// <summary>
        /// 注册事件处理器；同一处理器重复注册不会重复通知。
        /// </summary>
        void Subscribe<TEvent>(Action<TEvent> handler);

        /// <summary>
        /// 解除事件处理器注册；不存在的注册安全返回。
        /// </summary>
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
    /// 提供对话请求边界；文本资产和播放表现不属于本阶段。
    /// </summary>
    public interface IDialogueService
    {
        bool IsPlaying { get; }
        DialogueServiceResult StartDialogue(string contentId);
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

    public enum DialogueServiceResultCode
    {
        Unavailable,
        InvalidContentId
    }

    public sealed class DialogueServiceResult
    {
        private DialogueServiceResult(DialogueServiceResultCode code)
        {
            Code = code;
        }

        public DialogueServiceResultCode Code { get; }
        public bool IsSuccess => false;

        public static DialogueServiceResult Unavailable()
        {
            return new DialogueServiceResult(DialogueServiceResultCode.Unavailable);
        }

        public static DialogueServiceResult InvalidContent()
        {
            return new DialogueServiceResult(DialogueServiceResultCode.InvalidContentId);
        }
    }
}
