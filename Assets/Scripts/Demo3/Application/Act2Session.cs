using System.Collections.Generic;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Application
{
    public enum Act2Segment { Intro = 0, WalkUp = 1, Approach = 2, Mushrooms = 3, Done = 5 }

    /// <summary>
    /// 第二幕「拯救」用例编排（应用层纯逻辑）。
    /// 段落：Intro（救援播片）→ WalkUp（黑夜上行，男孩牵女孩）→
    ///       Approach（走进接管区[蓝框]：系统接管，自动走入集结点[黄点]，玩家输入无效）→
    ///       Mushrooms（女孩松手跳台引导；玩家无法移动，只能站进跳跃圈按空格逐台跳）→
    ///       Done（跳上最后一朵蘑菇 → 渐黑 → 切大树场景 Act3）。
    /// 速度恒定、响应正常（区别于第一幕）。玩家=男孩。
    /// 注意：原「ToTree 女孩牵男孩到树下」段已移除——大树内容全部在 Act3 大树场景进行。
    /// </summary>
    public sealed class Act2Session
    {
        private readonly IEventBus bus;
        private readonly IDemoSceneFlow flow;
        private readonly IDemo3RunFlags flags;

        // ---- 配置（由场景根注入的纯数据）----
        public float WalkSpeed = 2.2f;          // 牵手步行，偏慢稳健
        public float JumpPlanarSpeed = 6f;
        public float JumpApexVisual = 1.2f;     // 视觉腾空高度（适配层换算）
        public Rect MushroomEntryZone = new Rect(-6f, 58f, 12f, 6f); // 接管区（蓝框）：走入即被系统接管
        public Vector2 MushroomGatherPoint = new Vector2(0f, 62f);   // 集结点（黄点）：自动走位目标

        public Act2Segment Segment { get; private set; } = Act2Segment.Intro;

        /// <summary>当前被操控的角色（全程=男孩）</summary>
        public ActWalkSession Player { get; private set; }
        public GirlGuideAI Guide { get; private set; }

        // 玩家跳跃状态
        private readonly List<JumpZoneData> zones = new List<JumpZoneData>();
        private bool playerJumping;
        private float jumpT;
        private Vector2 jumpFrom, jumpTo;

        public bool PlayerJumping => playerJumping;
        public float PlayerJumpT => jumpT;
        public Vector2 PlayerJumpFrom => jumpFrom;
        public Vector2 PlayerJumpTo => jumpTo;

        public Act2Session(Vector2 boyStart, IReadOnlyList<Vector2> mushroomTops,
            IEventBus bus, IDemoSceneFlow flow, IDemo3RunFlags flags)
        {
            this.bus = bus; this.flow = flow; this.flags = flags;
            Player = new ActWalkSession(boyStart, WalkSpeed);
            if (mushroomTops != null && mushroomTops.Count > 0)
                Guide = new GirlGuideAI(mushroomTops, JumpPlanarSpeed * 1.25f, bus); // 女孩一定比男孩快
        }

        public void SetJumpZones(IEnumerable<JumpZoneData> z) { zones.Clear(); zones.AddRange(z); }

        // ---- 段落推进（接口调用）----

        /// <summary>救援播片播完（由导演脚本回调）</summary>
        public void NotifyIntroFinished()
        {
            if (Segment != Act2Segment.Intro) return;
            Segment = Act2Segment.WalkUp;
            bus.Publish(new Act2SegmentChangedEvent(Segment));
        }

        /// <summary>蘑菇段开场播片（女孩松手跳上第一个蘑菇顶）播完</summary>
        public void NotifyMushroomIntroFinished()
        {
            Guide?.BeginWaiting();
            Player.ControlLocked = false;
        }

        // ---- 跳跃（需要结果 → 接口调用，返回是否成功起跳）----

        public bool TryBeginJump()
        {
            if (playerJumping || Segment != Act2Segment.Mushrooms || Player.ControlLocked) return false;
            for (int i = 0; i < zones.Count; i++)
            {
                if (!zones[i].Contains(Player.Position)) continue;
                jumpFrom = Player.Position;
                jumpTo = zones[i].Landing;
                playerJumping = true;
                jumpT = 0f;
                Player.ControlLocked = true;
                bus.Publish(new JumpStartedEvent(jumpFrom, jumpTo, false));
                Guide?.NotifyPlayerJumpStarted();
                return true;
            }
            return false; // 不在交互区，跳跃键无效（需求：其他位置不能跳）
        }

        // ---- 主循环 ----

        public void Tick(float dt, Vector2 move, float cameraFloorY)
        {
            switch (Segment)
            {
                case Act2Segment.Intro:
                    return; // 播片中

                case Act2Segment.WalkUp:
                    Player.DynamicFloorY = cameraFloorY;
                    Player.Tick(dt, move);
                    if (MushroomEntryZone.Contains(Player.Position))
                    {
                        // 走进接管区（蓝框）：系统接管，玩家输入即刻失效
                        Segment = Act2Segment.Approach;
                        Player.ControlLocked = true;
                        bus.Publish(new Act2SegmentChangedEvent(Segment));
                    }
                    break;

                case Act2Segment.Approach:
                    // 自动走入集结点（黄点）；到达后进入蘑菇段（场景根播"女孩松手跳台"）
                    if (Player.AutoStepTo(MushroomGatherPoint, dt))
                    {
                        Segment = Act2Segment.Mushrooms;
                        bus.Publish(new Act2SegmentChangedEvent(Segment));
                    }
                    break;

                case Act2Segment.Mushrooms:
                    TickPlayerJump(dt);
                    Guide?.Tick(dt);
                    break;
            }
        }

        /// <summary>幕末渐黑播完（场景根回调）→ 切大树场景（Act3）</summary>
        public void NotifyOutroFinished()
        {
            if (Segment != Act2Segment.Done) return;
            flow?.GoTo(Demo3SceneIds.Act3Pursue);
        }

        private void TickPlayerJump(float dt)
        {
            if (!playerJumping) return;
            float dist = Vector2.Distance(jumpFrom, jumpTo);
            jumpT += dt / Mathf.Max(0.05f, JumpTrajectory.Duration(dist, JumpPlanarSpeed));
            if (jumpT < 1f) return;
            playerJumping = false;
            Player.ControlLocked = false;
            Player.Teleport(jumpTo);
            bus.Publish(new JumpLandedEvent(jumpTo, false));
            Guide?.NotifyPlayerLanded(jumpTo);
            if (Guide != null && Guide.State == GuideState.Done)
            {
                // 跳上最后一朵蘑菇 → 本幕结束：场景根播渐黑，回调 NotifyOutroFinished 切大树场景
                Segment = Act2Segment.Done;
                Player.ControlLocked = true;
                bus.Publish(new Act2SegmentChangedEvent(Segment));
            }
        }
    }
}
