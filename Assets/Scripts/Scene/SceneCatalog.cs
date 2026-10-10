namespace Spotlight.Scene
{
    /// <summary>
    /// 集中维护当前真实场景的稳定 ID 与 Unity 场景名映射。
    /// </summary>
    public sealed class SceneCatalog
    {
        /// <summary>
        /// 启动场景的稳定 ID。
        /// </summary>
        public const string Sample = "sample";

        /// <summary>
        /// 当前 Demo2 基线场景的稳定 ID。
        /// </summary>
        public const string Demo2 = "demo2";

        /// <summary>
        /// 查询稳定 ID 对应的 Build Settings 场景名。
        /// </summary>
        public bool TryGetSceneName(string sceneId, out string sceneName)
        {
            switch (sceneId)
            {
                case Sample:
                    sceneName = "SampleScene";
                    return true;
                case Demo2:
                    sceneName = "demo2";
                    return true;
                default:
                    sceneName = null;
                    return false;
            }
        }
    }
}