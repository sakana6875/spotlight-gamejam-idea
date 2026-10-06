using System;

namespace Healing.Demo3.Domain
{
    /// <summary>
    /// 环境压迫值（0~1，玩家不可见）。
    /// 影响因素：游戏时间 + 周围看客数量（暴露越久、围观越多，压迫越高）。
    /// 单调递增、只进不退，是本幕"无法逃离"的规则基础。
    /// </summary>
    public sealed class PressureModel
    {
        private readonly Demo3TuningData tuning;

        public float Elapsed { get; private set; }
        public float Value   { get; private set; }

        public PressureModel(Demo3TuningData tuning)
        {
            this.tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        }

        public void Tick(float dt, int crowdCount)
        {
            if (Value >= 1f) { Value = 1f; Elapsed += dt; return; }
            Elapsed += dt;

            float crowd01 = tuning.PressureCrowdReference <= 0f
                ? 0f
                : Math.Min(1f, crowdCount / tuning.PressureCrowdReference);

            float rate = tuning.PressureTimeRate + tuning.PressureCrowdRate * crowd01;
            Value = Math.Min(1f, Value + rate * dt);
        }

        /// <summary>检查点恢复用：直接回填状态（纯数据，不含 Unity 引用）</summary>
        public void Restore(float elapsed, float value)
        {
            Elapsed = Math.Max(0f, elapsed);
            Value = Math.Min(1f, Math.Max(0f, value));
        }
    }
}
