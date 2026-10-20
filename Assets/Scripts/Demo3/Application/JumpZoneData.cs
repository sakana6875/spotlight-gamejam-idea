using System;
using UnityEngine;

namespace Healing.Demo3.Application
{
    /// <summary>
    /// 交互跳跃区（纯数据）：站在区内按跳跃键 → 自动抛物线落到 Landing。
    /// 两种判定形状：圆形（Center+Radius，第二幕自动链用）与矩形（Area，场景里拖碰撞体换算而来）。
    /// 落地后可标记"可行走平台"（如 L1）：落地恢复 WASD 并把活动范围钳到 LandingWalkArea；
    /// 不标记则落地后只能继续跳（蘑菇垫——固定动线，禁止走动）。
    /// 用纯数据而非 Collider2D，保证玩法规则可单测、可序列化。
    /// </summary>
    [Serializable]
    public struct JumpZoneData
    {
        public string Id;

        [Header("圆形判定（自动链用）")]
        public Vector2 Center;
        public float Radius;

        [Header("矩形判定（拖碰撞体换算，UseRectArea=true 时生效）")]
        public Rect Area;
        public bool UseRectArea;

        [Header("落点")]
        public Vector2 Landing;
        public bool Enabled;
        [Tooltip("落点是可行走平台（恢复 WASD）；否则落地锁定移动、只能继续跳（蘑菇垫）")]
        public bool LandingIsWalkable;
        [Tooltip("可行走落地时的活动范围（如 L1 平台）；宽高为 0 则用全场 Walk Bounds")]
        public Rect LandingWalkArea;

        public JumpZoneData(string id, Vector2 center, float radius, Vector2 landing)
        {
            Id = id; Center = center; Radius = radius; Landing = landing; Enabled = true;
            Area = default; UseRectArea = false;
            LandingIsWalkable = false; LandingWalkArea = default;
        }

        public bool Contains(Vector2 p)
            => Enabled && (UseRectArea ? Area.Contains(p) : (p - Center).sqrMagnitude <= Radius * Radius);
    }
}
