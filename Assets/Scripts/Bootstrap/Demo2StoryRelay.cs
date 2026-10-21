using Spotlight.Scene;
using Spotlight.Domain.Story;
using System.Collections.Generic;
using Spotlight.Demos.Demo2;
using Spotlight.Story;
using UnityEngine;

namespace Spotlight.Bootstrap
{
    /// <summary>
    /// 把进化之地的玩法事实交给剧情，并在旁白期间暂停玩法。
    /// 由组合根挂到当前场景的暂停控制器上，场景卸载时解除订阅。
    /// </summary>
    public sealed class Demo2StoryRelay : MonoBehaviour
    {
        private readonly List<Demo2PushController> _pushControllers = new List<Demo2PushController>();
        private readonly List<Demo2PitFillTrigger> _pitTriggers = new List<Demo2PitFillTrigger>();
        private readonly List<Demo2MonsterSpawnDevice> _spawnDevices = new List<Demo2MonsterSpawnDevice>();
        private readonly List<PlayerMoveController> _players = new List<PlayerMoveController>();
        private readonly List<Demo2Act1PipePuzzleController> _puzzles = new List<Demo2Act1PipePuzzleController>();
        private readonly List<Demo2TreeController> _trees = new List<Demo2TreeController>();
        private readonly List<Demo2BossController> _bosses = new List<Demo2BossController>();

        private StorySessionHost _host;
        private Demo2SimulationController _simulation;
        private bool _puzzleCompleted;

        /// <summary>
        /// 订阅当前场景的事实，并按宿主当前画面同步暂停。重复绑定同一宿主不会重复订阅。
        /// </summary>
        public void Bind(StorySessionHost host)
        {
            if (host == null)
            {
                return;
            }

            if (_host == host)
            {
                OnPresentation(host.Current);
                return;
            }

            Unbind();
            _host = host;
            _simulation = GetComponent<Demo2SimulationController>();
            _host.PresentationChanged += OnPresentation;
            SubscribeFacts();
            OnPresentation(host.Current);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnPresentation(StoryPresentation presentation)
        {
            ApplyPause(presentation);
        }

        private void ApplyPause(StoryPresentation presentation)
        {
            if (_simulation == null)
            {
                return;
            }

            if (_host != null && _host.IsNarrationHeld)
            {
                _simulation.ReleasePause(Demo2PauseReason.Dialogue);
                return;
            }

            if (presentation.SceneLabel == "进化之地" &&
                (presentation.Kind == StoryWaitKind.Line ||
                 presentation.Kind == StoryWaitKind.Choices ||
                 presentation.Kind == StoryWaitKind.Event))
            {
                _simulation.AcquirePause(Demo2PauseReason.Dialogue);
                return;
            }

            _simulation.ReleasePause(Demo2PauseReason.Dialogue);
        }

        private void SubscribeFacts()
        {
            UnityEngine.SceneManagement.Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                Collect(roots[index].GetComponentsInChildren<Demo2PushController>(true));
                Collect(roots[index].GetComponentsInChildren<Demo2PitFillTrigger>(true));
                Collect(roots[index].GetComponentsInChildren<Demo2MonsterSpawnDevice>(true));
                Collect(roots[index].GetComponentsInChildren<PlayerMoveController>(true));
                Collect(roots[index].GetComponentsInChildren<Demo2Act1PipePuzzleController>(true));
                Collect(roots[index].GetComponentsInChildren<Demo2TreeController>(true));
                Collect(roots[index].GetComponentsInChildren<Demo2BossController>(true));
            }
        }


        private void Collect(Demo2Act1PipePuzzleController[] sources)
        {
            for (int index = 0; index < sources.Length; index++)
            {
                Demo2Act1PipePuzzleController source = sources[index];
                source.PuzzleCompleted += OnPuzzleCompleted;
                _puzzles.Add(source);
            }
        }

        private void Collect(Demo2PushController[] sources)
        {
            for (int index = 0; index < sources.Length; index++)
            {
                Demo2PushController source = sources[index];
                source.PushStarted += OnPushStarted;
                _pushControllers.Add(source);
            }
        }

        private void Collect(Demo2PitFillTrigger[] sources)
        {
            for (int index = 0; index < sources.Length; index++)
            {
                Demo2PitFillTrigger source = sources[index];
                source.PitFilled += OnPitFilled;
                _pitTriggers.Add(source);
            }
        }

        private void Collect(Demo2MonsterSpawnDevice[] sources)
        {
            for (int index = 0; index < sources.Length; index++)
            {
                Demo2MonsterSpawnDevice source = sources[index];
                source.FirstMonsterSpawned += OnFirstMonsterSpawned;
                source.WaveCompleted += OnWaveCompleted;
                _spawnDevices.Add(source);
            }
        }

        private void Collect(PlayerMoveController[] sources)
        {
            for (int index = 0; index < sources.Length; index++)
            {
                PlayerMoveController source = sources[index];
                source.AttackStrikeStarted += OnAttackStrikeStarted;
                _players.Add(source);
            }
        }


        private void Collect(Demo2TreeController[] sources)
        {
            for (int index = 0; index < sources.Length; index++)
            {
                Demo2TreeController source = sources[index];
                source.BiteStarted += OnBiteStarted;
                source.SproutingCompleted += OnSproutingCompleted;
                _trees.Add(source);
            }
        }

