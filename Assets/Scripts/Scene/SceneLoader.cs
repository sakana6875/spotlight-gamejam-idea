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
        /// 真正调用 LoadSceneAsync 之前通知。新场景的 EventSystem 会在 sceneLoaded 之前 OnEnable。
        /// </summary>
        public event Action BeforeLoad;

        /// <summary>
        /// LoadSceneAsync 没有开始时通知，方便把加载前关掉的 EventSystem 开回来。
        /// </summary>
        public event Action LoadAborted;
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
                BeforeLoad?.Invoke();
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
                if (operation == null)
                {
                    LoadAborted?.Invoke();
                    Debug.LogError("场景加载失败：Unity 未接受稳定场景 ID：" + sceneId);
                    return SceneLoadResult.FailedToStart;
                }

                return SceneLoadResult.Started;
            }
            catch (Exception exception)
            {
                LoadAborted?.Invoke();
                Debug.LogError("场景加载失败：" + sceneId + "；原因：" + exception.Message);
                return SceneLoadResult.FailedToStart;
            }
        }
    }

}
