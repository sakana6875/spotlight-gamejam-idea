using Healing.Demo3.Adapters;
using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Healing.Demo3.Tests
{
    /// <summary>本次迭代：人群边界、男女形象分配、玩家镜头禁锢</summary>
    public class CrowdBoundsAndVariantTests
    {
        private static Demo3TuningData Tuning() => new Demo3TuningData();

        [Test]
        public void Spawn_AssignsBothMaleAndFemaleVariants()
        {
            var sim = new CrowdSimulation(Tuning());
            var min = new Vector2(-20, -12); var max = new Vector2(20, 12);
            for (int i = 0; i < 600; i++) // 10 秒，足够生成一大批
                sim.Tick(0.016f, Vector2.zero, Vector2.zero, min, max, 0f);

            bool hasMale = false, hasFemale = false;
            foreach (var a in sim.Agents)
            {
                if (a.Variant == 0) hasMale = true;
                if (a.Variant == 1) hasFemale = true;
            }
            Assert.IsTrue(hasMale, "应生成男看客");
            Assert.IsTrue(hasFemale, "应生成女看客");
        }

        [Test]
        public void Crowd_StopsRetreatingAtStageBounds()
        {
            var sim = new CrowdSimulation(Tuning());
            // 小场地：以原点为中心 8×5
            sim.BoundsMin = new Vector2(-4f, -2.5f);
            sim.BoundsMax = new Vector2(4f, 2.5f);
            var min = new Vector2(-12, -7); var max = new Vector2(12, 7);

            // 玩家贴着右边界站桩，把看客往边界外挤
            var player = new Vector2(3.2f, 0f);
            for (int i = 0; i < 3000; i++)
                sim.Tick(0.016f, player, Vector2.zero, min, max, 0f);

            Assert.Greater(sim.Count, 50, "应先聚集起足够人群");
            foreach (var a in sim.Agents)
            {
                Assert.LessOrEqual(a.Position.x, sim.BoundsMax.x + 1e-3f, "看客不得越过右边界");
                Assert.GreaterOrEqual(a.Position.x, sim.BoundsMin.x - 1e-3f);
                Assert.LessOrEqual(a.Position.y, sim.BoundsMax.y + 1e-3f);
                Assert.GreaterOrEqual(a.Position.y, sim.BoundsMin.y - 1e-3f);
            }
        }

        [Test]
        public void SpawnPositions_StayInsideStageBounds()
        {
            var sim = new CrowdSimulation(Tuning());
            sim.BoundsMin = new Vector2(-4f, -2.5f);
            sim.BoundsMax = new Vector2(4f, 2.5f);
            var min = new Vector2(-12, -7); var max = new Vector2(12, 7);
            sim.Tick(0.016f, Vector2.zero, Vector2.zero, min, max, 0f); // 第一帧即生成一批
            foreach (var a in sim.Agents)
            {
                Assert.LessOrEqual(a.Position.x, sim.BoundsMax.x + 1e-3f);
                Assert.GreaterOrEqual(a.Position.x, sim.BoundsMin.x - 1e-3f);
            }
        }

        [Test]
        public void SpawnCurve_FrontLoadsGathering()
        {
            // easeOutCubic：时间过半时应已完成 87.5% 的生成（比 easeOutQuad 的 75% 更急）
            var sim = new CrowdSimulation(new Demo3TuningData { MaxCrowd = 400, SpawnFullTime = 40f });
            var min = new Vector2(-20, -12); var max = new Vector2(20, 12);
            for (int i = 0; i < 1250; i++) // 20 秒（一半时长）
                sim.Tick(0.016f, Vector2.zero, Vector2.zero, min, max, 0f);
            Assert.GreaterOrEqual(sim.Count, 340, "集结前段应明显更快");
        }
    }

    public class PlayerConfinementTests
    {
        private sealed class FakeRun : IDemoRun
        {
            public DemoRunResult? Result;
            public void Report(DemoRunResult r) => Result = r;
        }

        [Test]
        public void Girl_CannotWalkOutOfCameraBounds()
        {
            var session = new Demo3Session(new Demo3TuningData(),
                new NullDemo3Audio(), new FakeRun(), new LocalEventBus());
            session.PlayerBounds = new Rect(-3f, -2f, 6f, 4f); // 模拟镜头可视区域

            var min = new Vector2(-20, -12); var max = new Vector2(20, 12);
            for (int i = 0; i < 3000; i++) // 一直向右下走
                session.Tick(0.016f, new Vector2(1f, -1f), min, max);

            Assert.LessOrEqual(session.PlayerPosition.x, 3f + 1e-3f);
            Assert.GreaterOrEqual(session.PlayerPosition.y, -2f - 1e-3f);
        }
    }
}
