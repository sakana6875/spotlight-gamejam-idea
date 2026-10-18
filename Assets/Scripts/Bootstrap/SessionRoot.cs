using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Spotlight.Audio;
using Spotlight.Demos.Demo1;
using Spotlight.Save;
using Spotlight.Scene;

namespace Spotlight.Bootstrap
{
    /// <summary>
    /// 唯一游戏入口，创建跨场景服务并向场景入口注入依赖。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class SessionRoot : MonoBehaviour
    {
        private SaveService _saveService;
        private SceneLoader _sceneLoader;
        private AudioManager _audioManager;
        private bool _ownsServices;

        public SaveService SaveService => _saveService;
        public SceneLoader SceneLoader => _sceneLoader;
        public AudioManager AudioManager => _audioManager;
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            SessionRoot existingRoot = FindExistingRoot();
            if (existingRoot != null)
            {
                _saveService = existingRoot._saveService;
                _sceneLoader = existingRoot._sceneLoader;
                _audioManager = existingRoot._audioManager;
                IsInitialized = true;
                Destroy(gameObject);
                return;
            }

            _ownsServices = true;
            Initialize();
        }

        private void OnDestroy()
        {
            if (_ownsServices && IsInitialized)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
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
                AudioSource musicSource = GetComponent<AudioSource>();
                if (musicSource == null)
                {
                    musicSource = gameObject.AddComponent<AudioSource>();
                }

                _audioManager = new AudioManager(musicSource);
                SceneManager.sceneLoaded += OnSceneLoaded;
                IsInitialized = true;
                DontDestroyOnLoad(gameObject);
                InjectSceneEntry(gameObject.scene);
                return true;
            }
            catch (Exception exception)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _saveService = null;
                _sceneLoader = null;
                _audioManager = null;
                Debug.LogError("SessionRoot 初始化失败：" + exception.Message, this);
                return false;
            }
        }

        private SessionRoot FindExistingRoot()
        {
            SessionRoot[] roots = FindObjectsByType<SessionRoot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < roots.Length; index++)
            {
                SessionRoot root = roots[index];
                if (root != this && root._ownsServices && root.IsInitialized)
                {
                    return root;
                }
            }

            return null;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            InjectSceneEntry(scene);
        }

        private void InjectSceneEntry(UnityEngine.SceneManagement.Scene scene)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            Demo1SceneEntry selectedEntry = null;

            for (int index = 0; index < rootObjects.Length; index++)
            {
                Demo1SceneEntry entry = rootObjects[index].GetComponentInChildren<Demo1SceneEntry>(true);
                if (entry == null)
                {
                    continue;
                }

                if (selectedEntry != null)
                {
                    Debug.LogError("Demo1 场景存在多个 Demo1SceneEntry。", this);
                    return;
                }

                selectedEntry = entry;
            }

            if (selectedEntry != null
                && !selectedEntry.Initialize(_sceneLoader, _audioManager))
            {
                Debug.LogError("Demo1 场景入口初始化失败。", selectedEntry);
            }
        }
    }
}
