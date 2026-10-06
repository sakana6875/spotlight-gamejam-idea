using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Application
{
    // ============ 第一幕 ============
    public readonly struct Demo3PressureChangedEvent
    {
        public readonly float Value;
        public Demo3PressureChangedEvent(float value) { Value = value; }
    }

    public readonly struct Demo3PostureChangedEvent
    {
        public readonly Demo3Posture Posture;
        public Demo3PostureChangedEvent(Demo3Posture posture) { Posture = posture; }
    }

    public readonly struct Demo3StageChangedEvent
    {
        public readonly Demo3Stage Stage;
        public Demo3StageChangedEvent(Demo3Stage stage) { Stage = stage; }
    }

    public readonly struct Demo3GirlCollapsedEvent { }

    // ============ 第二幕 ============
    /// <summary>段落已切换（第一段→第二段→第三段）</summary>
    public readonly struct Act2SegmentChangedEvent
    {
        public readonly Act2Segment Segment;
        public Act2SegmentChangedEvent(Act2Segment segment) { Segment = segment; }
    }

    /// <summary>女孩已在蘑菇顶上落定（引导 AI 事实）</summary>
    public readonly struct GirlLandedOnPlatformEvent
    {
        public readonly int PlatformIndex;
        public GirlLandedOnPlatformEvent(int index) { PlatformIndex = index; }
    }

    /// <summary>一次交互跳跃已经开始（供动画/音效订阅）</summary>
    public readonly struct JumpStartedEvent
    {
        public readonly Vector2 From;
        public readonly Vector2 To;
        public readonly bool IsGirl;
        public JumpStartedEvent(Vector2 from, Vector2 to, bool isGirl)
        { From = from; To = to; IsGirl = isGirl; }
    }

    /// <summary>一次交互跳跃已经落定</summary>
    public readonly struct JumpLandedEvent
    {
        public readonly Vector2 Pos;
        public readonly bool IsGirl;
        public JumpLandedEvent(Vector2 pos, bool isGirl) { Pos = pos; IsGirl = isGirl; }
    }

    // ============ 第三/四幕与结局 ============
    /// <summary>石碑已被点亮</summary>
    public readonly struct TabletLitEvent
    {
        public readonly string TabletId;
        public TabletLitEvent(string id) { TabletId = id; }
    }

    /// <summary>女孩在无视她的人群中晕倒（第三幕结束事实）</summary>
    public readonly struct GirlFaintedEvent { }

    /// <summary>结局已判定</summary>
    public readonly struct EndingDecidedEvent
    {
        public readonly Demo3Ending Ending;
        public EndingDecidedEvent(Demo3Ending ending) { Ending = ending; }
    }
}
