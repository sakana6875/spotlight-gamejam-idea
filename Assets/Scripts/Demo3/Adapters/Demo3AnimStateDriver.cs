using System;
using System.Collections.Generic;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 动画状态直驱器（表现层）：代码按 移动/朝向/姿态 直接 CrossFade 到目标状态。
    ///
    /// 为什么有它：状态机转换线容易接错（条件永不触发、自我打断等），一旦接错就"动画错乱"。
    /// 直驱模式下 Animator Controller 里**只需要放好同名状态、不需要拉任何转换线**，
    /// 切换逻辑全部在代码里，所见即所得。
    ///
    /// 用法：在控制器里建以下状态（名字可在 Inspector 改），把对应动画片段拖进去：
    ///   Idle      待机（默认状态，Entry 指向它）
    ///   MoveDown  向前👇 跑动      MoveUp    向后👆 跑动
    ///   MoveLeft  向左👈 跑动      MoveRight 向右👉 跑动
    ///   Kneel     跪姿（女孩用，没有就留空）
    ///   CoverEarsMoveDown/Up/Left/Right  抱头跑动（女孩用，留空 = 用普通跑动）
    ///
    /// 注意：直驱模式与"参数条件转换线"二选一——转换线会和 CrossFade 打架。
    /// 想回到条件驱动：把视图组件上的 Direct Drive 勾掉，用 Demo3AnimParams 的参数接转换线。
    /// </summary>
    [Serializable]
    public sealed class Demo3AnimStateDriver
    {
        [Header("基础状态名（与 Animator 里的状态逐字一致）")]
        public string idle = "Idle";
        public string moveDown = "MoveDown";
        public string moveUp = "MoveUp";
        public string moveLeft = "MoveLeft";
        public string moveRight = "MoveRight";

        [Header("可选：女孩专用（留空则回退到普通跑动/待机）")]
        public string kneel = "Kneel";
        public string coverEarsMoveDown = "";
        public string coverEarsMoveUp = "";
        public string coverEarsMoveLeft = "";
        public string coverEarsMoveRight = "";

        [Header("切换淡入时长（秒）")]
        public float crossFadeSeconds = 0.08f;

        private string current;
        // 静态去重：池化的 130 个看客各自有驱动实例，按「控制器:状态名」全局只报一次
        private static readonly HashSet<string> warned = new HashSet<string>();

        private static string WarnKey(Animator anim, string s)
            => (anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "?") + ":" + s;

        /// <summary>外部演出（Trigger 播片）接管过动画后调用，强制下一帧重新选状态</summary>
        public void ResetCache() { current = null; }

        /// <summary>
        /// 启动预检：直驱需要的状态是否都在控制器里，缺失当场报警（比开跑后再报好查）。
        /// 可选状态（kneel/抱头）只检查非空配置；与 Play 共用 warned，不会重复报警。
        /// </summary>
        public void Preflight(Animator anim, UnityEngine.Object owner, bool needKneel)
        {
            if (anim == null) return;
            Check(anim, owner, idle);
            Check(anim, owner, moveDown);
            Check(anim, owner, moveUp);
            Check(anim, owner, moveLeft);
            Check(anim, owner, moveRight);
            if (needKneel) Check(anim, owner, kneel);
            Check(anim, owner, coverEarsMoveDown);
            Check(anim, owner, coverEarsMoveUp);
            Check(anim, owner, coverEarsMoveLeft);
            Check(anim, owner, coverEarsMoveRight);
        }

        private void Check(Animator anim, UnityEngine.Object owner, string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            if (!anim.HasState(0, Animator.StringToHash(s)) && warned.Add(WarnKey(anim, s)))
                Debug.LogWarning($"[Demo3] {owner.name} 的 Animator 找不到状态 \"{s}\"。" +
                                 "直驱模式请在控制器里建同名状态（不需要拉转换线）。" +
                                 "（此警告每个缺失状态只报一次）", owner);
        }

        /// <summary>
        /// 播一个演出动作（伸手/抬头/跪地/摘帽/相拥等一次性片段，按状态名直切）。
        /// 与 Play 共用缓存：演出结束后记得 ResetCache（SetExternalAnimControl(false) 会自动做）。
        /// </summary>
        public void PlayAction(Animator anim, UnityEngine.Object owner, string stateName)
        {
            if (anim == null || string.IsNullOrEmpty(stateName)) return;
            int hash = Animator.StringToHash(stateName);
            if (!anim.HasState(0, hash))
            {
                if (warned.Add(WarnKey(anim, stateName)))
                    Debug.LogWarning($"[Demo3] {owner.name} 的 Animator 找不到演出状态 \"{stateName}\"。" +
                                     "请在控制器里建同名状态（不需要拉转换线）。（此警告每个缺失状态只报一次）", owner);
                return;
            }
            anim.CrossFade(hash, crossFadeSeconds);
            current = stateName;
        }

        public void Play(Animator anim, UnityEngine.Object owner,
            bool moving, int facing, bool kneeling = false, bool coverEars = false)
        {
            if (anim == null) return;

            string s = kneeling && !string.IsNullOrEmpty(kneel) ? kneel
                     : moving ? MoveName(facing, coverEars)
                     : idle;
            if (string.IsNullOrEmpty(s) || s == current) return;

            int hash = Animator.StringToHash(s);
            if (!anim.HasState(0, hash))
            {
                if (warned.Add(WarnKey(anim, s)))
                    Debug.LogWarning($"[Demo3] {owner.name} 的 Animator 找不到状态 \"{s}\"。" +
                                     "直驱模式请在控制器里建同名状态（不需要拉转换线）。" +
                                     "（此警告每个缺失状态只报一次）", owner);
                return;
            }
            anim.CrossFade(hash, crossFadeSeconds);
            current = s;
        }

        private string MoveName(int facing, bool coverEars)
        {
            if (coverEars)
            {
                string ce = facing == Demo3AnimParams.FacingUp ? coverEarsMoveUp
                          : facing == Demo3AnimParams.FacingLeft ? coverEarsMoveLeft
                          : facing == Demo3AnimParams.FacingRight ? coverEarsMoveRight
                          : coverEarsMoveDown;
                if (!string.IsNullOrEmpty(ce)) return ce; // 没配抱头动画就回退普通跑动
            }
            return facing == Demo3AnimParams.FacingUp ? moveUp
                 : facing == Demo3AnimParams.FacingLeft ? moveLeft
                 : facing == Demo3AnimParams.FacingRight ? moveRight
                 : moveDown;
        }
    }
}
