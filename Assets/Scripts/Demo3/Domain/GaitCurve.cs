using System;

namespace Healing.Demo3.Domain
{
    /// <summary>某一压迫值下的步态快照：速度倍率 / 操作响应时间 / 姿态</summary>
    public readonly struct GaitFrame
    {
        public readonly float SpeedMultiplier;  // 0~1，乘在基础速度上
        public readonly float ResponseSeconds;  // 输入低通时间常数，越大越"迟钝沉重"
        public readonly Demo3Posture Posture;

        public GaitFrame(float speedMultiplier, float responseSeconds, Demo3Posture posture)
        {
            SpeedMultiplier = speedMultiplier;
            ResponseSeconds = responseSeconds;
            Posture = posture;
        }
    }

    /// <summary>
    /// 压迫迟钝机制的数值表（需求文档参考参数的代码化）：
    /// 压迫 0.0→速度100% 正常；0.3→90% 轻微低头；0.6→75% 捂耳；
    /// 0.8→55% 蜷缩 转向迟缓；1.0→0 跪倒。
    /// "操作迟钝"的实现方式：ResponseSeconds 作为输入低通滤波的时间常数，
    /// 数值越大，实际移动向量追上玩家输入越慢（详见 Demo3Session.Tick）。
    /// </summary>
    public static class GaitCurve
    {
        private struct Key
        {
            public float P; public float Speed; public float Resp; public Demo3Posture Posture;
            public Key(float p, float speed, float resp, Demo3Posture posture)
            { P = p; Speed = speed; Resp = resp; Posture = posture; }
        }

        private static readonly Key[] Keys =
        {
            new Key(0.0f, 1.00f, 0.05f, Demo3Posture.Normal),
            new Key(0.3f, 0.90f, 0.10f, Demo3Posture.SlightBow),
            new Key(0.6f, 0.75f, 0.22f, Demo3Posture.CoverEars),
            new Key(0.8f, 0.55f, 0.38f, Demo3Posture.Crouch),
            new Key(1.0f, 0.00f, 0.65f, Demo3Posture.Kneel),
        };

        public static GaitFrame Evaluate(float p)
        {
            p = Clamp01(p);
            for (int i = 0; i < Keys.Length - 1; i++)
            {
                var a = Keys[i];
                var b = Keys[i + 1];
                if (p <= b.P)
                {
                    float t = (p - a.P) / (b.P - a.P);
                    // 姿态取区间左端点的姿态（阶梯跳变），数值做线性插值
                    return new GaitFrame(
                        Lerp(a.Speed, b.Speed, t),
                        Lerp(a.Resp, b.Resp, t),
                        a.Posture);
                }
            }
            var last = Keys[Keys.Length - 1];
            return new GaitFrame(last.Speed, last.Resp, last.Posture);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
