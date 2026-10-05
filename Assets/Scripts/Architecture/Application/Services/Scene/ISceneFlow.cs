namespace Spotlight.Application.Services.Scene
{
    /// <summary>
    /// 提供顶层场景流程请求；具体 Unity 加载由适配器负责。
    /// </summary>
    public interface ISceneFlow
    {
        SceneId? CurrentSceneId { get; }
        SceneLoadResult LoadScene(SceneId sceneId);
        SceneLoadResult LoadMenu();
        SceneLoadResult LoadHub();
        SceneLoadResult EnterDemo(DemoId demoId, DemoEntryMode entryMode);
        SceneLoadResult RestartDemo(DemoId demoId);
    }
}
