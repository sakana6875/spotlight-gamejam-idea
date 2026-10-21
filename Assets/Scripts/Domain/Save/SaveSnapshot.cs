using System.Collections.Generic;

namespace Spotlight.Domain.Save
{
    /// <summary>
    /// 描述单个检查点的临时可重建状态。
    /// </summary>
    public sealed class SaveSnapshot
    {
        public string CheckpointId => Checkpoint.CheckpointId;
        public CheckpointData Checkpoint { get; }
        public IReadOnlyDictionary<string, string> TemporaryState => _temporaryState;
        public long SavedAtUnixSeconds { get; }
        public int PlayTimeSeconds { get; }

        private readonly Dictionary<string, string> _temporaryState;

        public SaveSnapshot(
            CheckpointData checkpoint,
            IReadOnlyDictionary<string, string> temporaryState,
            long savedAtUnixSeconds,
            int playTimeSeconds)
        {
            Checkpoint = checkpoint;
            _temporaryState = temporaryState == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(temporaryState);
            SavedAtUnixSeconds = savedAtUnixSeconds;
            PlayTimeSeconds = playTimeSeconds;
        }

        public SaveSnapshot Clone()
        {
            return new SaveSnapshot(
                Checkpoint.Clone(),
                _temporaryState,
                SavedAtUnixSeconds,
                PlayTimeSeconds);
        }
    }
}
