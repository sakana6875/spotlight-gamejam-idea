using Healing.Demo3.Adapters;
using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Healing.Demo3.Tests
{
    public class PressureModelTests
    {
        private static Demo3TuningData Tuning() => new Demo3TuningData();

        [Test]
        public void Pressure_IncreasesWithTime()
        {
            var p = new PressureModel(Tuning());
            p.Tick(1f, 0);
            Assert.Greater(p.Value, 0f);
        }

        [Test]
        public void Pressure_GrowsFasterWithMoreCrowd()
        {
            var a = new PressureModel(Tuning());
            var b = new PressureModel(Tuning());
            for (int i = 0; i < 10; i++) { a.Tick(0.1f, 0); b.Tick(0.1f, 200); }
            Assert.Greater(b.Value, a.Value);
        }

        [Test]
        public void Pressure_ClampedAtOne()
        {
            var p = new PressureModel(Tuning());
            for (int i = 0; i < 100000; i++) p.Tick(0.1f, 500);
            Assert.LessOrEqual(p.Value, 1f);
        }
    }

    public class GaitCurveTests
    {
        [Test]
        public void Gait_MatchesRequirementTable()
        {
            Assert.AreEqual(1.00f, GaitCurve.Evaluate(0.0f).SpeedMultiplier, 1e-3f);
            Assert.AreEqual(0.90f, GaitCurve.Evaluate(0.3f).SpeedMultiplier, 1e-3f);
            Assert.AreEqual(0.75f, GaitCurve.Evaluate(0.6f).SpeedMultiplier, 1e-3f);
            Assert.AreEqual(0.55f, GaitCurve.Evaluate(0.8f).SpeedMultiplier, 1e-3f);
            Assert.AreEqual(0.00f, GaitCurve.Evaluate(1.0f).SpeedMultiplier, 1e-3f);
        }

        [Test]
        public void Gait_PostureSteps()
        {
            Assert.AreEqual(Demo3Posture.Normal, GaitCurve.Evaluate(0.0f).Posture);
            Assert.AreEqual(Demo3Posture.SlightBow, GaitCurve.Evaluate(0.3f).Posture);
            Assert.AreEqual(Demo3Posture.CoverEars, GaitCurve.Evaluate(0.6f).Posture);
            Assert.AreEqual(Demo3Posture.Crouch, GaitCurve.Evaluate(0.8f).Posture);
            Assert.AreEqual(Demo3Posture.Kneel, GaitCurve.Evaluate(1.0f).Posture);
        }

        [Test]
        public void Gait_ResponseGrowsWithPressure()
        {
            Assert.Greater(GaitCurve.Evaluate(0.9f).ResponseSeconds,
                           GaitCurve.Evaluate(0.1f).ResponseSeconds);
        }
    }

    public class CrowdSteeringTests
    {
        private static Demo3TuningData Tuning() => new Demo3TuningData();
        private static CrowdFrameContext Ctx(Demo3TuningData t, Vector2 player) =>
            new CrowdFrameContext(player, Vector2.zero,
                new Vector2(-10, -6), new Vector2(10, 6), 0f, t);

        [Test]
        public void InsideAvoidRadius_EntersAvoid_AndMovesAway()
        {
            var t = Tuning();
            var a = new CrowdAgent { Id = 1, Position = new Vector2(1f, 0f), SideSign = 1f };
            var ctx = Ctx(t, Vector2.zero);
            a.State = CrowdSteering.ResolveState(a.State, 1f, t);
            Assert.AreEqual(CrowdNpcState.Avoid, a.State);
            Vector2 v = CrowdSteering.DesiredVelocity(a, ctx);
            Assert.Greater(Vector2.Dot(v.normalized, Vector2.right), 0.5f); // 大体远离玩家
        }

        [Test]
        public void Hysteresis_KeepsAvoidUntilSafeRadius()
        {
            var t = Tuning();
            // 已在 Avoid 状态、距离介于 Avoid 与 Safe 之间 → 保持 Avoid
            var s = CrowdSteering.ResolveState(CrowdNpcState.Avoid, (t.AvoidRadius + t.SafeRadius) * 0.5f, t);
            Assert.AreEqual(CrowdNpcState.Avoid, s);
            // 超过 SafeRadius → 退出 Avoid
            s = CrowdSteering.ResolveState(CrowdNpcState.Avoid, t.SafeRadius + 0.2f, t);
            Assert.AreNotEqual(CrowdNpcState.Avoid, s);
        }

        [Test]
        public void FarAgent_GathersTowardBoundary_NotCenter()
        {
            var t = Tuning();
            var a = new CrowdAgent { Id = 1, Position = new Vector2(6f, 0f), SideSign = 1f };
            var ctx = Ctx(t, Vector2.zero);
            a.State = CrowdSteering.ResolveState(a.State, 6f, t);
            Assert.AreEqual(CrowdNpcState.Gather, a.State);
            Vector2 v = CrowdSteering.DesiredVelocity(a, ctx);
            // 目标点是圈边界 (AvoidRadius+0.3, 0)，所以应向左（朝边界）移动
            Assert.Less(v.x, 0f);
        }

        [Test]
        public void BeyondOuterRadius_Wanders_IdleWhenNotNearEdge()
        {
            var t = Tuning();
            var a = new CrowdAgent { Id = 1, Position = new Vector2(12f, 0f), SideSign = 1f };
            a.State = CrowdSteering.ResolveState(a.State, 12f, t);
            Assert.AreEqual(CrowdNpcState.Wander, a.State);
            // 玩家不动 → 游荡者观望
            var v = CrowdSteering.DesiredVelocity(a, Ctx(t, Vector2.zero));
            Assert.AreEqual(Vector2.zero, v);
        }
    }

    public class Demo3SessionTests
    {
        private sealed class FakeRun : IDemoRun
        {
            public DemoRunResult? Result;
            public void Report(DemoRunResult r) => Result = r;
        }

        [Test]
        public void Session_EventuallyReportsSucceeded()
        {
            var t = new Demo3TuningData { CollapseDelay = 0.1f, PressureTimeRate = 1f }; // 加速
            var run = new FakeRun();
            var bus = new LocalEventBus();
            var session = new Demo3Session(t, new NullDemo3Audio(), run, bus);

            var min = new Vector2(-20, -12); var max = new Vector2(20, 12);
            for (int i = 0; i < 6000 && run.Result == null; i++)
                session.Tick(0.016f, Vector2.up, min, max);

            Assert.AreEqual(DemoRunResult.Succeeded, run.Result);
        }

        [Test]
        public void Session_AbandonReportsAbandoned()
        {
            var run = new FakeRun();
            var session = new Demo3Session(new Demo3TuningData(), new NullDemo3Audio(), run, new LocalEventBus());
            session.Abandon();
            Assert.AreEqual(DemoRunResult.Abandoned, run.Result);
        }
    }
}
