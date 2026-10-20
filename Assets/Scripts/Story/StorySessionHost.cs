using System;
using Spotlight.Domain.Story;
using Spotlight.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Spotlight.Story
{
    /// <summary>
    /// 跨场景剧情宿主。只提供可调用会话和展示数据，不创建对话框。
    /// </summary>
    public sealed class StorySessionHost : MonoBehaviour
    {
        public const string CatalogResourcePath = "Story/StoryCatalog";
        public const string SpeakerResourcePath = "Story/StorySpeakerCatalog";
        public const string Demo1PlayEventId = "music_008";
        public const string Demo2EnterEventId = "evolution_005";

        private readonly SceneCatalog _sceneCatalog = new SceneCatalog();
        private SceneLoader _sceneLoader;
        private StoryDemoBridge _bridge;
        private StorySpeakerCatalog _speakers;
        private StorySession _session;
        private string _completeWhenSceneId = string.Empty;
        private bool _hasRequestedAct1;
        private bool _suppressKnownEvents;
        private string _loadError = string.Empty;

        public StoryPresentation Current { get; private set; } =
            new StoryPresentation(StoryWaitKind.Idle, string.Empty, null, null, null, string.Empty, null, string.Empty, string.Empty, string.Empty, 0);

        public event Action<StoryPresentation> PresentationChanged;
        public bool IsReady => _session != null;
        public Sprite HallBackground => _speakers == null ? null : _speakers.HallBackground;

        /// <summary>
        /// 由组合根注入场景加载器和 Demo 结果通道。
        /// </summary>
        public void Initialize(SceneLoader sceneLoader, StoryDemoBridge bridge)
        {
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            if (_bridge != null)
            {
                _bridge.Demo1Completed -= OnDemo1Completed;
            }

            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _bridge.Demo1Completed += OnDemo1Completed;
            LoadCatalogs();
        }

        /// <summary>
        /// 从开场对白开始。已开始后再次调用不会重置，只把当前画面再交给新场景的对话框。
        /// </summary>
        public bool StartStory()
        {
            if (HasStarted)
            {
                Publish();
                return true;
            }

            return StartBranch(StorySession.StartNodeId);
        }

        /// <summary>
        /// 开场是否已经离开空闲。回到 Hub 或进入 Demo 时用来避免重播 intro。
        /// </summary>
        public bool HasStarted => _session != null && _session.WaitKind != StoryWaitKind.Idle;

        /// <summary>
        /// 从指定节点开始，用于问候等独立分支。
        /// </summary>
        public bool StartBranch(string nodeId)
        {
            if (_session == null)
            {
                PublishBlocked(_loadError.Length == 0 ? "剧情资产尚未加载。" : _loadError);
                return false;
            }

            return _session.Start(nodeId);
        }

        public bool Advance()
        {
            return _session != null && _session.Advance();
        }

        public bool SelectChoice(string nodeId)
        {
            return _session != null && _session.SelectChoice(nodeId);
        }

        public void SetPlayerName(string playerName)
        {
            _session?.SetPlayerName(playerName);
        }

        /// <summary>
        /// 完成当前演出。Demo1 进行中或场景切换未完成时忽略，避免跳过尚未交回的结果。
        /// </summary>
        public bool CompleteCurrentEvent()
        {
            if (_session == null || IsSceneCommandPending)
            {
                return false;
            }

            return _session.CompleteEvent();
        }

        /// <summary>
        /// 只在当前正在等待的演出 ID 与玩法事实一致时完成。提前发生的事实由调用方记住，等到该节点再补完成。
        /// </summary>
        public bool TryCompleteGameplayEvent(string eventId)
        {
            if (_session == null || IsSceneCommandPending || _session.WaitKind != StoryWaitKind.Event ||
                _session.Current == null || _session.Current.Id != eventId)
            {
                return false;
            }

            return _session.CompleteEvent();
        }

        /// <summary>
        /// 进化之地里必须由玩法事实完成、不能靠点屏幕跳过的演出。
        /// evolution_027 是提示，不在此列。
        /// </summary>
        public static bool IsDemo2GameplayEvent(string eventId)
        {
            switch (eventId)
            {
                case "evolution_010":
                case "evolution_012":
                case "evolution_014":
                case "evolution_018":
                case "evolution_020":
                case "evolution_022":
                case "evolution_024":
                case "evolution_028":
                case "evolution_030":
                case "evolution_032":
                case "evolution_035":
                case "evolution_038":
                case "evolution_040":
                case "evolution_041":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 基线侧视的树已经发芽且刷怪波次清空后，加载第一幕。
        /// 若当前正等待侧视演出，场景加载完成后再推进到下一段旁白。
        /// </summary>
        public bool LoadDemo2Act1()
        {
            if (_hasRequestedAct1 || _sceneLoader == null)
            {
                return false;
            }

            _hasRequestedAct1 = true;
            string eventId = _session != null && _session.Current != null
                ? _session.Current.Id
                : string.Empty;
            if (IsDemo2SideActEvent(eventId))
            {
                _completeWhenSceneId = SceneCatalog.Demo2Act1;
            }

            SceneLoadResult result = _sceneLoader.LoadScene(SceneCatalog.Demo2Act1);
            if (result == SceneLoadResult.Started)
            {
                return true;
            }

            _hasRequestedAct1 = false;
            if (_completeWhenSceneId == SceneCatalog.Demo2Act1)
            {
                _completeWhenSceneId = string.Empty;
            }

            Debug.LogError("剧情无法加载 Demo2 第一幕：" + result, this);
            return false;
        }

        private static bool IsDemo2SideActEvent(string eventId)
        {
            switch (eventId)
            {
                case "evolution_010":
                case "evolution_012":
                case "evolution_014":
                case "evolution_018":
                case "evolution_020":
                    return true;
                default:
                    return false;
            }
        }



        /// <summary>
        /// 按文案里的失败跳转离开当前演出。没有玩法回调的失败由调用方触发。
        /// </summary>
        public bool FailCurrentEvent()
        {
            if (_session == null || IsSceneCommandPending)
            {
                return false;
            }

            return _session.FailEvent();
        }

        public void NotifySceneLoaded(string sceneName)
        {
            if (_completeWhenSceneId.Length == 0
                || !_sceneCatalog.TryGetSceneId(sceneName, out string sceneId)
                || sceneId != _completeWhenSceneId)
            {
                return;
            }

            _completeWhenSceneId = string.Empty;
            _session?.CompleteEvent();
        }

        public void Publish()
        {
            PresentationChanged?.Invoke(Current);
        }

        private bool IsSceneCommandPending =>
            (_bridge != null && _bridge.IsWaitingForDemo1) || _completeWhenSceneId.Length > 0;

        private void OnDestroy()
        {
            if (_bridge != null)
            {
                _bridge.Demo1Completed -= OnDemo1Completed;
            }

            if (_session != null)
            {
                _session.Changed -= OnSessionChanged;
            }
        }

        private void LoadCatalogs()
        {
            StoryCatalogAsset catalog = Resources.Load<StoryCatalogAsset>(CatalogResourcePath);
            _speakers = Resources.Load<StorySpeakerCatalog>(SpeakerResourcePath);
            string error = null;
            if (catalog == null || !catalog.TryCreateGraph(out StoryGraph graph, out error))
            {
                _loadError = catalog == null ? "找不到剧情资产：" + CatalogResourcePath : error;
                Debug.LogError(_loadError, this);
                _session = null;
                PublishBlocked(_loadError);
                return;
            }

            if (_session != null)
            {
                _session.Changed -= OnSessionChanged;
            }

            _session = new StorySession(graph);
            _session.Changed += OnSessionChanged;
            _loadError = string.Empty;
            Refresh(StoryWaitKind.Idle);
        }

        private void OnSessionChanged()
        {
            Refresh(_session.WaitKind);
            if (!_suppressKnownEvents)
            {
                TryRunKnownEvent();
            }
        }

        private void TryRunKnownEvent()
        {
            if (_session == null || _session.WaitKind != StoryWaitKind.Event || _session.Current == null)
            {
                return;
            }

            string eventId = _session.Current.Id;
            if (eventId == Demo1PlayEventId)
            {
                BeginDemo1();
                return;
            }

            if (eventId == Demo2EnterEventId)
            {
                BeginDemo2();
            }
        }

        private void BeginDemo1()
        {
            if (_bridge.IsWaitingForDemo1)
            {
                return;
            }

            string resumeSceneId = SceneCatalog.Hub;
            if (_sceneCatalog.TryGetSceneId(SceneManager.GetActiveScene().name, out string activeId)
                && activeId != SceneCatalog.Demo1)
            {
                resumeSceneId = activeId;
            }

            _bridge.BeginDemo1(resumeSceneId);
            SceneLoadResult result = _sceneLoader.LoadScene(SceneCatalog.Demo1);
            if (result == SceneLoadResult.FailedToStart || result == SceneLoadResult.UnknownSceneId)
            {
                _bridge.CancelDemo1();
                Debug.LogError("剧情无法加载 Demo1：" + result, this);
            }
        }

        private void BeginDemo2()
        {
            _completeWhenSceneId = SceneCatalog.Demo2;
            SceneLoadResult result = _sceneLoader.LoadScene(SceneCatalog.Demo2);
            if (result != SceneLoadResult.Started)
            {
                _completeWhenSceneId = string.Empty;
                if (result == SceneLoadResult.AlreadyActive)
                {
                    _session.CompleteEvent();
                    return;
                }

                Debug.LogError("剧情无法加载 Demo2：" + result, this);
            }
        }

        private void OnDemo1Completed(int finalHeight)
        {
            _suppressKnownEvents = true;
            _session?.SetDemo1Height(finalHeight);
            _suppressKnownEvents = false;
            string resumeSceneId = _bridge.ResumeSceneId.Length == 0
                ? SceneCatalog.Hub
                : _bridge.ResumeSceneId;
            _completeWhenSceneId = resumeSceneId;
            SceneLoadResult result = _sceneLoader.LoadScene(resumeSceneId);
            if (result == SceneLoadResult.Started)
            {
                return;
            }

            _completeWhenSceneId = string.Empty;
            if (result != SceneLoadResult.AlreadyActive)
            {
                Debug.LogError("剧情无法回到进入 Demo1 前的场景：" + resumeSceneId, this);
            }

            _session?.CompleteEvent();
        }

        private void Refresh(StoryWaitKind kind)
        {
            if (_session == null || _session.Current == null)
            {
                Current = new StoryPresentation(
                    kind,
                    string.Empty,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<Sprite>(),
                    string.Empty,
                    Array.Empty<StoryChoice>(),
                    string.Empty,
                    string.Empty,
                    _session == null ? _loadError : _session.BlockReason,
                    _session == null ? 0 : _session.LastDemo1Height);
                Publish();
                return;
            }

            StoryNode node = _session.Current;
            string[] speakerIds = node.SpeakerIds;
            string[] names = new string[speakerIds.Length];
            Sprite[] portraits = new Sprite[speakerIds.Length];
            for (int index = 0; index < speakerIds.Length; index++)
            {
                names[index] = speakerIds[index];
                if (_speakers != null && _speakers.TryGet(speakerIds[index], out StorySpeakerEntry entry))
                {
                    names[index] = entry.DisplayName;
                    portraits[index] = entry.Portrait;
                }
            }

            StoryChoice[] choices = new StoryChoice[_session.CurrentChoices.Count];
            for (int index = 0; index < choices.Length; index++)
            {
                StoryNode choice = _session.CurrentChoices[index];
                choices[index] = new StoryChoice(choice.Id, _session.DisplayText(choice));
            }

            Current = new StoryPresentation(
                kind,
                node.Id,
                speakerIds,
                names,
                portraits,
                _session.DisplayText(node),
                choices,
                kind == StoryWaitKind.Event ? node.Id : string.Empty,
                node.SceneLabel,
                _session.BlockReason,
                _session.LastDemo1Height);
            Publish();
        }

        private void PublishBlocked(string reason)
        {
            Current = new StoryPresentation(
                StoryWaitKind.Blocked,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<Sprite>(),
                string.Empty,
                Array.Empty<StoryChoice>(),
                string.Empty,
                string.Empty,
                reason,
                0);
            Publish();
        }
    }
}
