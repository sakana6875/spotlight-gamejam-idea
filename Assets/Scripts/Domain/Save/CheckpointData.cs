namespace Spotlight.Domain.Save
{
    /// <summary>
    /// 描述可恢复检查点的稳定数据，不包含场景对象引用。
    /// </summary>
    public sealed class CheckpointData
    {
        public string CheckpointId { get; }
        public string SceneId { get; }
        public string DisplayName { get; }
        public float PlayerPositionX { get; }
        public float PlayerPositionY { get; }
        public float PlayerPositionZ { get; }

        public CheckpointData(
            string checkpointId,
            string sceneId,
            string displayName,
            float playerPositionX,
            float playerPositionY,
            float playerPositionZ)
        {
            CheckpointId = checkpointId;
            SceneId = sceneId;
            DisplayName = displayName;
            PlayerPositionX = playerPositionX;
            PlayerPositionY = playerPositionY;
            PlayerPositionZ = playerPositionZ;
        }

        public CheckpointData Clone()
        {
            return new CheckpointData(
                CheckpointId,
                SceneId,
                DisplayName,
                PlayerPositionX,
                PlayerPositionY,
                PlayerPositionZ);
        }
    }
}
