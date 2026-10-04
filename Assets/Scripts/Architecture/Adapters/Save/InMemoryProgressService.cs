using Spotlight.Domain.Save;

namespace Spotlight.Adapters.Save
{
    /// <summary>
    /// 将永久进度端口映射到内存存档中的永久进度区域。
    /// </summary>
    public sealed class InMemoryProgressService : IProgressService
    {
        private readonly PermanentProgress _progress;

        public InMemoryProgressService(PermanentProgress progress)
        {
            _progress = progress;
        }

        public bool IsDemoUnlocked(string demoId)
        {
            return _progress != null && _progress.IsDemoUnlocked(demoId);
        }

        public bool IsDemoCompleted(string demoId)
        {
            return _progress != null && _progress.IsDemoCompleted(demoId);
        }
        public bool HasStoryFlag(string flagId)
        {
            return _progress != null && _progress.HasStoryFlag(flagId);
        }

        public ProgressResult UnlockDemo(string demoId)
        {
            bool changed = _progress != null && _progress.SetDemoUnlocked(demoId);
            return Update(demoId, changed);
        }

        public ProgressResult CompleteDemo(string demoId)
        {
            bool changed = _progress != null && _progress.SetDemoCompleted(demoId);
            return Update(demoId, changed);
        }

        public ProgressResult SetStoryFlag(string flagId)
        {
            bool changed = _progress != null && _progress.SetStoryFlag(flagId);
            return Update(flagId, changed);
        }

        private ProgressResult Update(string id, bool changed)
        {
            bool succeeded = _progress != null && !string.IsNullOrWhiteSpace(id);
            return new ProgressResult(succeeded, changed);
        }
    }
}
