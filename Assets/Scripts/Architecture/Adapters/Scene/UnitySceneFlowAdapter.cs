using System;
using UnityEngine.SceneManagement;
using Spotlight.Application.Services.Scene;

namespace Spotlight.Adapters.Scene
{
    /// <summary>
    /// Unity 场景加载边界；只有该适配器可以调用 SceneManager。
    /// </summary>
    public sealed class UnitySceneFlowAdapter : ISceneFlow
    {
        private readonly SceneCatalog _catalog;

        public UnitySceneFlowAdapter(SceneCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public SceneId? CurrentSceneId { get; private set; }

        public SceneLoadResult LoadScene(SceneId sceneId)
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

            try
            {
                SceneManager.LoadSceneAsync(definition.UnitySceneName);
            }
            catch (Exception exception)
            {
                return SceneLoadResult.Failed(SceneLoadResultCode.LoadFailed, exception.Message);
            }

            CurrentSceneId = sceneId;
            return SceneLoadResult.Succeeded(sceneId);
        }

        public SceneLoadResult LoadMenu()
        {
            return LoadScene(SceneId.Menu);
        }

        public SceneLoadResult LoadHub()
        {
            return LoadScene(SceneId.Hub);
        }

        public SceneLoadResult EnterDemo(DemoId demoId, DemoEntryMode entryMode)
        {
            if (!_catalog.TryGetSceneId(demoId, out SceneId sceneId))
            {
                return SceneLoadResult.Failed(SceneLoadResultCode.InvalidDemoId);
            }

            SceneLoadResult result = LoadScene(sceneId);
            return result.IsSuccess
                ? SceneLoadResult.Succeeded(sceneId, demoId)
                : result;
        }

        public SceneLoadResult RestartDemo(DemoId demoId)
        {
            if (!_catalog.TryGetSceneId(demoId, out SceneId sceneId))
            {
                return SceneLoadResult.Failed(SceneLoadResultCode.InvalidDemoId);
            }

            CurrentSceneId = null;
            return LoadScene(sceneId);
        }
    }
}
