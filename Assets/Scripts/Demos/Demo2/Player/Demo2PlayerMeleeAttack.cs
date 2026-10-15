using System.Collections.Generic;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 在玩家攻击状态报告命中事实时，执行一次可调矩形范围伤害查询。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Demo2PlayerMeleeAttack : MonoBehaviour
    {
        private const int InitialOverlapCapacity = 8;

        [Header("攻击判定")]
        [SerializeField] private Transform _attackPoint;
        [SerializeField] private Vector2 _attackSize = new Vector2(1.5f, 0.8f);
        [SerializeField] private int _damage = 10;
        [SerializeField] private LayerMask _monsterLayers;
        [SerializeField] private LayerMask _obstacleLayers;

        private readonly List<Collider2D> _overlapResults = new List<Collider2D>(InitialOverlapCapacity);
        private readonly List<Demo2MonsterController> _targets =
            new List<Demo2MonsterController>(InitialOverlapCapacity);

        private ContactFilter2D _monsterFilter;
        private bool _isConfigured;

        private void Awake()
        {
            if (!TryValidateConfiguration(out string error))
            {
                Debug.LogError($"Demo2PlayerMeleeAttack 配置无效：{error}", this);
                enabled = false;
                return;
            }

            _monsterFilter = new ContactFilter2D();
            _monsterFilter.useLayerMask = true;
            _monsterFilter.SetLayerMask(_monsterLayers);
            _monsterFilter.useTriggers = false;
            _isConfigured = true;
        }

        /// <summary>
        /// 对当前 AttackPoint 矩形内所有未被侧墙遮挡的存活怪物各施加一次伤害。
        /// </summary>
        public void ResolveStrike()
        {
            if (!_isConfigured)
            {
                return;
            }

            _overlapResults.Clear();
            _targets.Clear();
            Physics2D.OverlapBox(_attackPoint.position, _attackSize, 0f, _monsterFilter, _overlapResults);

            foreach (Collider2D overlap in _overlapResults)
            {
                Demo2MonsterController monster = overlap.GetComponentInParent<Demo2MonsterController>();
                if (monster == null || !monster.IsAlive || _targets.Contains(monster))
                {
                    continue;
                }

                _targets.Add(monster);
            }

            Vector2 attackOrigin = transform.position;
            foreach (Demo2MonsterController target in _targets)
            {
                if (target == null || !target.IsAlive ||
                    Physics2D.Linecast(attackOrigin, target.transform.position, _obstacleLayers).collider != null)
                {
                    continue;
                }

                target.TryTakeDamage(_damage);
            }
        }

        private bool TryValidateConfiguration(out string error)
        {
            if (_attackPoint == null)
            {
                error = "必须配置 AttackPoint。";
                return false;
            }

            if (!IsPositiveFinite(_attackSize.x) || !IsPositiveFinite(_attackSize.y))
            {
                error = "攻击区域宽高必须是有限正数。";
                return false;
            }

            if (_damage <= 0)
            {
                error = "伤害必须是正整数。";
                return false;
            }

            if (_monsterLayers.value == 0)
            {
                error = "Monster Layers 不能为空。";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void OnDrawGizmosSelected()
        {
            if (_attackPoint == null || !IsPositiveFinite(_attackSize.x) || !IsPositiveFinite(_attackSize.y))
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.2f, 0.1f, 1f);
            Gizmos.DrawWireCube(_attackPoint.position, _attackSize);
        }
    }
}
