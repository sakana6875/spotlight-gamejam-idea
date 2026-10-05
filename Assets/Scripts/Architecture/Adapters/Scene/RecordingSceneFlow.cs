using System;
using Spotlight.Application.Services.Scene;

namespace Spotlight.Adapters.Scene
{
    /// <summary>
    /// 记录场景流程请求的纯 C# 适配器，用于验证参数和明确失败结果。
    /// </summary>
    public class RecordingSceneFlow : ISceneFlow
    {
        private readonly SceneCatalog _catalog;

        public RecordingSceneFlow(SceneCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public SceneId? CurrentSceneId { get; private set; }
        public DemoId? LastDemoId { get; private set; }
        public DemoEntryMode? LastEntryMode { get; private set; }

        public SceneLoadResult LoadScene(SceneId sceneId)
        {
            return TryActivate(sceneId);
        }

        public SceneLoadResult LoadMenu()
        {
            return TryActivate(SceneId.Menu);
        }

        public SceneLoadResult LoadHub()
        {
            return TryActivate(SceneId.Hub);
        }

        public SceneLoadResult EnterDemo(DemoId demoId, DemoEntryMode entryMode)
        {
            if (!_catalog.TryGetSceneId(demoId, out SceneId sceneId))
            {
                return SceneLoadResult.Failed(SceneLoadResultCode.InvalidDemoId);
            }

            LastDemoId = demoId;
            LastEntryMode = entryMode;
            return TryActivate(sceneId, demoId);
        }

        public SceneLoadResult RestartDemo(DemoId demoId)
        {
            return EnterDemo(demoId, DemoEntryMode.Replay);
        }

        private SceneLoadResult TryActivate(SceneId sceneId, DemoId? demoId = null)
        {
            if (!_catalog.TryGet(sceneId, out SceneDefinition definition))
            {
                return SceneLoadResult.Failed(SceneLoadResultCode.UnknownScene);
            }

            if (!definition.IsAvailable)
            {
                return SceneLoadResult.Failed(SceneLoadResultCode.NotAvailable);
            }

            if (CurrentSceneId == sceneId)
            {
                return SceneLoadResult.Failed(SceneLoadResultCode.AlreadyActive);
            }

            CurrentSceneId = sceneId;
            return SceneLoadResult.Succeeded(sceneId, demoId);
        }
    }
}
