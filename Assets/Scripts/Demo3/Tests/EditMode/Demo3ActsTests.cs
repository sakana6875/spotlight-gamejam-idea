using System.Collections.Generic;
using Healing.Demo3.Adapters;
using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Healing.Demo3.Tests
{
    public class JumpTrajectoryTests
    {
        [Test]
        public void Endpoints_HaveZeroHeight()
        {
            JumpTrajectory.Eval(Vector2.zero, Vector2.one * 5f, 0f, out float h0);
            JumpTrajectory.Eval(Vector2.zero, Vector2.one * 5f, 1f, out float h1);
            Assert.AreEqual(0f, h0, 1e-4f);
            Assert.AreEqual(0f, h1, 1e-4f);
        }

        [Test]
        public void Apex_AtHalfway()
        {
            JumpTrajectory.Eval(Vector2.zero, Vector2.one * 5f, 0.5f, out float h);
            Assert.AreEqual(1f, h, 1e-4f);
        }
    }

    public class EndingRulesTests
    {
        private static readonly string[] Required = { "L1", "L2" };

        [Test]
        public void AllLit_GivesEnding2()
        {
            var lit = new HashSet<string> { "L1", "L2" };
            Assert.AreEqual(Demo3Ending.IsItYou, EndingRules.Decide(Required, lit.Contains));
        }

        [Test]
        public void MissingAny_GivesEnding1()
        {
            var lit = new HashSet<string> { "L1" };
            Assert.AreEqual(Demo3Ending.StillYou, EndingRules.Decide(Required, lit.Contains));
        }

        [Test]
        public void NoneLit_GivesEnding1()
        {
            Assert.AreEqual(Demo3Ending.StillYou, EndingRules.Decide(Required, _ => false));
        }
    }

    public class ActWalkSessionTests
    {
        [Test]
        public void SideWalls_ClampHorizontal()
        {
            var s = new ActWalkSession(Vector2.zero, 3f) { Bounds = new Rect(-5, -5, 10, 100) };
            for (int i = 0; i < 100; i++) s.Tick(0.1f, Vector2.left);
            Assert.GreaterOrEqual(s.Position.x, -5f);
        }

        [Test]
        public void DynamicFloor_BlocksBacktrack()
        {
            var s = new ActWalkSession(new Vector2(0, 10f), 3f) { Bounds = new Rect(-5, -50, 10, 100) };
            s.DynamicFloorY = 8f; // 相机下沿抬到 8
            for (int i = 0; i < 100; i++) s.Tick(0.1f, Vector2.down);
            Assert.GreaterOrEqual(s.Position.y, 8f); // 无法回头
        }

        [Test]
        public void LockedControl_NoMovement()
        {
            var s = new ActWalkSession(Vector2.zero, 3f) { Bounds = new Rect(-5, -5, 10, 100) };
            s.ControlLocked = true;
            s.Tick(0.1f, Vector2.up);
            Assert.AreEqual(Vector2.zero, s.Position);
        }

        [Test]
        public void AutoStepTo_ArrivesAndStops()
        {
            var s = new ActWalkSession(Vector2.zero, 3f) { Bounds = new Rect(-5, -5, 10, 100) };
            bool arrived = false;
            for (int i = 0; i < 200 && !arrived; i++) arrived = s.AutoStepTo(Vector2.up * 2f, 0.016f);
            Assert.IsTrue(arrived);
            Assert.AreEqual((Vector2.up * 2f).x, s.Position.x, 1e-4f);
            Assert.AreEqual((Vector2.up * 2f).y, s.Position.y, 1e-4f);
            Assert.AreEqual(Vector2.zero, s.Velocity);
        }
    }

    /// <summary>第一幕动画时间轴：前段正常走路 → 后段抱头走路 → 收尾抱头静止</summary>
    public class Act1TimelineTests
    {
        private sealed class FakeRun : IDemoRun { public void Report(DemoRunResult r) { } }

        private static readonly Vector2 VMin = new Vector2(-20f, -12f);
        private static readonly Vector2 VMax = new Vector2(20f, 12f);

        private static Demo3Session Make(float coverAt, float collapseAt)
        {
            var t = new Demo3TuningData
            {
                CoverEarsStartSeconds = coverAt,
                CollapseSeconds = collapseAt,
                CollapseDelay = 10f, // 让收尾演出别在测试里跑完
            };
            return new Demo3Session(t, new NullDemo3Audio(), new FakeRun(), new LocalEventBus());
        }

        [Test]
        public void Posture_BeforeCoverTime_IsNormal()
        {
            var s = Make(0.5f, 60f);
            s.Tick(0.2f, Vector2.up, VMin, VMax);
            Assert.AreEqual(Demo3Posture.Normal, s.Gait.Posture);
        }

        [Test]
        public void Posture_AfterCoverTime_IsCoverEars()
        {
            var s = Make(0.5f, 60f);
            for (int i = 0; i < 40; i++) s.Tick(0.016f, Vector2.up, VMin, VMax); // ~0.64s
            Assert.AreEqual(Demo3Posture.CoverEars, s.Gait.Posture);
            Assert.AreEqual(Demo3Stage.Gathering, s.Stage); // 还能行动，只是抱头走路
        }

        [Test]
        public void CollapseTime_Reached_EntersCollapsing_EvenIfPressureLow()
        {
            var s = Make(0.5f, 1.2f);
            for (int i = 0; i < 100 && s.Stage == Demo3Stage.Gathering; i++)
                s.Tick(0.016f, Vector2.zero, VMin, VMax); // ~1.6s
            Assert.AreEqual(Demo3Stage.Collapsing, s.Stage);
            Assert.AreEqual(Demo3Posture.CoverEars, s.Gait.Posture); // 最终抱头静止
            Assert.Less(s.Pressure, 0.999f);                          // 由时间轴触发，而非压迫值
        }
    }

    public class GirlGuideAITests
    {
        private static GirlGuideAI Make(IEventBus bus) =>
            new GirlGuideAI(new List<Vector2> { Vector2.zero, Vector2.up * 3f, Vector2.up * 6f }, 8f, bus);

        [Test]
        public void PlayerJumpStart_MakesGirlPrepare()
        {
            var g = Make(new LocalEventBus());
            g.BeginWaiting();
            g.NotifyPlayerJumpStarted();
            Assert.AreEqual(GuideState.Preparing, g.State);
        }

        [Test]
        public void PlayerLandingOnSameTop_MakesGirlJumpAhead()
        {
            var g = Make(new LocalEventBus());
            g.BeginWaiting();
            g.NotifyPlayerLanded(Vector2.zero); // 落在女孩所在蘑菇顶
            Assert.AreEqual(GuideState.Jumping, g.State);
            for (int i = 0; i < 200; i++) g.Tick(0.016f);
            Assert.AreEqual(GuideState.WaitingAt, g.State);
            Assert.AreEqual(1, g.CurrentIndex);
        }

        [Test]
        public void PlayerLandingElsewhere_GirlWaits()
        {
            var g = Make(new LocalEventBus());
            g.BeginWaiting();
            g.NotifyPlayerLanded(new Vector2(10f, 10f)); // 落在别处
            Assert.AreEqual(GuideState.WaitingAt, g.State);
        }

        [Test]
        public void LastTop_GirlDone()
        {
            var g = Make(new LocalEventBus());
            g.BeginWaiting();
            g.NotifyPlayerLanded(Vector2.zero);
            for (int i = 0; i < 200; i++) g.Tick(0.016f);
            g.NotifyPlayerLanded(g.CurrentTop);
            for (int i = 0; i < 200; i++) g.Tick(0.016f);
            g.NotifyPlayerLanded(g.CurrentTop);
            Assert.AreEqual(GuideState.Done, g.State);
        }
    }

    public class Act2SessionTests
    {
        private static Act2Session Make(out LocalEventBus bus)
        {
            bus = new LocalEventBus();
            var s = new Act2Session(Vector2.zero,
                new List<Vector2> { Vector2.up * 65f }, bus, null, null);
            s.Player.Bounds = new Rect(-5, -5, 10, 200);
            s.MushroomEntryZone = new Rect(-5f, 8f, 10f, 4f);   // 蓝框：y 8~12
            s.MushroomGatherPoint = new Vector2(0f, 11f);       // 黄点
            return s;
        }

        [Test]
        public void EnterBlueZone_LocksControl_AndAutoWalksToGatherPoint()
        {
            var s = Make(out _);
            s.NotifyIntroFinished();
            Assert.AreEqual(Act2Segment.WalkUp, s.Segment);

            // 玩家向上走进蓝框 → 进入接管段且输入失效
            for (int i = 0; i < 2000 && s.Segment != Act2Segment.Approach; i++)
                s.Tick(0.016f, Vector2.up, float.NegativeInfinity);
            Assert.AreEqual(Act2Segment.Approach, s.Segment);
            Assert.IsTrue(s.Player.ControlLocked);

            // 接管段：即使玩家反向输入，也自动走向黄点
            for (int i = 0; i < 2000 && s.Segment != Act2Segment.Mushrooms; i++)
                s.Tick(0.016f, Vector2.down, float.NegativeInfinity);
            Assert.AreEqual(Act2Segment.Mushrooms, s.Segment);
            Assert.LessOrEqual(Vector2.Distance(s.Player.Position, s.MushroomGatherPoint), 1e-3f);
        }

        [Test]
        public void MushroomSegment_MoveInputDoesNothing_OnlySpaceJumps()
        {
            var s = Make(out _);
            s.NotifyIntroFinished();
            for (int i = 0; i < 4000 && s.Segment != Act2Segment.Mushrooms; i++)
                s.Tick(0.016f, Vector2.up, float.NegativeInfinity);
            s.NotifyMushroomIntroFinished();

            Vector2 before = s.Player.Position;
            for (int i = 0; i < 200; i++) s.Tick(0.016f, Vector2.right, float.NegativeInfinity);
            Assert.AreEqual(before, s.Player.Position); // 蘑菇段全程不可移动
        }

        [Test]
        public void Jump_OnlyInsideZone_AndOnlyInMushroomSegment()
        {
            var s = Make(out _);
            s.SetJumpZones(new[] { new JumpZoneData("z1", new Vector2(0f, 11f), 1.5f, Vector2.up * 20f) });

            // 上行段：即使人在交互区里也不能跳（段落不对）
            s.NotifyIntroFinished();
            Assert.IsFalse(s.TryBeginJump());

            // 推进到蘑菇段并完成开场
            for (int i = 0; i < 4000 && s.Segment != Act2Segment.Mushrooms; i++)
                s.Tick(0.016f, Vector2.up, float.NegativeInfinity);
            Assert.AreEqual(Act2Segment.Mushrooms, s.Segment);
            s.NotifyMushroomIntroFinished();

            // 不在交互区 → 跳不了（需求：其他位置不能跳）
            s.Player.Teleport(new Vector2(0f, 30f));
            Assert.IsFalse(s.TryBeginJump());

            // 回到交互区（黄点在圈内）→ 可以跳
            s.Player.Teleport(new Vector2(0f, 11f));
            Assert.IsTrue(s.TryBeginJump());
        }
    }

    public class Act3SessionTests
    {
        private sealed class FakeFlow : IDemoSceneFlow
        {
            public string Last;
            public void GoTo(string sceneId) => Last = sceneId;
        }

        [Test]
        public void LongStayInCrowd_Faints()
        {
            var bus = new LocalEventBus();
            var flow = new FakeFlow();
            var s = new Act3Session(Vector2.zero, bus, flow, null);
            s.Player.Bounds = new Rect(-10, -10, 20, 200);
            s.SetCrowdZone(new Rect(-5, -5, 10, 10));
            s.FaintDelay = 1f;
            bool fainted = false;
            bus.Subscribe<GirlFaintedEvent>(_ => fainted = true);
            for (int i = 0; i < 200; i++) s.Tick(0.016f, Vector2.zero, float.NegativeInfinity);
            Assert.IsTrue(fainted);
        }

        [Test]
        public void Jump_ChainOnlyFromLastLanding_BranchNeedsWire()
        {
            var bus = new LocalEventBus();
            var s = new Act3Session(new Vector2(0, 0), bus, new FakeFlow(), null);
            s.SetJumpZones(new[]
            {
                new JumpZoneData("wire-to-l1", new Vector2(20, 0), 1f, new Vector2(20, 5)) { LandingIsWalkable = true },
                new JumpZoneData("wire-l1-out", new Vector2(20, 5), 1f, new Vector2(20, 9)),
                new JumpZoneData("chain-1", new Vector2(0, 0), 1.3f, new Vector2(0, 5)),
                new JumpZoneData("chain-2", new Vector2(0, 5), 1.3f, new Vector2(0, 10)),
            });

            // 没起跳过：即使站上 chain-2 的圈也踩不动（链必须从出生点第一环开始）
            s.Player.Teleport(new Vector2(0, 5.5f));
            Assert.IsFalse(s.TryBeginJump());

            // 出生点踩 chain-1 → 落 M1；M1 上可续踩 chain-2 → 落 M2
            s.Player.Teleport(new Vector2(0, 0));
            Assert.IsTrue(s.TryBeginJump());
            for (int i = 0; i < 600 && s.Jumping; i++) s.Tick(0.016f, Vector2.zero, float.NegativeInfinity);
            Assert.AreEqual(new Vector2(0, 5), s.Player.Position);
            Assert.IsTrue(s.TryBeginJump());
            for (int i = 0; i < 600 && s.Jumping; i++) s.Tick(0.016f, Vector2.zero, float.NegativeInfinity);
            Assert.AreEqual(new Vector2(0, 10), s.Player.Position);

            // 经支线连线落 L1（可行走平台）后：主线链圈一律不响应（修：支线上被链圈吞回主线）
            s.Player.Teleport(new Vector2(20, 0));
            Assert.IsTrue(s.TryBeginJump()); // wire-to-l1
            for (int i = 0; i < 600 && s.Jumping; i++) s.Tick(0.016f, Vector2.zero, float.NegativeInfinity);
            Assert.AreEqual(new Vector2(20, 5), s.Player.Position);
            Assert.IsFalse(s.MoveLocked); // 可行走平台恢复 WASD

            s.Player.Teleport(new Vector2(0, 5.5f)); // 走进 chain-2 圈里
            Assert.IsFalse(s.TryBeginJump());        // 链锚点是 L1 落点 (20,5)，不是 chain-2 圆心

            // 支线平台上手配连线照常可用
            s.Player.Teleport(new Vector2(20, 5));
            Assert.IsTrue(s.TryBeginJump()); // wire-l1-out
        }
    }

    public class OpeningIntroTests
    {
        [Test]
        public void Opening_FallShakeRise_Flow()
        {
            var s = new OpeningIntroSession { FallSeconds = 1f, ShakesRequired = 3, ShakeInterval = 1f, RiseSeconds = 0.5f };
            int shakes = 0; s.Shook += _ => shakes++;

            // 落下期间按移动键无效
            for (int i = 0; i < 30; i++) s.Tick(0.016f, Vector2.up);
            Assert.AreEqual(OpeningStage.Falling, s.Stage);
            Assert.AreEqual(0, shakes);

            // 落完 → 摔在地上等晃动
            for (int i = 0; i < 40; i++) s.Tick(0.016f, Vector2.zero);
            Assert.AreEqual(OpeningStage.Fallen, s.Stage);

            // 第 1 次晃动（按下沿触发）
            s.Tick(0.016f, Vector2.up);
            Assert.AreEqual(1, shakes);
            s.Tick(0.016f, Vector2.zero); // 松开

            // 间隔不足 1 秒的晃动不算
            for (int i = 0; i < 30; i++) s.Tick(0.016f, Vector2.zero); // 0.48s
            s.Tick(0.016f, Vector2.up);
            Assert.AreEqual(1, shakes);
            s.Tick(0.016f, Vector2.zero);

            // 隔 1 秒以上再晃 → 算；晃满 3 次 → 起身
            for (int i = 0; i < 70; i++) s.Tick(0.016f, Vector2.zero); // 1.12s
            s.Tick(0.016f, Vector2.up); s.Tick(0.016f, Vector2.zero);
            Assert.AreEqual(2, shakes);
            for (int i = 0; i < 70; i++) s.Tick(0.016f, Vector2.zero);
            s.Tick(0.016f, Vector2.up);
            Assert.AreEqual(3, shakes);
            Assert.AreEqual(OpeningStage.Rising, s.Stage);

            // 起身演完 → Done（交还第一幕）
            for (int i = 0; i < 40; i++) s.Tick(0.016f, Vector2.zero); // 0.64s
            Assert.AreEqual(OpeningStage.Done, s.Stage);
        }

        [Test]
        public void Opening_HeldKeyCountsOnce()
        {
            var s = new OpeningIntroSession { FallSeconds = 0.1f, ShakesRequired = 3, ShakeInterval = 1f, RiseSeconds = 0.1f };
            int shakes = 0; s.Shook += _ => shakes++;
            for (int i = 0; i < 10; i++) s.Tick(0.016f, Vector2.zero); // 落完
            // 长按不松：间隔到了也只算一次（必须松开再按）
            for (int i = 0; i < 200; i++) s.Tick(0.016f, Vector2.right);
            Assert.AreEqual(1, shakes);
            Assert.AreEqual(OpeningStage.Fallen, s.Stage);
        }
    }
}
