using System;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 俯视小 Boss 的互斥阶段。数值不直接驱动 Animator。
    /// </summary>
    public enum Demo2BossPhase
    {
        Chase = 0,
        SweepWindup = 1,
        ChargeWindup = 2,
        Charging = 3,
        Stunned = 4,
        PermanentlyStunned = 5
    }

    /// <summary>
    /// 小 Boss 的下一次攻击。顺序固定交替，开场第一次是横扫。
    /// </summary>
    public enum Demo2BossAttack
    {
        Sweep = 0,
        Charge = 1
    }

    /// <summary>
    /// 俯视小 Boss 的时间、朝向和撞树结算。不读取玩家血量，也不产生伤害。
    /// 玩家没有生命值；冲撞第一次撞到成熟大树就死亡。
    /// </summary>
    public sealed class Demo2BossStateMachine
    {
        public const float MoveSpeed = 1f;
        public const float BodySize = 1.8f;
        public const float SweepRadius = 3f;
        public const float SweepWindupSeconds = 2f;
        public const float ChargeWindupSeconds = 5f;
        public const float TelegraphLeadSeconds = 1f;
        public const float ChargeDistance = 8f;
        public const float ChargeSpeed = 8f;
        public const float AttackGapSeconds = 5f;
        public const float StunSeconds = 3f;
        public const int DefeatTreeHits = 1;

        private const float DirectionEpsilon = 0.0001f;

        private float _cooldown = AttackGapSeconds;
        private float _phaseTime;
        private float _chargeDistance;
        private Demo2BossAttack _nextAttack = Demo2BossAttack.Sweep;
        private bool _chargeLocked;

        /// <summary>
        /// 当前阶段。
        /// </summary>
        public Demo2BossPhase Phase { get; private set; } = Demo2BossPhase.Chase;

        /// <summary>
        /// 已经叠上的撞树眩晕层数。永久眩晕后不再增加。
        /// </summary>
        public int StunStacks { get; private set; }

        /// <summary>
        /// 横扫红圈是否应该显示。只在前摇最后 1 秒为真。
        /// </summary>
        public bool IsSweepTelegraphVisible { get; private set; }

        /// <summary>
        /// 冲撞红区是否应该显示。朝向锁死之后才为真。
        /// </summary>
        public bool IsChargeTelegraphVisible { get; private set; }

        /// <summary>
        /// 当前朝向的单位向量 X。没有有效目标时保持上一次朝向。
        /// </summary>
        public float FacingX { get; private set; } = 1f;

        /// <summary>
        /// 当前朝向的单位向量 Y。
        /// </summary>
        public float FacingY { get; private set; }

        /// <summary>
        /// 已锁死的冲刺方向 X。未锁死时等于当前朝向。
        /// </summary>
        public float LockedChargeX { get; private set; } = 1f;

        /// <summary>
        /// 已锁死的冲刺方向 Y。
        /// </summary>
        public float LockedChargeY { get; private set; }

        /// <summary>
        /// 这一拍横扫前摇结束。不代表对玩家造成伤害。
        /// </summary>
        public bool SweepResolvedThisTick { get; private set; }

        /// <summary>
        /// 这一拍刚锁死冲刺方向。
        /// </summary>
        public bool ChargeLockedThisTick { get; private set; }

        /// <summary>
        /// 这一拍冲撞撞到了树并进入眩晕。
        /// </summary>
        public bool TreeStunThisTick { get; private set; }

        /// <summary>
        /// 这一拍应沿 X 移动的距离，单位米。
        /// </summary>
        public float StepX { get; private set; }

        /// <summary>
        /// 这一拍应沿 Y 移动的距离，单位米。
        /// </summary>
        public float StepY { get; private set; }

        /// <summary>
        /// 冲刺位移碰到成熟大树时调用。不在冲刺阶段则忽略。
        /// </summary>
        public void NotifyTreeHit()
        {
            if (Phase != Demo2BossPhase.Charging)
            {
                return;
            }

            IsChargeTelegraphVisible = false;
            StepX = 0f;
            StepY = 0f;
            EnterStun();
        }

        /// <summary>
        /// 推进一拍。玩家偏移是 Boss 指向玩家的向量，不必单位化。
        /// 冲撞期间由调用方报告身体是否与成熟大树重叠。
        /// </summary>
        public void Tick(float deltaTime, float playerOffsetX, float playerOffsetY, bool chargeHitsTree)
        {
            SweepResolvedThisTick = false;
            ChargeLockedThisTick = false;
            TreeStunThisTick = false;
            StepX = 0f;
            StepY = 0f;
            if (!IsFiniteNonNegative(deltaTime))
            {
                return;
            }

            switch (Phase)
            {
                case Demo2BossPhase.Chase:
                    TickChase(deltaTime, playerOffsetX, playerOffsetY);
                    break;
                case Demo2BossPhase.SweepWindup:
                    TickSweep(deltaTime);
                    break;
                case Demo2BossPhase.ChargeWindup:
                    TickChargeWindup(deltaTime, playerOffsetX, playerOffsetY);
                    break;
                case Demo2BossPhase.Charging:
                    TickCharge(deltaTime, chargeHitsTree);
                    break;
                case Demo2BossPhase.Stunned:
                    TickStun(deltaTime);
                    break;
                case Demo2BossPhase.PermanentlyStunned:
                    HideTelegraphs();
                    break;
            }
        }

        private void TickChase(float deltaTime, float playerOffsetX, float playerOffsetY)
        {
            HideTelegraphs();
            Face(playerOffsetX, playerOffsetY);
            StepAlong(FacingX, FacingY, MoveSpeed, deltaTime);
            _cooldown -= deltaTime;
            if (_cooldown > 0f)
            {
                return;
            }

            _cooldown = 0f;
            BeginAttack(_nextAttack);
        }

        private void TickSweep(float deltaTime)
        {
            _phaseTime += deltaTime;
            IsSweepTelegraphVisible = _phaseTime >= SweepWindupSeconds - TelegraphLeadSeconds;
            IsChargeTelegraphVisible = false;
            if (_phaseTime < SweepWindupSeconds)
            {
                return;
            }

            SweepResolvedThisTick = true;
            IsSweepTelegraphVisible = false;
            FinishAttack(Demo2BossAttack.Charge);
        }

        private void TickChargeWindup(float deltaTime, float playerOffsetX, float playerOffsetY)
        {
            _phaseTime += deltaTime;
            float lockTime = ChargeWindupSeconds - TelegraphLeadSeconds;
            if (!_chargeLocked)
            {
                Face(playerOffsetX, playerOffsetY);
                if (_phaseTime >= lockTime)
                {
                    LockCharge();
                }
            }

            IsChargeTelegraphVisible = _chargeLocked;
            IsSweepTelegraphVisible = false;
            if (_phaseTime < ChargeWindupSeconds)
            {
                return;
            }

            Phase = Demo2BossPhase.Charging;
            _phaseTime = 0f;
            _chargeDistance = 0f;
        }

        private void TickCharge(float deltaTime, bool chargeHitsTree)
        {
            IsChargeTelegraphVisible = true;
            if (chargeHitsTree)
            {
                IsChargeTelegraphVisible = false;
                EnterStun();
                return;
            }

            float remaining = ChargeDistance - _chargeDistance;
            float step = Math.Min(ChargeSpeed * deltaTime, remaining);
            StepX = LockedChargeX * step;
            StepY = LockedChargeY * step;
            _chargeDistance += step;
            if (_chargeDistance < ChargeDistance - DirectionEpsilon)
            {
                return;
            }

            IsChargeTelegraphVisible = false;
            FinishAttack(Demo2BossAttack.Sweep);
        }

        private void TickStun(float deltaTime)
        {
            HideTelegraphs();
            _cooldown -= deltaTime;
            _phaseTime += deltaTime;
            if (_phaseTime < StunSeconds)
            {
                return;
            }

            Phase = Demo2BossPhase.Chase;
            _phaseTime = 0f;
        }

        private void BeginAttack(Demo2BossAttack attack)
        {
            _phaseTime = 0f;
            _chargeLocked = false;
            Phase = attack == Demo2BossAttack.Sweep
                ? Demo2BossPhase.SweepWindup
                : Demo2BossPhase.ChargeWindup;
        }

        private void FinishAttack(Demo2BossAttack nextAttack)
        {
            _nextAttack = nextAttack;
            _cooldown = AttackGapSeconds;
            _phaseTime = 0f;
            _chargeLocked = false;
            Phase = Demo2BossPhase.Chase;
        }

        private void LockCharge()
        {
            _chargeLocked = true;
            LockedChargeX = FacingX;
            LockedChargeY = FacingY;
            ChargeLockedThisTick = true;
        }

        private void EnterStun()
        {
            TreeStunThisTick = true;
            _cooldown = AttackGapSeconds;
            _phaseTime = 0f;
            _chargeLocked = false;
            _nextAttack = Demo2BossAttack.Sweep;
            StunStacks++;
            if (StunStacks >= DefeatTreeHits)
            {
                Phase = Demo2BossPhase.PermanentlyStunned;
                return;
            }

            Phase = Demo2BossPhase.Stunned;
        }

        private void Face(float offsetX, float offsetY)
        {
            float length = (float)Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
            if (length < DirectionEpsilon)
            {
                return;
            }

            FacingX = offsetX / length;
            FacingY = offsetY / length;
            if (!_chargeLocked)
            {
                LockedChargeX = FacingX;
                LockedChargeY = FacingY;
            }
        }

        private void StepAlong(float directionX, float directionY, float speed, float deltaTime)
        {
            StepX = directionX * speed * deltaTime;
            StepY = directionY * speed * deltaTime;
        }

        private void HideTelegraphs()
        {
            IsSweepTelegraphVisible = false;
            IsChargeTelegraphVisible = false;
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }
}
