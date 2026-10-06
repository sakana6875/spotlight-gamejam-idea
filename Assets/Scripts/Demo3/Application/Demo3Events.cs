using Healing.Demo3.Domain;

namespace Healing.Demo3.Application
{
    // 类型化领域事件：只表示"事实已经发生"，不携带业务指令

    /// <summary>压迫值已变化（节流发布，供 UI 调试条 / 音频 / 视觉订阅）</summary>
    public readonly struct Demo3PressureChangedEvent
    {
        public readonly float Value;
        public Demo3PressureChangedEvent(float value) { Value = value; }
    }

    /// <summary>小女孩姿态已切换</summary>
    public readonly struct Demo3PostureChangedEvent
    {
        public readonly Demo3Posture Posture;
        public Demo3PostureChangedEvent(Demo3Posture posture) { Posture = posture; }
    }

    /// <summary>Demo3 阶段已切换（Gathering → Collapsing → Finished）</summary>
    public readonly struct Demo3StageChangedEvent
    {
        public readonly Demo3Stage Stage;
        public Demo3StageChangedEvent(Demo3Stage stage) { Stage = stage; }
    }

    /// <summary>小女孩已在人群中跪倒（压迫满值后的最终事实）</summary>
    public readonly struct Demo3GirlCollapsedEvent { }
}
