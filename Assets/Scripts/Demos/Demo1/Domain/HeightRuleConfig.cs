using System;

namespace Spotlight.Demos.Demo1.Domain
{
    /// <summary>
    /// Demo1 高度规则配置。
    /// 最高可达高度 = BaseHeight - GoodCount * GoodPenalty - MissCount * MissPenalty。
    /// 最终高度 = Lerp(MinHeight, 最高可达高度, Score / MaxScore)。
    /// 单位 m，仅用于展示，不对应场景世界坐标。
    /// </summary>
    public readonly struct HeightRuleConfig
    {
        /// <summary>全 Perfect 时可达到的最高高度。</summary>
        public readonly float BaseHeight;

        /// <summary>全 Miss 时的保底高度。</summary>
        public readonly float MinHeight;

        /// <summary>每个 Good 扣减的高度。</summary>
        public readonly float GoodPenalty;

        /// <summary>每个 Miss 扣减的高度。</summary>
        public readonly float MissPenalty;

        public HeightRuleConfig(float baseHeight, float minHeight, float goodPenalty, float missPenalty)
        {
            if (baseHeight < minHeight)
            {
                throw new ArgumentException("BaseHeight 必须大于等于 MinHeight。");
            }
            if (goodPenalty < 0f || missPenalty < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(goodPenalty), "扣减量不能为负。");
            }
            BaseHeight = baseHeight;
            MinHeight = minHeight;
            GoodPenalty = goodPenalty;
            MissPenalty = missPenalty;
        }
    }
}