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
    /// 玩家状态推进在当前帧产生的一次性攻击命中事实。
    /// </summary>
    public enum Demo2PlayerAdvanceResult
    {
        None = 0,
        Attack1Strike = 1,
        Attack2Strike = 2
    }

    /// <summary>
    /// 维护玩家移动、二段攻击、攻击间隔和受击硬直之间的合法转换。
    /// </summary>
    public sealed class Demo2PlayerStateMachine
    {
        private const float AttackDuration = 31f / 60f;
        private const float AttackInterval = 0.8f;
        private const float ComboQueueStartTime = 0.25f;
        private const float AttackStrikeTime = 0.25f;
        private const float HitStunDuration = 0.5f;

        private float _stateElapsedTime;
        private float _attackCooldownRemaining;
        private bool _isSecondAttackQueued;
        private bool _hasCurrentAttackStruck;

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
        /// 按缩放时间推进移动、攻击和受击状态，并返回当前帧首次发生的攻击命中事实。
        /// </summary>
        public Demo2PlayerAdvanceResult Advance(float deltaTime, bool isMoving, bool attackPressed)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            _attackCooldownRemaining = Math.Max(0f, _attackCooldownRemaining - deltaTime);

            switch (State)
            {
                case Demo2PlayerState.Attack1:
                    return AdvanceFirstAttack(deltaTime, isMoving, attackPressed);
                case Demo2PlayerState.Attack2:
                    return AdvanceSecondAttack(deltaTime, isMoving);
                case Demo2PlayerState.HitStun:
                    AdvanceTimedState(deltaTime, HitStunDuration, isMoving);
                    return Demo2PlayerAdvanceResult.None;
                default:
                    AdvanceLocomotion(isMoving, attackPressed);
                    return Demo2PlayerAdvanceResult.None;
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

        private Demo2PlayerAdvanceResult AdvanceFirstAttack(
            float deltaTime,
            bool isMoving,
            bool attackPressed)
        {
            Demo2PlayerAdvanceResult result = AdvanceAttackTime(
                deltaTime,
                Demo2PlayerAdvanceResult.Attack1Strike);

            if (attackPressed && _stateElapsedTime >= ComboQueueStartTime &&
                _stateElapsedTime < AttackDuration)
            {
                _isSecondAttackQueued = true;
            }

            if (_stateElapsedTime < AttackDuration)
            {
                return result;
            }

            if (_isSecondAttackQueued)
            {
                _isSecondAttackQueued = false;
                _attackCooldownRemaining = AttackInterval;
                EnterState(Demo2PlayerState.Attack2);
                return result;
            }

            EnterState(isMoving ? Demo2PlayerState.Moving : Demo2PlayerState.Idle);
            return result;
        }

        private Demo2PlayerAdvanceResult AdvanceSecondAttack(float deltaTime, bool isMoving)
        {
            Demo2PlayerAdvanceResult result = AdvanceAttackTime(
                deltaTime,
                Demo2PlayerAdvanceResult.Attack2Strike);

            if (_stateElapsedTime >= AttackDuration)
            {
                EnterState(isMoving ? Demo2PlayerState.Moving : Demo2PlayerState.Idle);
            }

            return result;
        }

        private Demo2PlayerAdvanceResult AdvanceAttackTime(
            float deltaTime,
            Demo2PlayerAdvanceResult strikeResult)
        {
            float previousElapsedTime = _stateElapsedTime;
            _stateElapsedTime += deltaTime;

            if (_hasCurrentAttackStruck || previousElapsedTime >= AttackStrikeTime ||
                _stateElapsedTime < AttackStrikeTime)
            {
                return Demo2PlayerAdvanceResult.None;
            }

            _hasCurrentAttackStruck = true;
            return strikeResult;
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
            if (state == Demo2PlayerState.Attack1 || state == Demo2PlayerState.Attack2)
            {
                _hasCurrentAttackStruck = false;
            }
        }
    }
}
