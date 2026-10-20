using System;

namespace Spotlight.Story
{
    /// <summary>
    /// 剧情发起的 Demo 与返回按钮之间的结果通道。不是全局单例，由组合根持有。
    /// </summary>
    public sealed class StoryDemoBridge
    {
        public bool IsWaitingForDemo1 { get; private set; }
        public string ResumeSceneId { get; private set; } = string.Empty;
        public int FinalHeight { get; private set; }

        public event Action<int> Demo1Completed;

        public void BeginDemo1(string resumeSceneId)
        {
            IsWaitingForDemo1 = true;
            ResumeSceneId = resumeSceneId ?? string.Empty;
            FinalHeight = 0;
        }

        public bool TryCompleteDemo1(int finalHeight)
        {
            if (!IsWaitingForDemo1)
            {
                return false;
            }

            IsWaitingForDemo1 = false;
            FinalHeight = finalHeight;
            Demo1Completed?.Invoke(finalHeight);
            return true;
        }

        public void CancelDemo1()
        {
            IsWaitingForDemo1 = false;
        }
    }
}
