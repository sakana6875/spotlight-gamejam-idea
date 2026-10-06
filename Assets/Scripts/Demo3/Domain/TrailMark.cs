using System;
using UnityEngine;

namespace Healing.Demo3.Domain
{
    /// <summary>轨迹类型：女孩脚印 / 男孩水纹（L1 区域女孩也产生水纹）</summary>
    public enum TrailKind { GirlFootprint = 0, BoyRipple = 1, GirlRipple = 2 }

    /// <summary>一个轨迹印记（纯数据，可跨场景携带、可入检查点快照）</summary>
    [Serializable]
    public struct TrailMark
    {
        public Vector2 Pos;
        public float AngleDeg;   // 移动方向角，脚印左右交替由视图层处理
        public TrailKind Kind;
        public float Strength;   // 0~1，"或深或浅的脚印"
        public float Age;        // 放置时的场景时间，用于第三幕做旧

        public TrailMark(Vector2 pos, float angleDeg, TrailKind kind, float strength, float age)
        { Pos = pos; AngleDeg = angleDeg; Kind = kind; Strength = strength; Age = age; }
    }

    /// <summary>脚印/水纹的落点间距规则（纯函数）</summary>
    public static class TrailSpacing
    {
        public static bool ShouldPlace(Vector2 last, Vector2 now, float spacing)
            => (now - last).sqrMagnitude >= spacing * spacing;
    }
}
