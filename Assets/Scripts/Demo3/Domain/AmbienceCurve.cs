namespace Healing.Demo3.Domain
{
    /// <summary>
    /// 压迫值 → 环境音频混合的纯函数。
    /// 对应需求对照表：
    ///  0.0  人群嘈杂声主导
    ///  0.3  嘈杂降低、脚步声增大
    ///  0.6  嘈杂退为背景、脚步主导、心跳增大
    ///  0.8  脚步降低、心跳主导、嘈杂保持背景
    ///  1.0  心跳绝对主导，其余为低背景音，小女孩无脚步声
    /// </summary>
    public static class AmbienceCurve
    {
        public static Demo3AudioMix Evaluate(float p, float girlSpeed01)
        {
            p = Clamp01(p);
            return new Demo3AudioMix
            {
                CrowdNoise = Piecewise(p, 0.0f, 0.50f,  0.3f, 0.90f,  0.6f, 0.50f,  1.0f, 0.12f),
                CrowdSteps = Piecewise(p, 0.0f, 0.00f,  0.3f, 0.60f,  0.6f, 1.00f,  1.0f, 0.15f),
                Heartbeat  = Piecewise(p, 0.0f, 0.00f,  0.3f, 0.00f,  0.8f, 0.70f,  1.0f, 1.00f),
                // 小女孩脚步声与实际移动量挂钩；压迫 1.0 时速度为 0，自然静音
                GirlSteps  = Clamp01(girlSpeed01) * (1f - 0.6f * p),
            };
        }

        /// <summary>四段折线：(x0,y0)-(x1,y1)-(x2,y2)-(x3,y3)</summary>
        private static float Piecewise(float x,
            float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3)
        {
            if (x <= x1) return Seg(x, x0, y0, x1, y1);
            if (x <= x2) return Seg(x, x1, y1, x2, y2);
            return Seg(x, x2, y2, x3, y3);
        }

        private static float Seg(float x, float xa, float ya, float xb, float yb)
            => ya + (yb - ya) * Clamp01((x - xa) / (xb - xa));

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
