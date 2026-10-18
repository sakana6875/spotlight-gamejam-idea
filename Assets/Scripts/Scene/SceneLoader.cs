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
        /// 请求加载稳定 ID 对应的场景，并返回 Unity 是否接受该请求。
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
                Debug.LogError("场景加载失败：场景不在 Build Settings 中：" + sceneName);
                return SceneLoadResult.FailedToStart;
            }

            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
                if (operation == null)
                {
                    Debug.LogError("场景加载失败：Unity 未接受加载请求：" + sceneName);
                    return SceneLoadResult.FailedToStart;
                }

                return SceneLoadResult.Started;
            }
            catch (Exception exception)
            {
                Debug.LogError("场景加载失败：" + sceneName + "；原因：" + exception.Message);
                return SceneLoadResult.FailedToStart;
            }
        }
    }
}
