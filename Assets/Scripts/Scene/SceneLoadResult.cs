namespace Spotlight.Scene
{
    /// <summary>
    /// 场景加载请求的可观察结果。
    /// </summary>
    public enum SceneLoadResult
    {
        Started,
        AlreadyActive,
        UnknownSceneId,
        FailedToStart
    }
}
