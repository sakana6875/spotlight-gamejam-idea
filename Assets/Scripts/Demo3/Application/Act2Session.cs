using System;
using System.Collections.Generic;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Application
{
    public enum Act2Segment { Intro = 0, WalkUp = 1, Mushrooms = 2, ToTree = 3, Done = 4 }

    /// <summary>
    /// 第二幕「拯救」用例编排（应用层纯逻辑）。
    /// 段落：Intro（救援播片）→ WalkUp（黑夜上行，男孩牵女孩， trails 分两段）→
    ///       Mushrooms（蘑菇跳台 + 女孩引导）→ ToTree（女孩牵男孩到树下睡去）→ Done。
    /// 速度恒定、响应正常（区别于第一幕）。玩家=男孩，第三段切换为操控女孩。
    /// </summary>
    public sealed class Act2Session
    {
        private readonly IEventBus bus;
        private readonly IDemoSceneFlow flow;
        private readonly IDemo3RunFlags flags;

        // ---- 配置（由场景根注入的纯数据）----
        public float WalkSpeed = 2.2f;          // 牵手步行，偏慢稳健
        public float GirlLeadSpeed = 2.2f;
        public float JumpPlanarSpeed = 6f;
        public float JumpApexVisual = 1.2f;     // 视觉腾空高度（适配层换算）
        public float TrailStartY = 20f;         // 超过此高度进入"浅水沙地"段，开始留轨迹
        public float MushroomEntryY = 60f;      // 到达蘑菇段入口
        public float TreeY = 40f;               // 第三段：大树位置（相对第三段起点）
        public string TrailAreaId = "sand";

        public Act2Segment Segment { get; private set; } = Act2Segment.Intro;

        /// <summary>当前被操控的角色（WalkUp/Mushrooms=男孩，ToTree=女孩）</summary>
        public ActWalkSession Player { get; private set; }
        /// <summary>被牵手跟随者的世界坐标由适配层 HandholdFollower 平滑跟随</summary>
        public bool GirlIsLeader { get; private set; }
        public GirlGuideAI Guide { get; private set; }

        // 全程轨迹累积（跨段落，供第三幕做旧复现）
        private readonly List<TrailMark> allMarks = new List<TrailMark>();

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
            GirlIsLeader = false;
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
                    if (Player.Position.y >= TrailStartY && Player.Trails == null)
                        StartTrails();
                    if (Player.Position.y >= MushroomEntryY)
                    {
                        Segment = Act2Segment.Mushrooms;
                        Player.ControlLocked = true; // 先播女孩松手跳台的开场
                        bus.Publish(new Act2SegmentChangedEvent(Segment));
                    }
                    break;

                case Act2Segment.Mushrooms:
                    TickPlayerJump(dt);
                    Guide?.Tick(dt);
                    break;

                case Act2Segment.ToTree:
                    Player.DynamicFloorY = cameraFloorY;
                    Player.Tick(dt, move);
                    if (Player.Position.y >= TreeY)
                    {
                        Segment = Act2Segment.Done;
                        bus.Publish(new Act2SegmentChangedEvent(Segment));
                        SaveTrailsForAct3();
                        flow?.GoTo(Demo3SceneIds.Act3Pursue);
                    }
                    break;
            }
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
                // 跟随到最后一个蘑菇顶 → 进入第三段
                Segment = Act2Segment.ToTree;
                GirlIsLeader = true;                  // 换女孩牵手引导
                Player = new ActWalkSession(jumpTo, GirlLeadSpeed);
                StartGirlFootprints();
                bus.Publish(new Act2SegmentChangedEvent(Segment));
            }
        }

        private TrailRecorder MakeRecorder(TrailKind kind)
        {
            var r = new TrailRecorder(0.8f, kind);
            r.MarkPlaced += m => allMarks.Add(m);
            return r;
        }

        private void StartTrails()
        {
            // 男孩水纹（女孩的脚印记录器由场景根在同一时刻挂到跟随者身上）
            Player.Trails = MakeRecorder(TrailKind.BoyRipple);
        }

        private void StartGirlFootprints()
        {
            Player.Trails = MakeRecorder(TrailKind.GirlFootprint);
        }

        private void SaveTrailsForAct3()
        {
            flags?.SaveOldMarks(TrailAreaId, new List<TrailMark>(allMarks));
        }
    }
}
