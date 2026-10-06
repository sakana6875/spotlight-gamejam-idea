using Spotlight.Application.Services;
using Spotlight.Application.Services.Dialogue;
using Spotlight.Domain.Story;

namespace Spotlight.Adapters.Session
{
    /// <summary>
    /// 在内容资产尚未接入前明确报告不可用，不伪装成成功的对话播放服务。
    /// </summary>
    public sealed class UnavailableDialogueService : IDialogueService
    {
        public bool IsPlaying => false;
        public bool IsMovementLocked => false;

        public DialogueResult StartDialogue(DialogueRequest request)
        {
            if (request == null || !request.ContentId.IsValid)
            {
                return DialogueResult.InvalidContent();
            }

            return DialogueResult.Unavailable();
        }

        public DialogueResult Skip()
        {
            return DialogueResult.NotPlaying();
        }

        public DialogueResult Complete()
        {
            return DialogueResult.NotPlaying();
        }
    }
}
