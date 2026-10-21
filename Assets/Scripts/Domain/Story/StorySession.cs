using System;
using System.Collections.Generic;

namespace Spotlight.Domain.Story
{
    /// <summary>
    /// 剧情播放器当前停在哪一种内容上。
    /// </summary>
    public enum StoryWaitKind
    {
        Idle = 0,
        Line = 1,
        Choices = 2,
        Event = 3,
        Ended = 4,
        Blocked = 5
    }

    /// <summary>
    /// 沿文案跳转推进剧情。不创建 UI，也不加载场景。
    /// </summary>
    public sealed class StorySession
    {
        public const string StartNodeId = "intro_001";
        public const string PlayerNameToken = "xxx";

        private readonly StoryGraph _graph;
        private readonly List<StoryNode> _choices = new List<StoryNode>();

        public StorySession(StoryGraph graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public StoryWaitKind WaitKind { get; private set; } = StoryWaitKind.Idle;
        public StoryNode Current { get; private set; }
        public IReadOnlyList<StoryNode> CurrentChoices => _choices;
        public string PlayerName { get; private set; } = string.Empty;
        public int LastDemo1Height { get; private set; }
        public string BlockReason { get; private set; } = string.Empty;

        public event Action Changed;

        public bool Start(string nodeId)
        {
            return MoveTo(nodeId, "起始节点不存在：" + nodeId);
        }

        public bool Advance()
        {
            if (WaitKind != StoryWaitKind.Line || Current == null)
            {
                return false;
            }

            return Follow(Current.NextId);
        }

        public bool SelectChoice(string nodeId)
        {
            if (WaitKind != StoryWaitKind.Choices)
            {
                return false;
            }

            for (int index = 0; index < _choices.Count; index++)
            {
                if (_choices[index].Id != nodeId)
                {
                    continue;
                }

                return Follow(_choices[index].NextId);
            }

            return false;
        }

        public bool CompleteEvent()
        {
            if (WaitKind != StoryWaitKind.Event || Current == null)
            {
                return false;
            }

            return Follow(Current.NextId);
        }

        public bool FailEvent()
        {
            if (WaitKind != StoryWaitKind.Event || Current == null)
            {
                return false;
            }

            if (Current.FailureNextId.Length == 0)
            {
                Block("事件没有失败跳转：" + Current.Id);
                return false;
            }

            return MoveTo(Current.FailureNextId, "失败跳转不存在：" + Current.FailureNextId);
        }

        public void SetPlayerName(string playerName)
        {
            PlayerName = playerName == null ? string.Empty : playerName.Trim();
            Changed?.Invoke();
        }

        public void SetDemo1Height(int height)
        {
            LastDemo1Height = height;
            Changed?.Invoke();
        }

        public string DisplayText(StoryNode node)
        {
            if (node == null || node.Text.Length == 0 || PlayerName.Length == 0)
            {
                return node == null ? string.Empty : node.Text;
            }

            return node.Text.Replace(PlayerNameToken, PlayerName);
        }

        private bool Follow(string nextId)
        {
            if (string.IsNullOrWhiteSpace(nextId))
            {
                EndBranch();
                return true;
            }

            return MoveTo(nextId, "跳转目标不存在：" + nextId);
        }

        private bool MoveTo(string nodeId, string missingReason)
        {
            if (!_graph.TryGet(nodeId, out StoryNode node))
            {
                Block(missingReason);
                return false;
            }

            Current = node;
            _choices.Clear();
            BlockReason = string.Empty;
            if (node.Kind == StoryNodeKind.End)
            {
                WaitKind = StoryWaitKind.Ended;
            }
            else if (node.Kind == StoryNodeKind.Event)
            {
                WaitKind = StoryWaitKind.Event;
            }
            else if (node.ChoiceGroup.Length > 0)
            {
                IReadOnlyList<StoryNode> group = _graph.GetChoiceGroup(node.ChoiceGroup);
                for (int index = 0; index < group.Count; index++)
                {
                    _choices.Add(group[index]);
                }

                WaitKind = StoryWaitKind.Choices;
            }
            else
            {
                WaitKind = StoryWaitKind.Line;
            }

            Changed?.Invoke();
            return true;
        }

        private void EndBranch()
        {
            WaitKind = StoryWaitKind.Ended;
            _choices.Clear();
            BlockReason = string.Empty;
            Changed?.Invoke();
        }

        private void Block(string reason)
        {
            WaitKind = StoryWaitKind.Blocked;
            BlockReason = reason;
            _choices.Clear();
            Changed?.Invoke();
        }
    }
}
