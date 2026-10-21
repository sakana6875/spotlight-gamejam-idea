using System;
using System.Collections.Generic;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Application
{
    /// <summary>
    /// 人群模拟（应用层用例）：生成、状态解算、拥挤分离、回避圈硬约束。
    /// 不持有任何 Unity 组件引用；输入输出都是 Vector2 等纯数据。
    /// 性能要点：NPC 上限 ~240，分离用均匀网格，O(n) 均摊。
    /// </summary>
    public sealed class CrowdSimulation
    {
        private readonly Demo3TuningData t;
        private readonly List<CrowdAgent> agents = new List<CrowdAgent>(256);
        private readonly List<Vector2> frameStartPos = new List<Vector2>(256); // 本帧起始位置（测算"被挡住"用）
        private readonly System.Random rng = new System.Random();
        private float elapsed;
        private int nextId;

        public IReadOnlyList<CrowdAgent> Agents => agents;
        public int Count => agents.Count;
        public float Elapsed => elapsed;

        // ---- 场地边界（由场景根按初始镜头视野设置；默认近似无限大）----
        // 看客被挤压到此边界后不再后退（需求：挤到一定区域后停住，允许被女孩逼到角落）。
        public Vector2 BoundsMin = new Vector2(-10000f, -10000f);
        public Vector2 BoundsMax = new Vector2(10000f, 10000f);

        public CrowdSimulation(Demo3TuningData tuning) { t = tuning; }

        public void Tick(float dt, Vector2 playerPos, Vector2 playerVel,
            Vector2 viewMin, Vector2 viewMax, float pressure)
        {
            elapsed += dt;
            SpawnTowardTarget(playerPos, viewMin, viewMax);

            var ctx = new CrowdFrameContext(playerPos, playerVel, viewMin, viewMax, pressure, t);
            // 压迫迟钝的牵连：NPC 转向响应也变慢
            float turnResponse = Mathf.Max(0.01f, t.TurnResponse * (1f + pressure));
            float maxSpeed = t.PlayerBaseSpeed * t.AvoidSpeedMul * 1.5f;
            float maxDelta = (maxSpeed / turnResponse) * dt;

            frameStartPos.Clear();
            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i];
                frameStartPos.Add(a.Position);
                float dist = Vector2.Distance(a.Position, playerPos);
                a.State = CrowdSteering.ResolveState(a.State, dist, t);
                Vector2 desired = CrowdSteering.DesiredVelocity(a, ctx);
                a.Velocity = Vector2.MoveTowards(a.Velocity, desired, maxDelta);
                a.Position += a.Velocity * dt;
                agents[i] = a;
            }

            Separate(dt);
            HardClampOutsideAvoidCircle(playerPos);
            ClampInsideBounds(); // 最后执行：场地边界优先，被逼到角落的看客可以暂时停留在回避圈内
            PauseBlockedAgents(dt);
        }

        // ---- 生成：由快到慢地填满（easeOutCubic，前段更急 → 集结更快），从四周视野外生成 ----
        private void SpawnTowardTarget(Vector2 playerPos, Vector2 viewMin, Vector2 viewMax)
        {
            float k = t.SpawnFullTime <= 0f ? 1f : Mathf.Clamp01(elapsed / t.SpawnFullTime);
            float inv = 1f - k;
            float eased = 1f - inv * inv * inv;
            int target = Mathf.RoundToInt(t.MaxCrowd * eased);
            while (agents.Count < target) SpawnOneOutsideView(playerPos, viewMin, viewMax);
        }

        private void SpawnOneOutsideView(Vector2 playerPos, Vector2 viewMin, Vector2 viewMax)
        {
            float m = t.SpawnMargin;
            int side = rng.Next(4);
            float x, y;
            switch (side)
            {
                case 0: x = viewMin.x - m; y = Mathf.Lerp(viewMin.y, viewMax.y, (float)rng.NextDouble()); break;
                case 1: x = viewMax.x + m; y = Mathf.Lerp(viewMin.y, viewMax.y, (float)rng.NextDouble()); break;
                case 2: y = viewMin.y - m; x = Mathf.Lerp(viewMin.x, viewMax.x, (float)rng.NextDouble()); break;
                default: y = viewMax.y + m; x = Mathf.Lerp(viewMin.x, viewMax.x, (float)rng.NextDouble()); break;
            }
            // 出生点限制在场地边界内：避免生成瞬间被边界钳制导致"瞬移"入镜
            x = Mathf.Clamp(x, BoundsMin.x, BoundsMax.x);
            y = Mathf.Clamp(y, BoundsMin.y, BoundsMax.y);
            agents.Add(new CrowdAgent
            {
                Id = nextId++,
                Position = new Vector2(x, y),
                Velocity = Vector2.zero,
                State = CrowdNpcState.Gather,
                SideSign = rng.Next(2) == 0 ? -1f : 1f,
                Variant = (byte)rng.Next(2), // 男/女看客各占一半
            });
        }

        // ---- 拥挤分离：允许回避圈外拥挤推搡，但谁也不得进入回避圈 ----
        private void Separate(float dt)
        {
            float cell = Mathf.Max(0.1f, t.SeparationRadius);
            var grid = new Dictionary<(int, int), List<int>>(agents.Count);
            for (int i = 0; i < agents.Count; i++)
            {
                var key = ((int)Mathf.Floor(agents[i].Position.x / cell),
                           (int)Mathf.Floor(agents[i].Position.y / cell));
                if (!grid.TryGetValue(key, out var list)) { list = new List<int>(8); grid[key] = list; }
                list.Add(i);
            }

            float r2 = t.SeparationRadius * t.SeparationRadius;
            float push = t.SeparationPush * dt;
            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i];
                var key = ((int)Mathf.Floor(a.Position.x / cell), (int)Mathf.Floor(a.Position.y / cell));
                for (int gx = key.Item1 - 1; gx <= key.Item1 + 1; gx++)
                for (int gy = key.Item2 - 1; gy <= key.Item2 + 1; gy++)
                {
                    if (!grid.TryGetValue((gx, gy), out var list)) continue;
                    foreach (int j in list)
                    {
                        if (j <= i) continue;
                        var b = agents[j];
                        Vector2 d = a.Position - b.Position;
                        float sqr = d.sqrMagnitude;
                        if (sqr >= r2 || sqr < 1e-6f) continue;
                        float dist = Mathf.Sqrt(sqr);
                        Vector2 corr = d / dist * ((t.SeparationRadius - dist) * 0.5f * Mathf.Clamp01(push));
                        a.Position += corr;
                        b.Position -= corr;
                        agents[j] = b;
                    }
                }
                agents[i] = a;
            }
        }

        private void HardClampOutsideAvoidCircle(Vector2 playerPos)
        {
            float min = t.AvoidRadius * 0.98f;
            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i];
                Vector2 d = a.Position - playerPos;
                float dist = d.magnitude;
                if (dist < min)
                {
                    a.Position = playerPos + (dist > 1e-4f ? d / dist : Vector2.up) * min;
                    agents[i] = a;
                }
            }
        }

        // ---- 场地边界：被挤压到边界后不再后退 ----
        // 位置钳制进边界，并消去指向界外的速度分量（沿边界滑行而非卡住不动）；
        // 与回避圈冲突时边界优先（女孩可以把看客逼到角落，看客不再后退）。
        private void ClampInsideBounds()
        {
            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i];
                float cx = Mathf.Clamp(a.Position.x, BoundsMin.x, BoundsMax.x);
                float cy = Mathf.Clamp(a.Position.y, BoundsMin.y, BoundsMax.y);
                if (Mathf.Approximately(cx, a.Position.x) && Mathf.Approximately(cy, a.Position.y)) continue;
                if (cx != a.Position.x)
                {
                    if ((cx <= BoundsMin.x && a.Velocity.x < 0f) || (cx >= BoundsMax.x && a.Velocity.x > 0f))
                        a.Velocity.x = 0f;
                    a.Position.x = cx;
                }
                if (cy != a.Position.y)
                {
                    if ((cy <= BoundsMin.y && a.Velocity.y < 0f) || (cy >= BoundsMax.y && a.Velocity.y > 0f))
                        a.Velocity.y = 0f;
                    a.Position.y = cy;
                }
                agents[i] = a;
            }
        }

        // ---- 被挡住就停下 ----
        // 对比本帧"想走"的速度与"实际位移"：实际位移不足 25% 说明被邻居/边界/回避圈堵住，
        // 把速度清零（表现为停下脚步站立），疏通后会自然恢复行走。
        private void PauseBlockedAgents(float dt)
        {
            if (dt <= 1e-5f) return;
            float invDt = 1f / dt;
            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i];
                float want = a.Velocity.magnitude;
                if (want < 0.05f) continue; // 本来就没打算动
                float moved = (a.Position - frameStartPos[i]).magnitude * invDt;
                if (moved < want * 0.25f)
                {
                    a.Velocity = Vector2.zero;
                    agents[i] = a;
                }
            }
        }

        // ---- 检查点恢复：瞬间在回避圈外重建指定数量的人群 ----
        public void RestoreCrowd(int count, Vector2 playerPos, Vector2 viewMin, Vector2 viewMax)
        {
            agents.Clear();
            elapsed = 0f;
            float ring = t.AvoidRadius + 0.5f;
            for (int i = 0; i < count; i++)
            {
                float ang = (float)(rng.NextDouble() * Math.PI * 2.0);
                float radius = ring + (float)rng.NextDouble() * (t.GatherOuterRadius - ring);
                Vector2 pos = playerPos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
                pos.x = Mathf.Clamp(pos.x, BoundsMin.x, BoundsMax.x);
                pos.y = Mathf.Clamp(pos.y, BoundsMin.y, BoundsMax.y);
                agents.Add(new CrowdAgent
                {
                    Id = nextId++,
                    Position = pos,
                    Velocity = Vector2.zero,
                    State = CrowdNpcState.Gather,
                    SideSign = rng.Next(2) == 0 ? -1f : 1f,
                    Variant = (byte)rng.Next(2),
                });
            }
        }
    }
}
