using System;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// Demo2 玩家当前可观察的互斥玩法状态；数值与 Animator 的 State 参数保持稳定映射。
    /// </summary>
    public enum Demo2PlayerState
    {
        Idle = 0,
        Moving = 1,
        Attack1 = 2,
        Attack2 = 3,
        HitStun = 4
    }

    /// <summary>
    /// 维护玩家移动、二段攻击、攻击间隔和受击硬直之间的合法转换。
    /// </summary>
    public sealed class Demo2PlayerStateMachine
    {
        private const float AttackDuration = 31f / 60f;
        private const float AttackInterval = 0.8f;
        private const float ComboQueueStartTime = 0.25f;
        private const float HitStunDuration = 0.5f;

        private float _stateElapsedTime;
        private float _attackCooldownRemaining;
        private bool _isSecondAttackQueued;

        /// <summary>
        /// 当前玩家状态。
        /// </summary>
        public Demo2PlayerState State { get; private set; } = Demo2PlayerState.Idle;

        /// <summary>
        /// 当前状态是否允许移动和跳跃输入生效。
        /// </summary>
        public bool CanMove => State == Demo2PlayerState.Idle || State == Demo2PlayerState.Moving;

        /// <summary>
        /// 当前状态是否允许向石头施加推动。
        /// </summary>
        public bool CanPush => CanMove;

        /// <summary>
        /// 按缩放时间推进移动、攻击和受击状态。
        /// </summary>
        public void Advance(float deltaTime, bool isMoving, bool attackPressed)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            _attackCooldownRemaining = Math.Max(0f, _attackCooldownRemaining - deltaTime);

            switch (State)
            {
                case Demo2PlayerState.Attack1:
                    AdvanceFirstAttack(deltaTime, isMoving, attackPressed);
                    return;
                case Demo2PlayerState.Attack2:
                    AdvanceTimedState(deltaTime, AttackDuration, isMoving);
                    return;
                case Demo2PlayerState.HitStun:
                    AdvanceTimedState(deltaTime, HitStunDuration, isMoving);
                    return;
                default:
                    AdvanceLocomotion(isMoving, attackPressed);
                    return;
            }
        }

        /// <summary>
        /// 中断当前动作并进入0.5秒受击硬直；硬直中重复请求不会刷新时长。
        /// </summary>
        public bool TryEnterHitStun()
        {
            if (State == Demo2PlayerState.HitStun)
            {
                return false;
            }

            _isSecondAttackQueued = false;
            EnterState(Demo2PlayerState.HitStun);
            return true;
        }

        /// <summary>
        /// 为视角转场取消当前动作和二段排队，并立即恢复待机。
        /// </summary>
        public void CancelCurrentAction()
        {
            _isSecondAttackQueued = false;
            EnterState(Demo2PlayerState.Idle);
        }

        private void AdvanceLocomotion(bool isMoving, bool attackPressed)
        {
            if (attackPressed && _attackCooldownRemaining <= 0f)
            {
                _isSecondAttackQueued = false;
                _attackCooldownRemaining = AttackInterval;
                EnterState(Demo2PlayerState.Attack1);
                return;
            }

            EnterState(isMoving ? Demo2PlayerState.Moving : Demo2PlayerState.Idle);
        }

        private void AdvanceFirstAttack(float deltaTime, bool isMoving, bool attackPressed)
        {
            _stateElapsedTime += deltaTime;

            if (attackPressed && _stateElapsedTime >= ComboQueueStartTime &&
                _stateElapsedTime < AttackDuration)
            {
                _isSecondAttackQueued = true;
            }

            if (_stateElapsedTime < AttackDuration)
            {
                return;
            }

            if (_isSecondAttackQueued)
            {
                _isSecondAttackQueued = false;
                _attackCooldownRemaining = AttackInterval;
                EnterState(Demo2PlayerState.Attack2);
                return;
            }

            EnterState(isMoving ? Demo2PlayerState.Moving : Demo2PlayerState.Idle);
        }

        private void AdvanceTimedState(float deltaTime, float duration, bool isMoving)
        {
            _stateElapsedTime += deltaTime;
            if (_stateElapsedTime >= duration)
            {
                EnterState(isMoving ? Demo2PlayerState.Moving : Demo2PlayerState.Idle);
            }
        }

        private void EnterState(Demo2PlayerState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            _stateElapsedTime = 0f;
        }
    }
}