        private void Collect(Demo2BossController[] sources)
        {
            for (int index = 0; index < sources.Length; index++)
            {
                Demo2BossController source = sources[index];
                source.Defeated += OnBossDefeated;
                _bosses.Add(source);
                if (source.IsDefeated)
                {
                    OnBossDefeated();
                }
            }
        }

        private void OnPushStarted()
        {
            _host?.NotifyDemo2NarrationGate(Demo2StoryGates.PushNarrationGateId);
        }

        private void OnPitFilled()
        {
            _host?.NotifyDemo2NarrationGate(Demo2StoryGates.PitNarrationGateId);
        }

        private void OnFirstMonsterSpawned()
        {
            _host?.NotifyDemo2NarrationGate(Demo2StoryGates.MonsterNarrationGateId);
        }

        private void OnAttackStrikeStarted()
        {
            _host?.NotifyDemo2NarrationGate(Demo2StoryGates.AttackNarrationGateId);
        }

        private void OnBiteStarted()
        {
            _host?.NotifyDemo2NarrationGate(Demo2StoryGates.BiteNarrationGateId);
        }

        private void OnWaveCompleted()
        {
            _host?.NotifyDemo2NarrationGate(Demo2StoryGates.WaveNarrationGateId);
        }

        private void OnBossDefeated()
        {
            _host?.NotifyDemo2BossDefeated();
        }


        private void OnPuzzleCompleted()
        {
            _puzzleCompleted = true;
            RequestMatureGrowth();
        }


        private void OnSproutingCompleted()
        {
            _host?.NotifyDemo2NarrationGate(Demo2StoryGates.PitNarrationGateId);
            if (IsAct1() && _puzzleCompleted)
            {
                RequestMatureGrowth();
            }
        }




        private void RequestMatureGrowth()
        {
            if (!IsAct1() || !_puzzleCompleted)
            {
                return;
            }

            bool sawTree = false;
            for (int index = 0; index < _trees.Count; index++)
            {
                Demo2TreeController tree = _trees[index];
                if (tree == null)
                {
                    continue;
                }

                sawTree = true;
                if (tree.Phase == Demo2TreePhase.Mature)
                {
                    continue;
                }

                if (tree.Phase == Demo2TreePhase.AwaitingGrowth)
                {
                    if (tree.BeginGrowth() != Demo2TreeTransitionResult.Succeeded)
                    {
                        Debug.LogError("第一幕的树无法生长，不能进入第二幕。", tree);
                    }

                    continue;
                }

                if (tree.Phase == Demo2TreePhase.Dormant &&
                    tree.BeginSprouting() != Demo2TreeTransitionResult.Succeeded)
                {
                    Debug.LogError("第一幕的树无法发芽，不能长成后进入第二幕。", tree);
                }
            }

            if (!sawTree)
            {
                Debug.LogError("第一幕没有可生长的树，无法进入第二幕。", this);
                return;
            }

        }



        private bool IsAct1()
        {
            return IsScene(SceneCatalog.Demo2Act1);
        }

        private bool IsScene(string sceneId)
        {
            SceneCatalog catalog = new SceneCatalog();
            return catalog.TryGetSceneName(sceneId, out string sceneName) &&
                gameObject.scene.name == sceneName;
        }


        private void Unbind()
        {
            if (_host != null)
            {
                _host.PresentationChanged -= OnPresentation;
                _host = null;
            }

            for (int index = 0; index < _pushControllers.Count; index++)
            {
                if (_pushControllers[index] != null)
                {
                    _pushControllers[index].PushStarted -= OnPushStarted;
                }
            }

            for (int index = 0; index < _pitTriggers.Count; index++)
            {
                if (_pitTriggers[index] != null)
                {
                    _pitTriggers[index].PitFilled -= OnPitFilled;
                }
            }

            for (int index = 0; index < _spawnDevices.Count; index++)
            {
                if (_spawnDevices[index] != null)
                {
                    _spawnDevices[index].FirstMonsterSpawned -= OnFirstMonsterSpawned;
                    _spawnDevices[index].WaveCompleted -= OnWaveCompleted;
                }
            }

            for (int index = 0; index < _players.Count; index++)
            {
                if (_players[index] != null)
                {
                    _players[index].AttackStrikeStarted -= OnAttackStrikeStarted;
                }
            }

            for (int index = 0; index < _puzzles.Count; index++)
            {
                if (_puzzles[index] != null)
                {
                    _puzzles[index].PuzzleCompleted -= OnPuzzleCompleted;
                }
            }

            for (int index = 0; index < _trees.Count; index++)
            {
                if (_trees[index] != null)
                {
                    _trees[index].BiteStarted -= OnBiteStarted;
                    _trees[index].SproutingCompleted -= OnSproutingCompleted;
                }
            }

            for (int index = 0; index < _bosses.Count; index++)
            {
                if (_bosses[index] != null)
                {
                    _bosses[index].Defeated -= OnBossDefeated;
                }
            }

            _pushControllers.Clear();
            _pitTriggers.Clear();
            _spawnDevices.Clear();
            _players.Clear();
            _puzzles.Clear();
            _trees.Clear();
            _bosses.Clear();
            _puzzleCompleted = false;
            if (_simulation != null)
            {
                _simulation.ReleasePause(Demo2PauseReason.Dialogue);
                _simulation = null;
            }
        }
    }
}
