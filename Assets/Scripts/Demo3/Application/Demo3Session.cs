using System;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Application
{
    /// <summary>
    /// Demo3 的用例协调器（Application 层，纯 C#，无 Unity 组件依赖）。
    /// 职责：推进压迫值 → 计算步态与输入迟钝 → 推进人群 → 更新音频混合 →
    ///       发布事实事件 → 满值后进入跪倒演出 → 通过 IDemoRun 上报结果。
    /// 不负责：场景切换、UI 显示、具体音频组件、动画状态机细节（均在适配层/全局服务）。
    /// </summary>
    public sealed class Demo3Session
    {
        private readonly Demo3TuningData t;
        private readonly IDemo3Audio audio;
        private readonly IDemoRun run;
        private readonly IEventBus bus;

        private readonly PressureModel pressure;
        private Vector2 smoothedMove;
        private float collapseTimer;
        private float pressureEventTimer;
        private Demo3Posture lastPosture;

        public Demo3Stage Stage { get; private set; } = Demo3Stage.Gathering;
        public Vector2 PlayerPosition { get; private set; }
        public Vector2 PlayerVelocity { get; private set; }
        public GaitFrame Gait { get; private set; }
        public CrowdSimulation Sim { get; }
        public float Pressure => pressure.Value;
        public float Elapsed => pressure.Elapsed;
        public Demo3AudioMix CurrentMix { get; private set; }

        public Demo3Session(Demo3TuningData tuning, IDemo3Audio audio, IDemoRun run, IEventBus bus)
        {
            t = tuning ?? throw new ArgumentNullException(nameof(tuning));
            this.audio = audio ?? throw new ArgumentNullException(nameof(audio));
            this.run = run ?? throw new ArgumentNullException(nameof(run));
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));

            pressure = new PressureModel(t);
            Sim = new CrowdSimulation(t);
            Gait = GaitCurve.Evaluate(0f);
            lastPosture = Gait.Posture;

            this.audio.EnterAmbience();
            this.bus.Publish(new Demo3StageChangedEvent(Demo3Stage.Gathering));
        }

        /// <param name="rawMove">IInputService.Move 的原始输入（玩法不直接读设备）</param>
        /// <param name="viewMin/viewMax">适配层提供的视野范围（用于生成与游荡边沿判定）</param>
        public void Tick(float dt, Vector2 rawMove, Vector2 viewMin, Vector2 viewMax)
        {
            if (Stage == Demo3Stage.Finished) return;

            if (Stage == Demo3Stage.Gathering)
            {
                pressure.Tick(dt, Sim.Count);
                Gait = GaitCurve.Evaluate(pressure.Value);

                // —— 操作迟钝的实现 ——
                // 对输入向量做一阶低通：时间常数 ResponseSeconds 随压迫值增大，
                // 实际移动向量越来越"追不上"玩家的即时输入，产生延迟与沉重感；
                // 同时速度倍率下降，p=1 时归零。
                float k = Mathf.Min(1f, dt / Mathf.Max(1e-3f, Gait.ResponseSeconds));
                smoothedMove = Vector2.ClampMagnitude(smoothedMove + (rawMove - smoothedMove) * k, 1f);

                float speed = t.PlayerBaseSpeed * Gait.SpeedMultiplier;
                PlayerVelocity = smoothedMove * speed;
                PlayerPosition += PlayerVelocity * dt;

                Sim.Tick(dt, PlayerPosition, PlayerVelocity, viewMin, viewMax, pressure.Value);

                float girlSpeed01 = t.PlayerBaseSpeed > 0f
                    ? PlayerVelocity.magnitude / t.PlayerBaseSpeed : 0f;
                CurrentMix = AmbienceCurve.Evaluate(pressure.Value, girlSpeed01);
                audio.ApplyMix(CurrentMix);

                PublishFactsThrottled(dt);

                if (pressure.Value >= 0.999f) BeginCollapse();
            }
            else // Collapsing：玩家已无法移动，人群继续缓慢收缩
            {
                PlayerVelocity = Vector2.zero;
                Sim.Tick(dt, PlayerPosition, Vector2.zero, viewMin, viewMax, 1f);
                CurrentMix = AmbienceCurve.Evaluate(1f, 0f);
                audio.ApplyMix(CurrentMix);

                collapseTimer -= dt;
                if (collapseTimer <= 0f)
                {
                    Stage = Demo3Stage.Finished;
                    bus.Publish(new Demo3GirlCollapsedEvent());
                    bus.Publish(new Demo3StageChangedEvent(Demo3Stage.Finished));
                    // Demo 只报告结果；返回 Hub、解锁后续、播放剧情由 DemoFlowManager 决定
                    run.Report(DemoRunResult.Succeeded);
                }
            }
        }

        /// <summary>玩家在中途选择放弃（由暂停 UI 按钮经接口调用触发）</summary>
        public void Abandon()
        {
            if (Stage == Demo3Stage.Finished) return;
            Stage = Demo3Stage.Finished;
            audio.ExitAmbience();
            run.Report(DemoRunResult.Abandoned);
        }

        private void BeginCollapse()
        {
            Stage = Demo3Stage.Collapsing;
            Gait = GaitCurve.Evaluate(1f);
            collapseTimer = t.CollapseDelay;
            bus.Publish(new Demo3StageChangedEvent(Demo3Stage.Collapsing));
        }

        private void PublishFactsThrottled(float dt)
        {
            pressureEventTimer -= dt;
            if (pressureEventTimer <= 0f)
            {
                pressureEventTimer = 0.15f; // ~7Hz，够 UI/音频/视觉平滑，又不刷屏
                bus.Publish(new Demo3PressureChangedEvent(pressure.Value));
            }
            if (Gait.Posture != lastPosture)
            {
                lastPosture = Gait.Posture;
                bus.Publish(new Demo3PostureChangedEvent(lastPosture));
            }
        }

        // ---- 检查点临时状态（Memento）：交给 CheckpointManager / SaveManager ----

        public Demo3Snapshot CaptureSnapshot()
        {
            return new Demo3Snapshot
            {
                Elapsed = pressure.Elapsed,
                Pressure = pressure.Value,
                CrowdCount = Sim.Count,
                PlayerX = PlayerPosition.x,
                PlayerY = PlayerPosition.y,
            };
        }

        public void RestoreSnapshot(Demo3Snapshot s, Vector2 viewMin, Vector2 viewMax)
        {
            if (s == null || Stage != Demo3Stage.Gathering) return;
            pressure.Restore(s.Elapsed, s.Pressure);
            PlayerPosition = new Vector2(s.PlayerX, s.PlayerY);
            smoothedMove = Vector2.zero;
            Gait = GaitCurve.Evaluate(pressure.Value);
            lastPosture = Gait.Posture;
            Sim.RestoreCrowd(s.CrowdCount, PlayerPosition, viewMin, viewMax);
            bus.Publish(new Demo3PressureChangedEvent(pressure.Value));
            bus.Publish(new Demo3PostureChangedEvent(lastPosture));
        }

        public void Dispose()
        {
            if (Stage != Demo3Stage.Finished) audio.ExitAmbience();
        }
    }
}
