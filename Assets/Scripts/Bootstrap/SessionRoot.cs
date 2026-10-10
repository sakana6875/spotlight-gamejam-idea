using UnityEngine;
using Spotlight.Save;
using Spotlight.Scene;

namespace Spotlight.Bootstrap
{
    /// <summary>
    /// 唯一游戏入口，直接创建当前项目实际需要的服务。
    /// </summary>
    public sealed class SessionRoot : MonoBehaviour
    {
        private SaveService _saveService;
        private SceneLoader _sceneLoader;

        public SaveService SaveService => _saveService;
        public SceneLoader SceneLoader => _sceneLoader;
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            Initialize();
        }


        /// <summary>
        /// 创建当前运行周期的服务；重复调用不会创建第二组服务。
        /// </summary>
        public bool Initialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            try
            {
                _saveService = new SaveService();
                _sceneLoader = new SceneLoader(new SceneCatalog());
                IsInitialized = true;
                DontDestroyOnLoad(gameObject);
                return true;
            }
            catch (System.Exception exception)
            {
                _saveService = null;
                _sceneLoader = null;
                Debug.LogError("SessionRoot 初始化失败：" + exception.Message, this);
                return false;
            }
        }

    }
}
