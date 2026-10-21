using System.Collections.Generic;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Application
{
    public enum Act3Stage { WalkBack = 0, InCrowd = 1, Fainting = 2, Done = 3 }

    /// <summary>
    /// 女孩独行幕（Act2-1 追寻 / Act4 看客 / Act2-2 重演）用例编排。
    /// 与第二幕的差别（需求）：
    ///  - 操控女孩，可慢跑，速度较上一幕稍快
    ///  - 蘑菇垫固定动线：落地锁定移动，只能按跳跃键沿跳区链前进（MoveLocked）；
    ///    落到标记 LandingIsWalkable 的平台（如 L1）才恢复 WASD，且活动范围钳到平台
    ///  - 女孩能看见未点亮平台的微弱轮廓（可见性由适配层按身份配置）
    ///  - 石碑交互（L1 按 E 传送隐藏区——传送由场景根执行，会话只记账）
    ///  - 可选看客无视人群：迷失计时只进不退 → 晕倒 → 切 FaintGoToSceneId
    /// </summary>
    public sealed class Act3Session
    {
        private readonly IEventBus bus;
        private readonly IDemoSceneFlow flow;
        private readonly IDemo3RunFlags flags;

        public float RunSpeed = 3.4f;           // 比第二幕 2.2 快
        public float JumpPlanarSpeed = 6.5f;
        public float FaintDelay = 8f;           // 在无视人群中迷失所需时间
        /// <summary>晕倒演出播完后切入的场景（由场景根配置；默认 Act2-2 重演蘑菇）</summary>
        public string FaintGoToSceneId = Demo3SceneIds.Act2Reenact;

        public Act3Stage Stage { get; private set; } = Act3Stage.WalkBack;
        public ActWalkSession Player { get; }
        public float CrowdTimer { get; private set; }

        /// <summary>移动锁定：跳上蘑菇垫后为 true（只能跳，WASD 无效）；落到可行走平台/地面为 false</summary>
        public bool MoveLocked { get; private set; }

        private readonly List<JumpZoneData> zones = new List<JumpZoneData>();
        private readonly Vector2 startPos;       // 出生点（自动链第一环的圆心）
        private Rect crowdZone;
        private Rect fullBounds;                 // 全场活动范围（落到可行走平台时会临时换成平台范围）
        private bool hasFullBounds;
        private JumpZoneData? currentZone;       // 当前这一跳使用的跳区（落地后按它恢复移动状态；null=尚未起跳）
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
            startPos = girlStart;
            Player = new ActWalkSession(girlStart, RunSpeed);
        }

        public void SetJumpZones(IEnumerable<JumpZoneData> z) { zones.Clear(); zones.AddRange(z); }
        public void SetCrowdZone(Rect r) => crowdZone = r;

        /// <summary>全场活动范围（场景根由拖入的碰撞体换算后设置）</summary>
        public void SetWalkBounds(Rect r) { fullBounds = r; hasFullBounds = true; Player.Bounds = r; }

        /// <summary>强制进入"可行走"状态并钳到指定范围（从隐藏区返回 L1 平台时用；宽高为 0 则用全场范围）</summary>
        public void ForceFreeMove(Rect walkArea)
        {
            MoveLocked = false;
            Player.ControlLocked = false;
            Player.Bounds = walkArea.width > 0f && walkArea.height > 0f ? walkArea
                          : hasFullBounds ? fullBounds : Player.Bounds;
        }

        /// <summary>当前是否处于可按跳状态（供适配层区分「按跳没反应」是锁定中还是不在任何跳区）</summary>
        public bool CanBeginJump => !jumping && Stage == Act3Stage.WalkBack && !Player.ControlLocked;

        public bool TryBeginJump()
        {
            if (!CanBeginJump) return false;
            for (int i = 0; i < zones.Count; i++)
            {
                if (!zones[i].Contains(Player.Position)) continue;
                // 自动链严格续链：链圈只在「圆心 == 当前立足点」时可踩——
                // 没起跳过就只认出生点引出的第一环，起跳过后只认上一落点引出的下一环；
                // 支线/可行走平台（经手配连线到达）上链圈一律不响应，支线必须走手配连线。
                // 否则站在支线平台上会被旁边的主线链圈（半径较大）吞掉起跳、直接跳回主线。
                if (IsChainZone(zones[i]) && !ChainContinuityOk(zones[i])) continue;
                currentZone = zones[i];
                jumpFrom = Player.Position;
                jumpTo = zones[i].Landing;
                jumping = true; jumpT = 0f;
                Player.ControlLocked = true;
                bus.Publish(new JumpStartedEvent(jumpFrom, jumpTo, true));
                return true;
            }
            return false;
        }

        private static bool IsChainZone(JumpZoneData z) => z.Id != null && z.Id.StartsWith("chain-");

        private bool ChainContinuityOk(JumpZoneData z)
        {
            Vector2 anchor = currentZone.HasValue ? currentZone.Value.Landing : startPos;
            return ((Vector2)z.Center - anchor).sqrMagnitude <= 0.04f; // 0.2 米容差（浮点噪声）
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
                    // 落地恢复移动状态：可行走平台（L1）恢复 WASD 并钳到平台范围；
                    // 蘑菇垫锁移动（只能继续跳，固定动线）
                    JumpZoneData cz = currentZone.Value; // 起跳时必选过区，这里不为空
                    MoveLocked = !cz.LandingIsWalkable;
                    Player.Bounds = cz.LandingIsWalkable
                        ? (cz.LandingWalkArea.width > 0f && cz.LandingWalkArea.height > 0f
                            ? cz.LandingWalkArea
                            : hasFullBounds ? fullBounds : Player.Bounds)
                        : Player.Bounds;
                    bus.Publish(new JumpLandedEvent(jumpTo, true));
                }
                return;
            }

            Player.DynamicFloorY = cameraFloorY;
            Player.Tick(dt, MoveLocked ? Vector2.zero : move); // 蘑菇垫上：WASD 无效，只能跳

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

        /// <summary>晕倒演出播完（导演脚本回调）→ 切 FaintGoToSceneId 配置的场景</summary>
        public void NotifyFaintFinished()
        {
            if (Stage != Act3Stage.Fainting) return;
            Stage = Act3Stage.Done;
            flow?.GoTo(FaintGoToSceneId);
        }
    }
}
