using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 在绑定树苗开始发芽后，独立生成并统计一组侧视怪物。
    /// </summary>
    public sealed class Demo2MonsterSpawnDevice : MonoBehaviour
    {
        private const int OverlapCapacity = 8;

        [Header("场景依赖")]
        [SerializeField] private Demo2TreeController _treeController;
        [SerializeField] private Demo2SimulationController _simulationController;

        [Header("生成配置")]
        [SerializeField] private Demo2MonsterController _monsterPrefab;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _spawnedMonstersRoot;
        [SerializeField] private int _spawnCount = 8;
        [SerializeField] private float _spawnIntervalSeconds = 2f;
        [SerializeField] private LayerMask _spawnBlockingLayers = ~0;
        [SerializeField] private Vector2 _spawnClearanceSize = new Vector2(0.9f, 1.5f);

        private readonly Collider2D[] _overlapResults = new Collider2D[OverlapCapacity];
        private readonly HashSet<Demo2MonsterController> _activeMonsters =
            new HashSet<Demo2MonsterController>();

        private float _elapsedSeconds;
        private int _spawnedCount;
        private bool _isConfigured;
        private bool _hasStarted;
        private bool _isSpawnDue;
        private bool _hasCompleted;
        private bool _hasReportedFirstSpawn;


        /// <summary>
        /// 当前装置已经成功生成的怪物数量。
        /// </summary>
        public int SpawnedCount => _spawnedCount;

        /// <summary>
        /// 当前由本装置生成且尚未死亡的怪物数量。
        /// </summary>
        public int ActiveMonsterCount => _activeMonsters.Count;

        /// <summary>
        /// 配置数量全部生成且全部死亡时发布的一次性事实。
        /// </summary>
        public event Action WaveCompleted;
        /// <summary>
        /// 本装置第一次成功生成怪物的事实。出生点被挡住时不会提前发布。
        /// </summary>
        public event Action FirstMonsterSpawned;


        private void Awake()
        {
            if (!TryValidateConfiguration(out string error))
            {
                DisableForInvalidConfiguration(error);
                return;
            }

            _isConfigured = true;
        }

        private void OnEnable()
        {
            if (!_isConfigured)
            {
                return;
            }

            _treeController.SproutingStarted += HandleSproutingStarted;
            if (_treeController.Phase != Demo2TreePhase.Dormant)
            {
                StartWave();
            }
        }

        private void OnDisable()
        {
            if (_isConfigured)
            {
                _treeController.SproutingStarted -= HandleSproutingStarted;
            }
        }

        private void OnDestroy()
        {
            if (_isConfigured)
            {
                _treeController.SproutingStarted -= HandleSproutingStarted;
            }

            foreach (Demo2MonsterController monster in _activeMonsters)
            {
                if (monster != null)
                {
                    monster.Died -= HandleMonsterDied;
                }
            }

            _activeMonsters.Clear();
        }

        private void Update()
        {
            if (!_isConfigured || !_hasStarted || _hasCompleted ||
                _spawnedCount >= _spawnCount || _simulationController.IsGameplayFrozen)
            {
                return;
            }

            if (!_isSpawnDue)
            {
                _elapsedSeconds += Time.deltaTime;
                _isSpawnDue = _elapsedSeconds >= _spawnIntervalSeconds;
            }

            if (_isSpawnDue && !IsSpawnBlocked())
            {
                SpawnMonster();
            }
        }

        private void HandleSproutingStarted()
        {
            StartWave();
        }

        private void StartWave()
        {
            if (_hasStarted)
            {
                return;
            }

            _hasStarted = true;
            _isSpawnDue = true;
            _elapsedSeconds = 0f;
        }

        private bool IsSpawnBlocked()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.SetLayerMask(_spawnBlockingLayers);
            filter.useTriggers = false;

            int overlapCount = Physics2D.OverlapBox(
                _spawnPoint.position,
                _spawnClearanceSize,
                0f,
                filter,
                _overlapResults);
            return overlapCount > 0;
        }

        private void SpawnMonster()
        {
            Demo2MonsterController monster = Instantiate(
                _monsterPrefab,
                _spawnPoint.position,
                Quaternion.identity,
                _spawnedMonstersRoot);

            if (!monster.Initialize(_treeController, _simulationController))
            {
                Destroy(monster.gameObject);
                DisableForInvalidConfiguration("生成的怪物拒绝场景依赖初始化。");
                return;
            }

            monster.Died += HandleMonsterDied;
            _activeMonsters.Add(monster);
            _spawnedCount++;
            _elapsedSeconds = 0f;
            _isSpawnDue = false;
            if (!_hasReportedFirstSpawn)
            {
                _hasReportedFirstSpawn = true;
                FirstMonsterSpawned?.Invoke();
            }

        }

        private void HandleMonsterDied(IMonster deadMonster)
        {
            if (deadMonster is not Demo2MonsterController monster || !_activeMonsters.Remove(monster))
            {
                return;
            }

            monster.Died -= HandleMonsterDied;
            TryCompleteWave();
        }

        private void TryCompleteWave()
        {
            if (_hasCompleted || _spawnedCount != _spawnCount || _activeMonsters.Count != 0)
            {
                return;
            }

            _hasCompleted = true;
            WaveCompleted?.Invoke();
        }

        private bool TryValidateConfiguration(out string error)
        {
            if (_treeController == null || _simulationController == null)
            {
                error = "必须配置树苗和玩法暂停控制器。";
                return false;
            }

            if (_monsterPrefab == null || _spawnPoint == null || _spawnedMonstersRoot == null)
            {
                error = "必须配置怪物 Prefab、SpawnPoint 和 SpawnedMonsters Root。";
                return false;
            }

            if (_spawnCount <= 0)
            {
                error = "生成数量必须是正整数。";
                return false;
            }

            if (!IsPositiveFinite(_spawnIntervalSeconds))
            {
                error = "生成间隔必须是有限正数。";
                return false;
            }

            if (!IsPositiveFinite(_spawnClearanceSize.x) || !IsPositiveFinite(_spawnClearanceSize.y))
            {
                error = "生成区域尺寸必须是有限正数。";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private void DisableForInvalidConfiguration(string reason)
        {
            Debug.LogError("Demo2MonsterSpawnDevice 配置无效：" + reason, this);
            enabled = false;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_spawnPoint == null)
            {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_spawnPoint.position, _spawnClearanceSize);
        }
#endif
    }
}
