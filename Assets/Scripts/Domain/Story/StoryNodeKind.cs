namespace Spotlight.Domain.Story
{
    /// <summary>
    /// 剧情节点类型。与文案表的标志列对应。
    /// </summary>
    public enum StoryNodeKind
    {
        /// <summary>普通对白。</summary>
        Line = 0,

        /// <summary>需要玩法或 UI 接手的演出。</summary>
        Event = 1,

        /// <summary>选项组中的一个选项。</summary>
        Choice = 2,

        /// <summary>分支或全剧结束。</summary>
        End = 3
    }
}
