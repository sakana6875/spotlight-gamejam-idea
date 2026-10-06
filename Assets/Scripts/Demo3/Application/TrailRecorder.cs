using System;
using System.Collections.Generic;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Application
{
    /// <summary>
    /// 轨迹记录器（应用层）：按间距在角色身后落脚印/水纹。
    /// 纯数据输出，渲染由 TrailViewAdapter 完成；
    /// 记录结果可交给 IDemo3RunFlags 跨幕携带（第三幕做旧复现）。
    /// </summary>
    public sealed class TrailRecorder
    {
        private readonly List<TrailMark> marks = new List<TrailMark>(256);
        private readonly float spacing;
        private readonly TrailKind kind;
        private readonly Func<float> clock;
        private Vector2 last;
        private bool hasLast;
        private float time;

        public IReadOnlyList<TrailMark> Marks => marks;

        /// <summary>放置新印记时通知（视图层订阅，用于实时生成脚印精灵）</summary>
        public event Action<TrailMark> MarkPlaced;

        public TrailRecorder(float spacing, TrailKind kind)
        {
            this.spacing = Mathf.Max(0.05f, spacing);
            this.kind = kind;
        }

        /// <summary>or 深或浅：由速度比例驱动深浅，可在外部改规则</summary>
        public float StrengthScale = 1f;

        public void Tick(float dt, Vector2 pos, Vector2 velocity)
        {
            time += dt;
            if (velocity.sqrMagnitude < 0.04f) { last = pos; hasLast = true; return; }
            if (!hasLast) { last = pos; hasLast = true; }
            if (!TrailSpacing.ShouldPlace(last, pos, spacing)) return;

            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            float speed01 = Mathf.Clamp01(velocity.magnitude / 4f);
            // 深浅：走得越慢印越浅（0.5~1 区间抖动出自然感）
            float strength = Mathf.Lerp(0.5f, 1f, speed01) * StrengthScale;

            var mark = new TrailMark(pos, angle, kind, strength, time);
            marks.Add(mark);
            last = pos;
            MarkPlaced?.Invoke(mark);
        }

        public void Clear() { marks.Clear(); hasLast = false; }
    }
}
