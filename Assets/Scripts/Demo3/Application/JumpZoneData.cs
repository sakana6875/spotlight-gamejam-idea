using System;
using UnityEngine;

namespace Healing.Demo3.Application
{
    /// <summary>
    /// 交互跳跃区（纯数据）：站在圈内按跳跃键 → 自动抛物线落到 Landing。
    /// 用距离判定而非 Collider2D，保证玩法规则可单测、可序列化。
    /// </summary>
    [Serializable]
    public struct JumpZoneData
    {
        public string Id;
        public Vector2 Center;
        public float Radius;
        public Vector2 Landing;
        public bool Enabled;

        public JumpZoneData(string id, Vector2 center, float radius, Vector2 landing)
        { Id = id; Center = center; Radius = radius; Landing = landing; Enabled = true; }

        public bool Contains(Vector2 p)
            => Enabled && (p - Center).sqrMagnitude <= Radius * Radius;
    }
}
