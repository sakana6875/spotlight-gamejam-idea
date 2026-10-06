using System.Collections.Generic;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Application
{
    public enum Act3Stage { WalkBack = 0, InCrowd = 1, Fainting = 2, Done = 3 }

    /// <summary>
    /// 第三幕「追寻」用例编排：从大树反向走回看客区域。
    /// 与第二幕的差别（需求）：
    ///  - 操控女孩，可慢跑，速度较上一幕稍快
    ///  - 女孩能看见未点亮平台的微弱轮廓（可见性由适配层按身份配置）
    ///  - 隐藏路线 B 开放：M8→B1→B2→L1(按 E 点亮石碑)→B2→M4(高处坠落单向)
    ///  - 沙地天亮、旧脚印变淡、新脚印更清晰
    ///  - 看客无视女孩（不回避不追逐），人群中迷失 → 晕倒 → 第四幕
    /// </summary>
    public sealed class Act3Session
    {
        private readonly IEventBus bus;
        private readonly IDemoSceneFlow flow;
        private readonly IDemo3RunFlags flags;

        public float RunSpeed = 3.4f;           // 比第二幕 2.2 快
        public float JumpPlanarSpeed = 6.5f;
        public float FaintDelay = 8f;           // 在无视人群中迷失所需时间
        public string TrailAreaId = "sand";

        public Act3Stage Stage { get; private set; } = Act3Stage.WalkBack;
        public ActWalkSession Player { get; }
        public float CrowdTimer { get; private set; }

        private readonly List<JumpZoneData> zones = new List<JumpZoneData>();
        private Rect crowdZone;
        private bool jumping;
        private float jumpT;
        private Vector2 jumpFrom, jumpTo;

        public bool Jumping => jumping;
        public float JumpT => jumpT;
        public Vector2 JumpFrom => jumpFrom;
        public Vector2 JumpTo => jumpTo;

        public Act3Session(Vector2 girlStart, IEventBus bus, IDemoSceneFlow flow, IDemo3RunFlags flags)
        {
            this.bus = bus; this.flow = flow; this.flags = flags;
            Player = new ActWalkSession(girlStart, RunSpeed);
            Player.Trails = new TrailRecorder(0.7f, TrailKind.GirlFootprint) { StrengthScale = 1.2f }; // 新脚印更清晰
        }

        public void SetJumpZones(IEnumerable<JumpZoneData> z) { zones.Clear(); zones.AddRange(z); }
        public void SetCrowdZone(Rect r) => crowdZone = r;

        public bool TryBeginJump()
        {
            if (jumping || Stage != Act3Stage.WalkBack || Player.ControlLocked) return false;
            for (int i = 0; i < zones.Count; i++)
            {
                if (!zones[i].Contains(Player.Position)) continue;
                jumpFrom = Player.Position;
                jumpTo = zones[i].Landing;
                jumping = true; jumpT = 0f;
                Player.ControlLocked = true;
                bus.Publish(new JumpStartedEvent(jumpFrom, jumpTo, true));
                return true;
            }
            return false;
        }

        /// <summary>E 键交互（石碑等）。由适配层探测到可交互物后调用。</summary>
        public void NotifyTabletInteracted(string tabletId)
        {
            if (flags == null || flags.IsLit(tabletId)) return;
            flags.MarkLit(tabletId);
            bus.Publish(new TabletLitEvent(tabletId));
        }

        public void Tick(float dt, Vector2 move, float cameraFloorY)
        {
            if (Stage == Act3Stage.Done || Stage == Act3Stage.Fainting) return;

            if (jumping)
            {
                float dist = Vector2.Distance(jumpFrom, jumpTo);
                jumpT += dt / Mathf.Max(0.05f, JumpTrajectory.Duration(dist, JumpPlanarSpeed));
                if (jumpT >= 1f)
                {
                    jumping = false;
                    Player.ControlLocked = false;
                    Player.Teleport(jumpTo);
                    bus.Publish(new JumpLandedEvent(jumpTo, true));
                }
                return;
            }

            Player.DynamicFloorY = cameraFloorY;
            Player.Tick(dt, move);

            // 看客区域：人群无视，迷失计时
            if (Stage == Act3Stage.WalkBack && crowdZone.Contains(Player.Position))
            {
                Stage = Act3Stage.InCrowd;
            }
            if (Stage == Act3Stage.InCrowd)
            {
                CrowdTimer += dt;
                if (CrowdTimer >= FaintDelay)
                {
                    Stage = Act3Stage.Fainting;
                    Player.ControlLocked = true;
                    bus.Publish(new GirlFaintedEvent());
                }
            }
        }

        /// <summary>晕倒演出播完（导演脚本回调）→ 第四幕</summary>
        public void NotifyFaintFinished()
        {
            if (Stage != Act3Stage.Fainting) return;
            Stage = Act3Stage.Done;
            flags?.SaveOldMarks(TrailAreaId, new List<TrailMark>(Player.Trails.Marks));
            flow?.GoTo(Demo3SceneIds.Act4Reenact);
        }
    }
}
