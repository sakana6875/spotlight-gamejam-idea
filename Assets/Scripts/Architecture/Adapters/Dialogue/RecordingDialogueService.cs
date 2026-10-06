using Spotlight.Application.Services;
using Spotlight.Application.Services.Dialogue;
using Spotlight.Domain.Story;

namespace Spotlight.Adapters.Dialogue
{
    /// <summary>
    /// 记录对话请求和生命周期，不播放内容、不操作 UI 或场景对象。
    /// </summary>
    public sealed class RecordingDialogueService : IDialogueService
    {
        public bool IsPlaying { get; private set; }
        public bool IsMovementLocked { get; private set; }
        public DialogueRequest LastRequest { get; private set; }
        public DialogueOperation LastOperation { get; private set; }
        public DialogueResult LastResult { get; private set; }

        public DialogueResult StartDialogue(DialogueRequest request)
        {
            LastRequest = request;
            LastOperation = DialogueOperation.Start;
            if (request == null || !request.ContentId.IsValid)
            {
                LastResult = DialogueResult.InvalidContent();
                return LastResult;
            }

            IsPlaying = true;
            IsMovementLocked = request.LocksMovement;
            LastResult = DialogueResult.Started();
            return LastResult;
        }

        public DialogueResult Skip()
        {
            LastOperation = DialogueOperation.Skip;
            if (!IsPlaying)
            {
                LastResult = DialogueResult.NotPlaying();
                return LastResult;
            }

            IsPlaying = false;
            IsMovementLocked = false;
            LastResult = DialogueResult.Skipped();
            return LastResult;
        }

        public DialogueResult Complete()
        {
            LastOperation = DialogueOperation.Complete;
            if (!IsPlaying)
            {
                LastResult = DialogueResult.NotPlaying();
                return LastResult;
            }

            IsPlaying = false;
            IsMovementLocked = false;
            LastResult = DialogueResult.Completed();
            return LastResult;
        }
    }

    /// <summary>
    /// 最近一次记录的对话操作。
    /// </summary>
    public enum DialogueOperation
    {
        None,
        Start,
        Skip,
        Complete
    }
}
