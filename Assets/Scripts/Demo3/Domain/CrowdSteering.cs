using UnityEngine;

namespace Healing.Demo3.Domain
{
    /// <summary>NPC 行为状态：回避 / 聚集 / 游荡</summary>
    public enum CrowdNpcState { Gather = 0, Avoid = 1, Wander = 2 }

    /// <summary>单个看客的全部运行时状态（纯数据，可被空间网格复用）</summary>
    public struct CrowdAgent
    {
        public int Id;
        public Vector2 Position;
        public Vector2 Velocity;
        public CrowdNpcState State;
        public float SideSign; // +1 / -1：侧向偏移方向（生成时随机，终身不变）
    }

    /// <summary>一帧人群解算所需的只读上下文</summary>
    public readonly struct CrowdFrameContext
    {
        public readonly Vector2 PlayerPos;
        public readonly Vector2 PlayerVel;
        public readonly Vector2 ViewMin;   // 视野（扩展后）左下角
        public readonly Vector2 ViewMax;   // 视野（扩展后）右上角
        public readonly float Pressure;    // 当前压迫值
        public readonly Demo3TuningData T;

        public CrowdFrameContext(Vector2 playerPos, Vector2 playerVel,
            Vector2 viewMin, Vector2 viewMax, float pressure, Demo3TuningData t)
        {
            PlayerPos = playerPos; PlayerVel = playerVel;
            ViewMin = viewMin; ViewMax = viewMax;
            Pressure = pressure; T = t;
        }
    }

    /// <summary>
    /// 人群回避机制的纯规则：
    ///  - 进入 AvoidRadius → 回避：远离玩家 + 少量侧向偏移
    ///    （侧向偏移实现：给每个 NPC 生成时随机分配一个 ±1 的 SideSign，
    ///      回避方向 = normalize(远离方向 + 切线方向 × TangentBias)，
    ///      切线即垂直于"玩家→NPC"连线的方向。这样 NPC 沿弧线散开而非直线四散，
    ///      即需求图中蓝色箭头的动线。）
    ///  - 在 SafeRadius 之外且能挤到圈边 → 聚集：朝回避圈边界上的最近点走（不是朝圆心）
    ///  - 被挤在 GatherOuterRadius 之外 → 游荡：默认不动；
    ///      在屏幕边沿且处于玩家行进方向前侧 → 远离回避区域并向两侧散开；
    ///      处于行进反方向后侧 → 靠近回避区域并向边沿中心收拢。
    /// </summary>
    public static class CrowdSteering
    {
        public static CrowdNpcState ResolveState(CrowdNpcState current, float distToPlayer, Demo3TuningData t)
        {
            if (distToPlayer < t.AvoidRadius) return CrowdNpcState.Avoid;
            if (current == CrowdNpcState.Avoid && distToPlayer < t.SafeRadius)
                return CrowdNpcState.Avoid; // 滞回：防止在边界上抖动切换
            if (distToPlayer > t.GatherOuterRadius) return CrowdNpcState.Wander;
            return CrowdNpcState.Gather;
        }

        public static Vector2 DesiredVelocity(in CrowdAgent a, in CrowdFrameContext c)
        {
            var t = c.T;
            Vector2 toAgent = a.Position - c.PlayerPos;
            float dist = toAgent.magnitude;
            Vector2 radial = dist > 1e-4f ? toAgent / dist : Vector2.up;

            // 压迫迟钝的牵连：压迫越高，看客逃跑/追击也越慢
            float crowdSlow = 1f - 0.5f * Mathf.Clamp01(c.Pressure);

            switch (a.State)
            {
                case CrowdNpcState.Avoid:
                {
                    Vector2 tangent = new Vector2(-radial.y, radial.x) * a.SideSign;
                    Vector2 dir = (radial + tangent * t.TangentBias).normalized;
                    // 陷入越深逃得越急，保证"玩家难以追上"
                    float depth = Mathf.InverseLerp(t.AvoidRadius, 0f, dist);
                    float speed = t.PlayerBaseSpeed * t.AvoidSpeedMul * (1f + 0.5f * depth);
                    return dir * speed * crowdSlow;
                }
                case CrowdNpcState.Gather:
                {
                    // 目标是回避圈边界上离自己最近的点，而不是玩家本人：
                    // 这样人群自然形成"围绕但保持距离"的圆环
                    Vector2 boundaryPoint = c.PlayerPos + radial * (t.AvoidRadius + 0.3f);
                    Vector2 d = boundaryPoint - a.Position;
                    if (d.sqrMagnitude < 0.04f) return Vector2.zero;
                    return d.normalized * (t.PlayerBaseSpeed * t.GatherSpeedMul) * crowdSlow;
                }
                default: // Wander
                    return WanderDesired(a, c, radial) * crowdSlow;
            }
        }

        private static Vector2 WanderDesired(in CrowdAgent a, in CrowdFrameContext c, Vector2 radial)
        {
            var t = c.T;
            float band = t.EdgeBand;
            bool nearEdge =
                a.Position.x < c.ViewMin.x + band || a.Position.x > c.ViewMax.x - band ||
                a.Position.y < c.ViewMin.y + band || a.Position.y > c.ViewMax.y - band;
            if (!nearEdge) return Vector2.zero; // 默认不主动移动

            Vector2 pdir = c.PlayerVel.sqrMagnitude > 0.04f ? c.PlayerVel.normalized : Vector2.zero;
            if (pdir == Vector2.zero) return Vector2.zero; // 玩家没动，游荡者观望

            float speed = t.PlayerBaseSpeed * t.WanderSpeedMul;
            float facing = Vector2.Dot(pdir, radial); // >0 在行进方向前侧，<0 在后侧

            if (facing > 0.25f)
            {
                // 前侧边沿：远离回避区域，同时向两侧散开
                Vector2 perp = new Vector2(-pdir.y, pdir.x);
                Vector2 side = perp * Mathf.Sign(Vector2.Dot(perp, radial));
                return (radial + side * 0.9f).normalized * speed;
            }
            if (facing < -0.25f)
            {
                // 后侧边沿：靠近回避区域，并向最近的边沿中心收拢
                Vector2 inward = -radial; // radial 已归一化，inward 亦是
                Vector2 edgeCenter = NearestEdgeCenter(a.Position, c.ViewMin, c.ViewMax);
                Vector2 toCenter = edgeCenter - a.Position;
                // Vector2 没有 Project，手动投影后取切向分量（只保留"向边沿中心收拢"的横向部分）
                Vector2 lateral = toCenter - inward * Vector2.Dot(toCenter, inward);
                if (lateral.sqrMagnitude < 1e-6f)
                    return inward * speed; // 正好正对边沿中心时直线靠近
                return (inward + lateral.normalized * 0.6f).normalized * speed;
            }
            return Vector2.zero;
        }

        private static Vector2 NearestEdgeCenter(Vector2 p, Vector2 min, Vector2 max)
        {
            float dl = p.x - min.x, dr = max.x - p.x, db = p.y - min.y, dt2 = max.y - p.y;
            float m = Mathf.Min(dl, dr, db, dt2);
            Vector2 c = (min + max) * 0.5f;
            if (m == dl) return new Vector2(min.x, c.y);
            if (m == dr) return new Vector2(max.x, c.y);
            if (m == db) return new Vector2(c.x, min.y);
            return new Vector2(c.x, max.y);
        }
    }
}
