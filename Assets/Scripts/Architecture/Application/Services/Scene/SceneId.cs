namespace Spotlight.Application.Services.Scene
{
    /// <summary>
    /// 顶层场景的稳定标识；它与 Unity 场景文件名分离，避免资源重命名改变业务数据。
    /// </summary>
    public enum SceneId
    {
        SampleScene,
        Bootstrap,
        Menu,
        Hub,
        Demo1,
        Demo2,
        Demo3,
        ChaosDemo
    }
}
