namespace Spotlight.Application.Services.Dialogue
{
    /// <summary>
    /// 对话生命周期操作的可观察结果。
    /// </summary>
    public sealed class DialogueResult
    {
        private DialogueResult(DialogueResultCode code)
        {
            Code = code;
        }

        public DialogueResultCode Code { get; }
        public bool IsSuccess => Code == DialogueResultCode.Started
            || Code == DialogueResultCode.Skipped
            || Code == DialogueResultCode.Completed;

        public static DialogueResult Started()
        {
            return new DialogueResult(DialogueResultCode.Started);
        }

        public static DialogueResult Skipped()
        {
            return new DialogueResult(DialogueResultCode.Skipped);
        }

        public static DialogueResult Completed()
        {
            return new DialogueResult(DialogueResultCode.Completed);
        }

        public static DialogueResult Unavailable()
        {
            return new DialogueResult(DialogueResultCode.Unavailable);
        }

        public static DialogueResult InvalidContent()
        {
            return new DialogueResult(DialogueResultCode.InvalidContentId);
        }

        public static DialogueResult NotPlaying()
        {
            return new DialogueResult(DialogueResultCode.NotPlaying);
        }
    }

    /// <summary>
    /// 对话生命周期和能力边界的结果代码。
    /// </summary>
    public enum DialogueResultCode
    {
        Started,
        Skipped,
        Completed,
        Unavailable,
        InvalidContentId,
        NotPlaying
    }
}
