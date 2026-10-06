using System.Collections.Generic;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Application
{
    public enum Act4Stage { WalkToTree = 0, Ending = 1, Done = 2 }

    /// <summary>
    /// 第四幕「重演」用例编排：从看客区域再次走向大树。
    /// 与第三幕的差别：方向相反（向下→向上）；C 隐藏路线开放（M5→C1→C2→M3 飞升单向）；
    /// 引导粒子由适配层按区域触发；到达大树 → EndingRules 判定结局 → 播结局演出。
    /// </summary>
    public sealed class Act4Session
    {
        private static readonly string[] RequiredTablets = { "L1", "L2" };

        private readonly IEventBus bus;
        private readonly IDemo3RunFlags flags;
        private readonly IDemoRun run;

        public float WalkSpeed = 3.4f;
        public float JumpPlanarSpeed = 6.5f;

        public Act4Stage Stage { get; private set; } = Act4Stage.WalkToTree;
        public ActWalkSession Player { get; }
        public Demo3Ending? Ending { get; private set; }

        private readonly List<JumpZoneData> zones = new List<JumpZoneData>();
        private Rect treeZone;
        private bool jumping;
        private float jumpT;
        private Vector2 jumpFrom, jumpTo;

        public bool Jumping => jumping;
        public float JumpT => jumpT;
        public Vector2 JumpFrom => jumpFrom;
        public Vector2 JumpTo => jumpTo;

        public Act4Session(Vector2 girlStart, IEventBus bus, IDemo3RunFlags flags, IDemoRun run)
        {
            this.bus = bus; this.flags = flags; this.run = run;
            Player = new ActWalkSession(girlStart, WalkSpeed);
            Player.Trails = new TrailRecorder(0.7f, TrailKind.GirlFootprint);
        }

        public void SetJumpZones(IEnumerable<JumpZoneData> z) { zones.Clear(); zones.AddRange(z); }
        public void SetTreeZone(Rect r) => treeZone = r;

        public bool TryBeginJump()
        {
            if (jumping || Stage != Act4Stage.WalkToTree || Player.ControlLocked) return false;
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

        public void Tick(float dt, Vector2 move, float cameraFloorY)
        {
            if (Stage != Act4Stage.WalkToTree) return;

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

            if (treeZone.Contains(Player.Position))
            {
                Stage = Act4Stage.Ending;
                Player.ControlLocked = true;
                // 结局判定是纯规则：石碑是否全部点亮
                Demo3Ending ending = EndingRules.Decide(RequiredTablets, id => flags != null && flags.IsLit(id));
                Ending = ending;
                bus.Publish(new EndingDecidedEvent(ending));
            }
        }

        /// <summary>结局演出播完（导演脚本回调）→ Demo 结束，上报结果</summary>
        public void NotifyEndingFinished()
        {
            if (Stage != Act4Stage.Ending) return;
            Stage = Act4Stage.Done;
            run?.Report(DemoRunResult.Succeeded);
        }
    }
}
