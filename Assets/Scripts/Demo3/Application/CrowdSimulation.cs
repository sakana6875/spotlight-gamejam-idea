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
        private readonly System.Random rng = new System.Random();
        private float elapsed;
        private int nextId;

        public IReadOnlyList<CrowdAgent> Agents => agents;
        public int Count => agents.Count;
        public float Elapsed => elapsed;

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

            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i];
                float dist = Vector2.Distance(a.Position, playerPos);
                a.State = CrowdSteering.ResolveState(a.State, dist, t);
                Vector2 desired = CrowdSteering.DesiredVelocity(a, ctx);
                a.Velocity = Vector2.MoveTowards(a.Velocity, desired, maxDelta);
                a.Position += a.Velocity * dt;
                agents[i] = a;
            }

            Separate(dt);
            HardClampOutsideAvoidCircle(playerPos);
        }

        // ---- 生成：由快到慢地填满（easeOutQuad），从四周视野外生成 ----
        private void SpawnTowardTarget(Vector2 playerPos, Vector2 viewMin, Vector2 viewMax)
        {
            float k = t.SpawnFullTime <= 0f ? 1f : Mathf.Clamp01(elapsed / t.SpawnFullTime);
            float eased = 1f - (1f - k) * (1f - k);
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
            agents.Add(new CrowdAgent
            {
                Id = nextId++,
                Position = new Vector2(x, y),
                Velocity = Vector2.zero,
                State = CrowdNpcState.Gather,
                SideSign = rng.Next(2) == 0 ? -1f : 1f,
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
                agents.Add(new CrowdAgent
                {
                    Id = nextId++,
                    Position = playerPos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius,
                    Velocity = Vector2.zero,
                    State = CrowdNpcState.Gather,
                    SideSign = rng.Next(2) == 0 ? -1f : 1f,
                });
            }
        }
    }
}
