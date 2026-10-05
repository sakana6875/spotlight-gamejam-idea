using System;
using Spotlight.Application.Services;

namespace Spotlight.Adapters.Session
{
    /// <summary>
    /// 在内容资产尚未接入前明确报告不可用，不伪装成成功的对话播放服务。
    /// </summary>
    public sealed class UnavailableDialogueService : IDialogueService
    {
        public bool IsPlaying => false;

        public DialogueServiceResult StartDialogue(string contentId)
        {
            if (string.IsNullOrWhiteSpace(contentId))
            {
                return DialogueServiceResult.InvalidContent();
            }

            return DialogueServiceResult.Unavailable();
        }
    }
}
