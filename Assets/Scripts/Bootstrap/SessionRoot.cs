using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Spotlight.Audio;
using Healing.Demo3.Adapters;
using Healing.Demo3.Application;
using Spotlight.Demos.Demo1;
using Spotlight.Demos.Demo2;
using Spotlight.Save;
using Spotlight.Scene;
using Spotlight.Story;

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
        private StoryDemoBridge _storyBridge;
        private StorySessionHost _storyHost;
        private bool _ownsServices;
        private bool _holdOwnedEventSystem;
        private StoryDialogueView _placedStoryView;

        public SaveService SaveService => _saveService;
        public SceneLoader SceneLoader => _sceneLoader;
        public AudioManager AudioManager => _audioManager;
        public StorySessionHost StoryHost => _storyHost;
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
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                if (_sceneLoader != null)
                {
                    _sceneLoader.BeforeLoad -= DisableOwnedEventSystem;
                    _sceneLoader.BeforeLoad -= ReclaimStoryCanvas;
                    _sceneLoader.LoadAborted -= OnSceneLoadAborted;
                    _sceneLoader.LoadAborted -= RestoreStoryCanvasAfterAbort;
                }
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
                _storyBridge = new StoryDemoBridge();
                _storyHost = GetComponent<StorySessionHost>();
                if (_storyHost == null)
                {
                    _storyHost = gameObject.AddComponent<StorySessionHost>();
                }

                _storyHost.Initialize(_sceneLoader, _storyBridge);
                AudioSource musicSource = GetComponent<AudioSource>();
                if (musicSource == null)
                {
                    musicSource = gameObject.AddComponent<AudioSource>();
                }

                _audioManager = new AudioManager(musicSource);
                _sceneLoader.BeforeLoad += DisableOwnedEventSystem;
                _sceneLoader.BeforeLoad += ReclaimStoryCanvas;
                _sceneLoader.LoadAborted += OnSceneLoadAborted;
                _sceneLoader.LoadAborted += RestoreStoryCanvasAfterAbort;
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.sceneUnloaded += OnSceneUnloaded;
                IsInitialized = true;
                DontDestroyOnLoad(gameObject);
                InjectSceneEntry(gameObject.scene);
                HideOwnedStoryCanvas();
                return true;
            }
            catch (Exception exception)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                if (_sceneLoader != null)
                {
                    _sceneLoader.BeforeLoad -= DisableOwnedEventSystem;
                    _sceneLoader.BeforeLoad -= ReclaimStoryCanvas;
                    _sceneLoader.LoadAborted -= OnSceneLoadAborted;
                    _sceneLoader.LoadAborted -= RestoreStoryCanvasAfterAbort;
                }

                _saveService = null;
                _sceneLoader = null;
                _audioManager = null;
                _storyBridge = null;
                _storyHost = null;
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

        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            if (_holdOwnedEventSystem)
            {
                return;
            }

            KeepSingleEventSystem();
        }

        private void InjectSceneEntry(UnityEngine.SceneManagement.Scene scene)
        {
            _holdOwnedEventSystem = false;
            KeepSingleEventSystem();
            BindStoryViews(scene);
            _storyHost?.NotifySceneLoaded(scene.name);
            BindDemo2Story(scene);
            BindDemo3Return(scene);
            _storyHost?.NotifyDemo3Returned(scene.name);
            if (scene.name == "Hub")
            {
                _storyHost?.StartStory();
            }

            PlaceStoryCanvas(scene);

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
                && !selectedEntry.Initialize(_sceneLoader, _audioManager, _storyBridge))
            {
                Debug.LogError("Demo1 场景入口初始化失败。", selectedEntry);
            }
        }

        /// <summary>
        /// SessionRoot 的 EventSystem 要跨场景保住剧情点击。目标场景自己已有一份时关掉它，避免控制台报两个 EventSystem。
        /// 必须在 LoadSceneAsync 之前关掉，因为新场景的 EventSystem 会先 OnEnable。
        /// </summary>
        private void KeepSingleEventSystem()
        {
            if (_holdOwnedEventSystem)
            {
                SetOwnedEventSystemEnabled(false);
                return;
            }

            EventSystem owned = GetComponentInChildren<EventSystem>(true);
            if (owned == null)
            {
                return;
            }

            bool sceneHasAnother = false;
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                UnityEngine.SceneManagement.Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded || scene == gameObject.scene)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                for (int index = 0; index < roots.Length; index++)
                {
                    EventSystem[] systems = roots[index].GetComponentsInChildren<EventSystem>(true);
                    for (int systemIndex = 0; systemIndex < systems.Length; systemIndex++)
                    {
                        if (systems[systemIndex] != owned)
                        {
                            sceneHasAnother = true;
                            break;
                        }
                    }

                    if (sceneHasAnother)
                    {
                        break;
                    }
                }

                if (sceneHasAnother)
                {
                    break;
                }
            }
            SetOwnedEventSystemEnabled(!sceneHasAnother);
        }

        private void OnSceneLoadAborted()
        {
            _holdOwnedEventSystem = false;
            KeepSingleEventSystem();
        }

        private void DisableOwnedEventSystem()
        {
            _holdOwnedEventSystem = true;
            SetOwnedEventSystemEnabled(false);
        }

        private void SetOwnedEventSystemEnabled(bool enabled)
        {
            EventSystem owned = GetComponentInChildren<EventSystem>(true);
            if (owned == null)
            {
                return;
            }

            owned.enabled = enabled;
            BaseInputModule module = owned.GetComponent<BaseInputModule>();
            if (module != null)
            {
                module.enabled = enabled;
            }
        }

        private void BindStoryViews(UnityEngine.SceneManagement.Scene scene)
        {
            if (_storyHost == null)
            {
                return;
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int index = 0; index < rootObjects.Length; index++)
            {
                StoryViewEntry[] entries = rootObjects[index].GetComponentsInChildren<StoryViewEntry>(true);
                for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
                {
                    entries[entryIndex].Bind(_storyHost);
                }
            }
        }

        private static bool HostsDialogue(string sceneName)
        {
            return sceneName == "Hub" || sceneName == "demo1" || sceneName == "demo2" ||
                sceneName == "demo2_act1" || sceneName == "demo2_act2";
        }

        private void ReclaimStoryCanvas()
        {
            if (_placedStoryView == null)
            {
                return;
            }

            _placedStoryView.gameObject.SetActive(false);
            _placedStoryView.transform.SetParent(transform, false);
            _placedStoryView = null;
        }

        private void RestoreStoryCanvasAfterAbort()
        {
            PlaceStoryCanvas(SceneManager.GetActiveScene());
        }

        private void PlaceStoryCanvas(UnityEngine.SceneManagement.Scene scene)
        {
            if (_storyHost == null || !scene.IsValid() || !scene.isLoaded || !HostsDialogue(scene.name))
            {
                return;
            }

            if (SceneHasDialogue(scene))
            {
                return;
            }

            StoryDialogueView view = GetComponentInChildren<StoryDialogueView>(true);
            if (view == null)
            {
                return;
            }

            view.transform.SetParent(null, false);
            SceneManager.MoveGameObjectToScene(view.gameObject, scene);
            _placedStoryView = view;
            view.GetComponent<StoryViewEntry>()?.Bind(_storyHost);
            view.gameObject.SetActive(true);
        }

        private static bool SceneHasDialogue(UnityEngine.SceneManagement.Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].GetComponentInChildren<StoryDialogueView>(true) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void HideOwnedStoryCanvas()
        {
            StoryDialogueView view = GetComponentInChildren<StoryDialogueView>(true);
            if (view != null)
            {
                view.gameObject.SetActive(false);
            }
        }

        private void BindDemo2Story(UnityEngine.SceneManagement.Scene scene)
        {
            if (_storyHost == null || !scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();
            Demo2SimulationController simulation = null;
            for (int index = 0; index < rootObjects.Length; index++)
            {
                simulation = rootObjects[index].GetComponentInChildren<Demo2SimulationController>(true);
                if (simulation != null)
                {
                    break;
                }
            }

            // 第二幕场景没有暂停控制器。旁白期间仍要冻住 Boss，否则打字机还没读完就会被攻击打断。
            if (simulation == null && scene.name == "demo2_act2")
            {
                GameObject pauseRoot = new GameObject("Demo2Simulation");
                SceneManager.MoveGameObjectToScene(pauseRoot, scene);
                simulation = pauseRoot.AddComponent<Demo2SimulationController>();
            }

            if (simulation == null)
            {
                return;
            }

            Demo2StoryRelay relay = simulation.GetComponent<Demo2StoryRelay>();
            if (relay == null)
            {
                relay = simulation.gameObject.AddComponent<Demo2StoryRelay>();
            }

            relay.Bind(_storyHost);
        }

        /// <summary>
        /// 只在剧情拉起的 Act1 上替换结果上报。直接打开 Demo3 场景时仍走本地日志。
        /// </summary>
        private void BindDemo3Return(UnityEngine.SceneManagement.Scene scene)
        {
            if (_storyHost == null || !_storyHost.IsWaitingForDemo3 || scene.name != "Act1")
            {
                return;
            }

            Demo3FlowCarrier carrier = Demo3FlowCarrier.Current;
            if (carrier == null)
            {
                Debug.LogError("Act1 没有 Demo3FlowCarrier，结局后无法回到 Hub。", this);
                return;
            }

            carrier.UseRunReporter(new Demo3StoryRun(_storyHost));
        }

        /// <summary>
        /// 结局上报成功后拆掉 Demo3 载体并回 Hub。失败和中途退出不打断 Demo3。
        /// </summary>
        private sealed class Demo3StoryRun : IDemoRun
        {
            private readonly StorySessionHost _host;

            public Demo3StoryRun(StorySessionHost host)
            {
                _host = host;
            }

            public void Report(DemoRunResult result)
            {
                if (result != DemoRunResult.Succeeded || _host == null)
                {
                    return;
                }

                Demo3FlowCarrier.Shutdown();
                _host.ReturnFromDemo3();
            }
        }
    }
}
