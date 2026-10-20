using System;
using UnityEngine;
using Spotlight.Demos.Demo1.Application;
using Spotlight.Scene;
using Spotlight.Story;

namespace Spotlight.Demos.Demo1
{
    /// <summary>
    /// 接收组合根服务并初始化 Demo1 场景对象。
    /// </summary>
    public sealed class Demo1SceneEntry : MonoBehaviour
    {
        [SerializeField] private Demo1FlowController _flowController;
        [SerializeField] private Demo1GameplayAssembler _gameplayAssembler;

        private SceneLoader _sceneLoader;
        private IMusicPlaybackService _musicPlaybackService;
        private StoryDemoBridge _storyBridge;

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 注入 Demo1 当前实际使用的场景和音乐能力。
        /// </summary>
        public bool Initialize(
            SceneLoader sceneLoader,
            IMusicPlaybackService musicPlaybackService,
            StoryDemoBridge storyBridge)
        {
            if (sceneLoader == null)
            {
                Debug.LogError("Demo1 初始化失败：sceneLoader 为 null。", this);
                return false;
            }

            if (musicPlaybackService == null)
            {
                Debug.LogError("Demo1 初始化失败：musicPlaybackService 为 null。", this);
                return false;
            }

            if (IsInitialized)
            {
                return ReferenceEquals(_sceneLoader, sceneLoader)
                    && ReferenceEquals(_musicPlaybackService, musicPlaybackService);
            }

            if (_flowController == null || _gameplayAssembler == null)
            {
                Debug.LogError("Demo1 初始化失败：场景入口缺少流程控制器或玩法装配器。", this);
                return false;
            }

            _sceneLoader = sceneLoader;
            _musicPlaybackService = musicPlaybackService;
            _flowController.Initialize(_sceneLoader);

            _storyBridge = storyBridge;
            if (!_gameplayAssembler.Initialize(_musicPlaybackService, OnRunFinished))
            {
                return false;
            }

            IsInitialized = true;
            return true;
        }

        private void OnRunFinished(int finalHeight)
        {
            if (_storyBridge != null && _storyBridge.TryCompleteDemo1(finalHeight))
            {
                return;
            }

            _flowController.ReturnToSampleScene();
        }
    }
}
