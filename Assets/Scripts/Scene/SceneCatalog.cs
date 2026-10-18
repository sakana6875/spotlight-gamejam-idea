namespace Spotlight.Scene
{
    /// <summary>
    /// 维护当前真实场景的稳定 ID 与 Unity 场景名映射。
    /// </summary>
    public sealed class SceneCatalog
    {
        public const string Sample = "sample";
        public const string Demo1 = "demo1";

        /// <summary>
        /// 查询稳定场景 ID 对应的 Build Settings 场景名。
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
                default:
                    sceneName = null;
                    return false;
            }
        }
    }
}
