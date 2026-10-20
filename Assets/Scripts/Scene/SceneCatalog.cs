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
        /// Demo1 场景的稳定 ID。
        /// </summary>
        public const string Demo1 = "demo1";

        /// <summary>
        /// 当前 Demo2 基线场景的稳定 ID。
        /// </summary>
        public const string Demo2 = "demo2";

        /// <summary>
        /// Demo2 第一幕场景的稳定 ID。由基线侧视清空后加载，不是启动场景。
        /// </summary>
        public const string Demo2Act1 = "demo2_act1";

        /// <summary>
        /// 剧情可返回的 Hub 场景稳定 ID。不是默认启动场景。
        /// </summary>
        public const string Hub = "hub";

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
                case Demo1:
                    sceneName = "demo1";
                    return true;
                case Demo2:
                    sceneName = "demo2";
                    return true;
                case Demo2Act1:
                    sceneName = "demo2_act1";
                    return true;
                case Hub:
                    sceneName = "Hub";
                    return true;
                default:
                    sceneName = null;
                    return false;
            }
        }

        /// <summary>
        /// 由当前 Unity 场景名反查稳定 ID，供剧情记录返回点。
        /// </summary>
        public bool TryGetSceneId(string sceneName, out string sceneId)
        {
            switch (sceneName)
            {
                case "SampleScene":
                    sceneId = Sample;
                    return true;
                case "demo1":
                    sceneId = Demo1;
                    return true;
                case "demo2":
                    sceneId = Demo2;
                    return true;
                case "demo2_act1":
                    sceneId = Demo2Act1;
                    return true;
                case "Hub":
                    sceneId = Hub;
                    return true;
                default:
                    sceneId = null;
                    return false;
            }
        }
    }
}
