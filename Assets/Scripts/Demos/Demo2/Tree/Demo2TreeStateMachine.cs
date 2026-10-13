using System;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// Demo2 树苗的生命周期阶段。
    /// </summary>
    public enum Demo2TreePhase
    {
        Dormant,
        Sprouting,
        AwaitingGrowth,
        Growing,
        Mature
    }

    /// <summary>
    /// 树生命周期命令的执行结果。
    /// </summary>
    public enum Demo2TreeTransitionResult
    {
        Succeeded,
        InvalidState
    }

    /// <summary>
    /// 单次啃食时间推进的执行结果。
    /// </summary>
    public enum Demo2TreeBiteResult
    {
        Ignored,
        Accumulated,
        LimitReached
    }

    /// <summary>
    /// 维护树苗状态转换和累计啃食时间，不依赖 Unity 对象。
    /// </summary>
    public sealed class Demo2TreeStateMachine
    {
        private readonly float _biteLimitSeconds;
        private bool _hasReportedBiteLimit;

        /// <summary>
        /// 使用指定啃食阈值创建可见但尚未发芽的休眠树苗。
        /// </summary>
        public Demo2TreeStateMachine(float biteLimitSeconds)
        {
            if (biteLimitSeconds <= 0f || float.IsNaN(biteLimitSeconds) || float.IsInfinity(biteLimitSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(biteLimitSeconds), "啃食时间阈值必须是有限正数。");
            }

            _biteLimitSeconds = biteLimitSeconds;
            Phase = Demo2TreePhase.Dormant;
        }

        /// <summary>
        /// 当前生命周期阶段。
        /// </summary>
        public Demo2TreePhase Phase { get; private set; }

        /// <summary>
        /// 至少一只怪物攻击时累计的总秒数；不会按攻击者数量倍增。
        /// </summary>
        public float AccumulatedBiteSeconds { get; private set; }

        /// <summary>
        /// 请求开始嫩芽出现动画。
        /// </summary>
        public Demo2TreeTransitionResult BeginSprouting()
        {
            return TryTransition(Demo2TreePhase.Dormant, Demo2TreePhase.Sprouting);
        }

        /// <summary>
        /// 记录嫩芽出现动画已经完成。
        /// </summary>
        public Demo2TreeTransitionResult CompleteSprouting()
        {
            return TryTransition(Demo2TreePhase.Sprouting, Demo2TreePhase.AwaitingGrowth);
        }

        /// <summary>
        /// 由关卡结算流程请求开始最终成长。
        /// </summary>
        public Demo2TreeTransitionResult BeginGrowth()
        {
            return TryTransition(Demo2TreePhase.AwaitingGrowth, Demo2TreePhase.Growing);
        }

        /// <summary>
        /// 记录最终成长动画已经完成。
        /// </summary>
        public Demo2TreeTransitionResult CompleteGrowth()
        {
            return TryTransition(Demo2TreePhase.Growing, Demo2TreePhase.Mature);
        }

        /// <summary>
        /// 在待成长阶段按是否存在攻击者推进一份啃食时间。
        /// </summary>
        public Demo2TreeBiteResult AdvanceBiteTime(float deltaSeconds, bool isUnderAttack)
        {
            if (deltaSeconds < 0f || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "啃食时间增量必须是有限非负数。");
            }

            if (Phase != Demo2TreePhase.AwaitingGrowth || !isUnderAttack || deltaSeconds <= 0f)
            {
                return Demo2TreeBiteResult.Ignored;
            }

            AccumulatedBiteSeconds = Math.Min(_biteLimitSeconds, AccumulatedBiteSeconds + deltaSeconds);
            if (!_hasReportedBiteLimit && AccumulatedBiteSeconds >= _biteLimitSeconds)
            {
                _hasReportedBiteLimit = true;
                return Demo2TreeBiteResult.LimitReached;
            }

            return Demo2TreeBiteResult.Accumulated;
        }

        private Demo2TreeTransitionResult TryTransition(Demo2TreePhase expected, Demo2TreePhase target)
        {
            if (Phase != expected)
            {
                return Demo2TreeTransitionResult.InvalidState;
            }

            Phase = target;
            return Demo2TreeTransitionResult.Succeeded;
        }
    }
}
