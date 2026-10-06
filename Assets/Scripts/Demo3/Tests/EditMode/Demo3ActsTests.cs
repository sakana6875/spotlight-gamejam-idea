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
        [Test]
        public void Jump_OnlyInsideZone_AndOnlyInMushroomSegment()
        {
            var bus = new LocalEventBus();
            var s = new Act2Session(Vector2.zero,
                new List<Vector2> { Vector2.up * 65f }, bus, null, null);
            s.Player.Bounds = new Rect(-5, -5, 10, 200);
            s.MushroomEntryY = 10f; // 加速测试
            s.SetJumpZones(new[] { new JumpZoneData("z1", Vector2.up * 5f, 1f, Vector2.up * 8f) });

            // 上行段：即使人在交互区里也不能跳（段落不对）
            s.NotifyIntroFinished();
            Assert.IsFalse(s.TryBeginJump());

            // 推进到蘑菇段并完成开场
            for (int i = 0; i < 2000 && s.Segment != Act2Segment.Mushrooms; i++)
                s.Tick(0.016f, Vector2.up, float.NegativeInfinity);
            Assert.AreEqual(Act2Segment.Mushrooms, s.Segment);
            s.NotifyMushroomIntroFinished();

            // 不在交互区 → 跳不了（需求：其他位置不能跳）
            Assert.IsFalse(s.TryBeginJump());

            // 瞬移进交互区 → 可以跳
            s.Player.Teleport(Vector2.up * 5f);
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
    }
}
