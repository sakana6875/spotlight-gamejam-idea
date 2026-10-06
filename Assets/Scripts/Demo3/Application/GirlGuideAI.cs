using System;
using System.Collections.Generic;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Application
{
    public enum GuideState { WaitingAt = 0, Preparing = 1, Jumping = 2, Done = 3 }

    /// <summary>
    /// 第二幕蘑菇平台的女孩引导 AI（应用层纯逻辑）：
    ///  1. 开场播片结束后女孩已在第一个蘑菇顶（WaitingAt）
    ///  2. 玩家在交互区按下跳跃键 → 女孩进入 Preparing（走向蘑菇边缘）
    ///  3. 玩家落到女孩所在蘑菇顶 → 女孩跳往下一个并触发点亮（Jumping → WaitingAt）
    ///  4. 女孩始终比男孩快：跳跃速度参数 > 玩家跳跃速度，且落点提前一个平台
    /// </summary>
    public sealed class GirlGuideAI
    {
        private readonly IReadOnlyList<Vector2> tops;
        private readonly float planarSpeed;
        private readonly IEventBus bus;
        private int index;
        private float t;
        private Vector2 from, to;

        public Vector2 Position { get; private set; }
        public GuideState State { get; private set; } = GuideState.WaitingAt;
        public int CurrentIndex => index;
        public Vector2 CurrentTop => tops[Mathf.Clamp(index, 0, tops.Count - 1)];
        public float JumpHeight01 { get; private set; }

        public GirlGuideAI(IReadOnlyList<Vector2> mushroomTops, float planarSpeed, IEventBus bus)
        {
            if (mushroomTops == null || mushroomTops.Count == 0)
                throw new ArgumentException("至少需要一个蘑菇顶落点", nameof(mushroomTops));
            tops = mushroomTops;
            this.planarSpeed = planarSpeed;
            this.bus = bus;
            Position = tops[0];
        }

        /// <summary>开场播片：女孩跳上第一个蘑菇顶（由导演脚本播完后调用）</summary>
        public void BeginWaiting()
        {
            index = 0;
            Position = tops[0];
            State = GuideState.WaitingAt;
            bus.Publish(new GirlLandedOnPlatformEvent(0));
        }

        /// <summary>玩家按下了跳跃键（事实通知）→ 走向边缘准备</summary>
        public void NotifyPlayerJumpStarted()
        {
            if (State == GuideState.WaitingAt) State = GuideState.Preparing;
        }

        /// <summary>玩家落定（事实通知）：落在女孩当前蘑菇顶附近 → 跳往下一个</summary>
        public void NotifyPlayerLanded(Vector2 playerPos, float arriveRadius = 1.2f)
        {
            if (State != GuideState.WaitingAt && State != GuideState.Preparing) return;
            if ((playerPos - CurrentTop).sqrMagnitude > arriveRadius * arriveRadius) return;
            StartJumpToNext();
        }

        public void Tick(float dt)
        {
            if (State != GuideState.Jumping) { JumpHeight01 = 0f; return; }
            float dist = Vector2.Distance(from, to);
            t += dt / Mathf.Max(0.05f, JumpTrajectory.Duration(dist, planarSpeed));
            Position = JumpTrajectory.Eval(from, to, t, out float h);
            JumpHeight01 = h;
            if (t >= 1f)
            {
                State = GuideState.WaitingAt;
                JumpHeight01 = 0f;
                bus.Publish(new GirlLandedOnPlatformEvent(index));
            }
        }

        private void StartJumpToNext()
        {
            if (index + 1 >= tops.Count) { State = GuideState.Done; return; }
            from = tops[index];
            index++;
            to = tops[index];
            t = 0f;
            State = GuideState.Jumping;
            bus.Publish(new JumpStartedEvent(from, to, true));
        }
    }
}
