using System;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// Demo2 侧视小怪的互斥玩法状态；数值同时作为 Animator State 参数合同。
    /// </summary>
    public enum Demo2MonsterState
    {
        Moving = 0,
        Biting = 1,
        HitStun = 2,
        Dead = 3
    }

    /// <summary>
    /// 维护小怪生命、啃食、受击硬直和死亡表现之间的合法转换。
    /// </summary>
    public sealed class Demo2MonsterStateMachine
    {
        private readonly float _hitStunDurationSeconds;
        private readonly float _deathDurationSeconds;

        private float _stateElapsedSeconds;

        /// <summary>
        /// 使用已验证的生命和计时配置创建移动中的小怪。
        /// </summary>
        public Demo2MonsterStateMachine(
            int maxHealth,
            float hitStunDurationSeconds,
            float deathDurationSeconds)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "最大生命必须是正整数。");
            }

            if (!IsPositiveFinite(hitStunDurationSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hitStunDurationSeconds),
                    "受击硬直时长必须是有限正数。");
            }

            if (!IsPositiveFinite(deathDurationSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deathDurationSeconds),
                    "死亡表现时长必须是有限正数。");
            }

            CurrentHealth = maxHealth;
            _hitStunDurationSeconds = hitStunDurationSeconds;
            _deathDurationSeconds = deathDurationSeconds;
            State = Demo2MonsterState.Moving;
        }

        /// <summary>
        /// 当前互斥玩法状态。
        /// </summary>
        public Demo2MonsterState State { get; private set; }

        /// <summary>
        /// 当前剩余生命值；死亡后固定为零。
        /// </summary>
        public int CurrentHealth { get; private set; }

        /// <summary>
        /// 怪物是否仍可接受伤害。
        /// </summary>
        public bool IsAlive => State != Demo2MonsterState.Dead;

        /// <summary>
        /// 死亡表现是否已经达到配置时长，可以销毁视觉对象。
        /// </summary>
        public bool ShouldDisappear { get; private set; }

        /// <summary>
        /// 按缩放时间推进当前状态；暂停期间调用方不得调用。
        /// </summary>
        public void Advance(float deltaTime, bool isWithinBiteRange)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "状态时间增量必须是有限非负数。");
            }

            switch (State)
            {
                case Demo2MonsterState.Moving:
                case Demo2MonsterState.Biting:
                    State = isWithinBiteRange
                        ? Demo2MonsterState.Biting
                        : Demo2MonsterState.Moving;
                    break;

                case Demo2MonsterState.HitStun:
                    _stateElapsedSeconds += deltaTime;
                    if (_stateElapsedSeconds >= _hitStunDurationSeconds)
                    {
                        _stateElapsedSeconds = 0f;
                        State = isWithinBiteRange
                            ? Demo2MonsterState.Biting
                            : Demo2MonsterState.Moving;
                    }

                    break;

                case Demo2MonsterState.Dead:
                    if (ShouldDisappear)
                    {
                        break;
                    }

                    _stateElapsedSeconds += deltaTime;
                    ShouldDisappear = _stateElapsedSeconds >= _deathDurationSeconds;
                    break;

                default:
                    throw new InvalidOperationException("遇到未定义的小怪状态。");
            }
        }

        /// <summary>
        /// 尝试扣除生命；每次未致死伤害都会从零重新计算硬直。
        /// </summary>
        public MonsterDamageResult TryTakeDamage(int damage)
        {
            if (damage <= 0)
            {
                return MonsterDamageResult.InvalidAmount;
            }

            if (!IsAlive)
            {
                return MonsterDamageResult.AlreadyDead;
            }

            CurrentHealth = Math.Max(0, CurrentHealth - damage);
            _stateElapsedSeconds = 0f;

            if (CurrentHealth == 0)
            {
                State = Demo2MonsterState.Dead;
                ShouldDisappear = false;
                return MonsterDamageResult.Killed;
            }

            State = Demo2MonsterState.HitStun;
            return MonsterDamageResult.Applied;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
