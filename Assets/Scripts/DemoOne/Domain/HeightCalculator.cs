using UnityEngine;

namespace Spotlight.Features.DemoOne.Domain
{
    /// <summary>
    /// Demo1 纯高度计算。不持有状态，可作为无状态服务使用。
    /// </summary>
    public sealed class HeightCalculator
    {
        private readonly HeightRuleConfig _config;

        public HeightCalculator(HeightRuleConfig config)
        {
            _config = config;
        }

        /// <summary>最高可达高度：全 Perfect 时为 BaseHeight，每个 Good / Miss 依次扣减。</summary>
        public float CalcMaxReachable(int goodCount, int missCount)
        {
            float raw = _config.BaseHeight
                        - goodCount * _config.GoodPenalty
                        - missCount * _config.MissPenalty;
            return raw < _config.MinHeight ? _config.MinHeight : raw;
        }

        /// <summary>
        /// 最终高度：以得分比例在 MinHeight 与最高可达高度之间线性插值。
        /// score 为 0 时返回 MinHeight，score 等于 maxScore 时返回最高可达高度。
        /// </summary>
        public float CalcFinalHeight(int score, int maxScore, int goodCount, int missCount)
        {
            float maxReachable = CalcMaxReachable(goodCount, missCount);
            float ratio = maxScore > 0 ? (float)score / maxScore : 0f;
            if (ratio < 0f) ratio = 0f;
            if (ratio > 1f) ratio = 1f;
            return Mathf.Lerp(_config.MinHeight, maxReachable, ratio);
        }

        /// <summary>完美通关高度：即 BaseHeight，用于结算面板展示。</summary>
        public float PerfectHeight => _config.BaseHeight;
    }
}