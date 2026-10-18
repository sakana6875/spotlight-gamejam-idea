using System;
using UnityEngine;
using Spotlight.Scene;

namespace Spotlight.Demos.Demo1
{
    /// <summary>
    /// 把 Demo1 的返回请求交给统一场景加载边界。
    /// </summary>
    public sealed class Demo1FlowController : MonoBehaviour
    {
        private SceneLoader _sceneLoader;

        /// <summary>
        /// 绑定组合根持有的场景加载器。
        /// </summary>
        public void Initialize(SceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
        }

        /// <summary>
        /// 请求返回当前启动场景。
        /// </summary>
        public SceneLoadResult ReturnToSampleScene()
        {
            if (_sceneLoader == null)
            {
                Debug.LogError("Demo1 返回失败：场景流程尚未初始化。", this);
                return SceneLoadResult.FailedToStart;
            }

            return _sceneLoader.LoadScene(SceneCatalog.Sample);
        }
    }
}
