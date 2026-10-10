using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Spotlight.Scene
{
    /// <summary>
    /// 当前项目唯一的 Unity 场景加载边界。
    /// </summary>
    public sealed class SceneLoader
    {
        private readonly SceneCatalog _sceneCatalog;

        /// <summary>
        /// 使用组合根提供的稳定场景映射创建加载器。
        /// </summary>
        public SceneLoader(SceneCatalog sceneCatalog)
        {
            _sceneCatalog = sceneCatalog ?? throw new ArgumentNullException(nameof(sceneCatalog));
        }

        /// <summary>
        /// 请求加载稳定 ID 对应的 Build Settings 场景，并返回请求是否已被 Unity 接受。
        /// </summary>
        public SceneLoadResult LoadScene(string sceneId)
        {
            if (!_sceneCatalog.TryGetSceneName(sceneId, out string sceneName))
            {
                Debug.LogError("场景加载失败：未登记稳定场景 ID：" + sceneId);
                return SceneLoadResult.UnknownSceneId;
            }

            if (SceneManager.GetActiveScene().name == sceneName)
            {
                return SceneLoadResult.AlreadyActive;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("场景加载失败：稳定场景 ID 未映射到 Build Settings 场景：" + sceneId);
                return SceneLoadResult.FailedToStart;
            }

            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
                if (operation == null)
                {
                    Debug.LogError("场景加载失败：Unity 未接受稳定场景 ID：" + sceneId);
                    return SceneLoadResult.FailedToStart;
                }

                return SceneLoadResult.Started;
            }
            catch (Exception exception)
            {
                Debug.LogError("场景加载失败：" + sceneId + "；原因：" + exception.Message);
                return SceneLoadResult.FailedToStart;
            }
        }
    }

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
