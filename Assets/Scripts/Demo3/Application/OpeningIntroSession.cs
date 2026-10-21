using System;
using UnityEngine;

namespace Healing.Demo3.Application
{
    /// <summary>开场演出的四个阶段：落下 → 摔在地上（等晃动）→ 起身 → 交还第一幕</summary>
    public enum OpeningStage { Falling = 0, Fallen = 1, Rising = 2, Done = 3 }

    /// <summary>
    /// 开场演出会话（Application 层纯 C#，可单测）：
    /// Falling：女孩从空中落下（Fall 剪辑），计时 FallSeconds；
    /// Fallen ：摔在画面中央，周围空无一物——按任意移动键晃动她的身体，
    ///          每次有效晃动间隔至少 ShakeInterval 秒，满 ShakesRequired 次后进入起身；
    /// Rising ：起身，计时 RiseSeconds 后 Done，第一幕（人群聚拢/压迫计时）才开始推进。
    /// 注意：演出期间第一幕的 Tick 不会被调用。
    /// </summary>
    public sealed class OpeningIntroSession
    {
        public float FallSeconds = 2.2f;   // 落下时长（与 Fall 剪辑长度一致）
        public int ShakesRequired = 3;     // 起身所需晃动次数
        public float ShakeInterval = 1f;   // 两次有效晃动的最小间隔（防长按连刷）
        public float RiseSeconds = 1.0f;   // 起身时长

        public OpeningStage Stage { get; private set; } = OpeningStage.Falling;
        public int ShakeCount { get; private set; }

        /// <summary>第 n 次有效晃动（适配层播晃动表现）。</summary>
        public event Action<int> Shook;

        private float timer;
        private float shakeCooldown;
        private bool moveHeld;

        public void Tick(float dt, Vector2 move)
        {
            switch (Stage)
            {
                case OpeningStage.Falling:
                    timer += dt;
                    if (timer >= FallSeconds) { Stage = OpeningStage.Fallen; timer = 0f; }
                    break;

                case OpeningStage.Fallen:
                    shakeCooldown -= dt;
                    bool held = move.sqrMagnitude > 0.25f; // 任意方向推过半程即算一次「按」
                    if (held && !moveHeld && shakeCooldown <= 0f)
                    {
                        ShakeCount++;
                        shakeCooldown = ShakeInterval;
                        Shook?.Invoke(ShakeCount);
                        if (ShakeCount >= ShakesRequired) { Stage = OpeningStage.Rising; timer = 0f; }
                    }
                    moveHeld = held;
                    break;

                case OpeningStage.Rising:
                    timer += dt;
                    if (timer >= RiseSeconds) Stage = OpeningStage.Done;
                    break;
            }
        }
    }
}
