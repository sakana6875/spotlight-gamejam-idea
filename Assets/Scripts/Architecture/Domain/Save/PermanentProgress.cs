using System.Collections.Generic;

namespace Spotlight.Domain.Save
{
    /// <summary>
    /// 保存跨检查点持续存在的解锁、完成和剧情标记。
    /// </summary>
    public sealed class PermanentProgress
    {
        private readonly HashSet<string> _unlockedDemos;
        private readonly HashSet<string> _completedDemos;
        private readonly HashSet<string> _storyFlags;

        public IReadOnlyCollection<string> UnlockedDemos => _unlockedDemos;
        public IReadOnlyCollection<string> CompletedDemos => _completedDemos;
        public IReadOnlyCollection<string> StoryFlags => _storyFlags;

        public PermanentProgress()
        {
            _unlockedDemos = new HashSet<string>();
            _completedDemos = new HashSet<string>();
            _storyFlags = new HashSet<string>();
        }

        public bool IsDemoUnlocked(string demoId)
        {
            return !string.IsNullOrWhiteSpace(demoId) && _unlockedDemos.Contains(demoId);
        }

        public bool IsDemoCompleted(string demoId)
        {
            return !string.IsNullOrWhiteSpace(demoId) && _completedDemos.Contains(demoId);
        }

        public bool HasStoryFlag(string flagId)
        {
            return !string.IsNullOrWhiteSpace(flagId) && _storyFlags.Contains(flagId);
        }

        public bool SetDemoUnlocked(string demoId)
        {
            return !string.IsNullOrWhiteSpace(demoId) && _unlockedDemos.Add(demoId);
        }

        public bool SetDemoCompleted(string demoId)
        {
            return !string.IsNullOrWhiteSpace(demoId) && _completedDemos.Add(demoId);
        }

        public bool SetStoryFlag(string flagId)
        {
            return !string.IsNullOrWhiteSpace(flagId) && _storyFlags.Add(flagId);
        }

        public void Clear()
        {
            _unlockedDemos.Clear();
            _completedDemos.Clear();
            _storyFlags.Clear();
        }
        public PermanentProgress Clone()
        {
            PermanentProgress clone = new PermanentProgress();
            clone._unlockedDemos.UnionWith(_unlockedDemos);
            clone._completedDemos.UnionWith(_completedDemos);
            clone._storyFlags.UnionWith(_storyFlags);
            return clone;
        }
    }
}
