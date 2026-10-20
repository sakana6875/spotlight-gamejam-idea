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
        private readonly HashSet<string> _reportedEventIds = new HashSet<string>();
        private readonly List<Demo2PushController> _pushControllers = new List<Demo2PushController>();
        private readonly List<Demo2PitFillTrigger> _pitTriggers = new List<Demo2PitFillTrigger>();
        private readonly List<Demo2MonsterSpawnDevice> _spawnDevices = new List<Demo2MonsterSpawnDevice>();
        private readonly List<PlayerMoveController> _players = new List<PlayerMoveController>();
        private readonly List<Demo2Act1PipePuzzleController> _puzzles = new List<Demo2Act1PipePuzzleController>();
        private readonly List<Demo2TreeController> _trees = new List<Demo2TreeController>();

        private StorySessionHost _host;
        private Demo2SimulationController _simulation;
        private bool _hasSprouted;
        private bool _hasClearedWave;

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
            if (_host != null &&
                StorySessionHost.IsDemo2GameplayEvent(presentation.EventId) &&
                _reportedEventIds.Contains(presentation.EventId) &&
                _host.TryCompleteGameplayEvent(presentation.EventId))
            {
                return;
            }

            ApplyPause(presentation);
        }

        private void ApplyPause(StoryPresentation presentation)
        {
            if (_simulation == null)
            {
                return;
            }

            if (StorySessionHost.IsDemo2GameplayEvent(presentation.EventId))
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

        private void Collect(Demo2Act1PipePuzzleController[] sources)
        {
            for (int index = 0; index < sources.Length; index++)
            {
                Demo2Act1PipePuzzleController source = sources[index];
                source.PuzzleCompleted += OnPuzzleCompleted;
                _puzzles.Add(source);
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
                if (HasSprouted(source.Phase))
                {
                    _hasSprouted = true;
                }
            }
        }

        private void OnPushStarted()
        {
            Report("evolution_010");
        }

        private void OnPitFilled()
        {
            Report("evolution_012");
        }

        private void OnFirstMonsterSpawned()
        {
            Report("evolution_014");
        }

        private void OnAttackStrikeStarted()
        {
            Report("evolution_018");
        }

        private void OnPuzzleCompleted()
        {
            Report("evolution_030");
        }

        private void OnBiteStarted()
        {
            Report("evolution_032");
        }

        private void OnSproutingCompleted()
        {
            _hasSprouted = true;
            TryEnterAct1();
        }

        private void OnWaveCompleted()
        {
            _hasClearedWave = true;
            TryEnterAct1();
        }

        private void TryEnterAct1()
        {
            if (!_hasSprouted || !_hasClearedWave || _host == null || !IsBaselineDemo2())
            {
                return;
            }

            _host.LoadDemo2Act1();
        }

        private bool IsBaselineDemo2()
        {
            SceneCatalog catalog = new SceneCatalog();
            return catalog.TryGetSceneName(SceneCatalog.Demo2, out string sceneName) &&
                gameObject.scene.name == sceneName;
        }

        private static bool HasSprouted(Demo2TreePhase phase)
        {
            return phase == Demo2TreePhase.AwaitingGrowth ||
                phase == Demo2TreePhase.Growing ||
                phase == Demo2TreePhase.Mature;
        }


        private void Report(string eventId)
        {
            if (!_reportedEventIds.Add(eventId) || _host == null)
            {
                return;
            }

            _host.TryCompleteGameplayEvent(eventId);
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

            _pushControllers.Clear();
            _pitTriggers.Clear();
            _spawnDevices.Clear();
            _players.Clear();
            _puzzles.Clear();
            _trees.Clear();
            _hasSprouted = false;
            _hasClearedWave = false;
            if (_simulation != null)
            {
                _simulation.ReleasePause(Demo2PauseReason.Dialogue);
                _simulation = null;
            }
        }
    }
}
