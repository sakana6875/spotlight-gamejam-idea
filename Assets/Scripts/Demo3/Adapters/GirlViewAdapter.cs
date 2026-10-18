using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 小女孩的 Unity 适配：把 Session 的纯数据状态落到 Rigidbody2D 与 Animator。
    /// 移动逻辑（速度、迟钝、姿态）全在 Session/Domain，这里只做表现同步。
    /// 动画驱动两种方式（Inspector 上二选一）：
    ///  - Direct Drive ✓（默认，推荐）：代码直接 CrossFade 到同名状态，控制器不需要拉转换线；
    ///  - Direct Drive ✗：条件驱动，按 <see cref="Demo3AnimParams"/> 的参数表自己接转换线。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class GirlViewAdapter : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private bool directDrive = true;
        [SerializeField] private Demo3AnimStateDriver states = new Demo3AnimStateDriver();

        private Rigidbody2D rb;
        private Demo3Session session;
        private Vector2 pendingPos;
        private int lastFacing = Demo3AnimParams.FacingDown; // 初始面向镜头（向前👇）
        private bool holdingHands;
        private int appearance;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
            rb.simulated = true;                 // Simulated 被关掉时 MovePosition 完全无效
            if (animator != null) animator.applyRootMotion = false; // 根运动会和 MovePosition 抢 transform
        }

        public void Bind(Demo3Session s)
        {
            session = s;
            pendingPos = s.PlayerPosition;
            rb.position = s.PlayerPosition;
            if (directDrive) { states.Preflight(animator, this, needKneel: true); return; }    // 直驱模式不需要任何 Animator 参数
            Demo3AnimParams.Validate(animator, this, needPosture: true, needHandhold: true);
            if (animator != null)
            {
                animator.SetBool(Demo3AnimParams.IsHoldingHands, holdingHands);
                animator.SetInteger(Demo3AnimParams.Appearance, appearance);
                animator.SetInteger(Demo3AnimParams.Facing, lastFacing);
            }
        }

        /// <summary>牵手状态（第一幕恒 false；API 预留给复用本组件的场景）</summary>
        public void SetHandholding(bool value)
        {
            holdingHands = value;
            if (!directDrive && animator != null) animator.SetBool(Demo3AnimParams.IsHoldingHands, value);
        }

        /// <summary>形象变体：0=带帽子 1=不戴帽子 2=不戴帽子不带披风</summary>
        public void SetAppearance(int index)
        {
            appearance = index;
            if (!directDrive && animator != null) animator.SetInteger(Demo3AnimParams.Appearance, index);
        }

        public void Apply()
        {
            if (session == null) return;
            pendingPos = session.PlayerPosition;
            if (animator == null) return;

            Vector2 v = session.PlayerVelocity;
            bool moving = v.magnitude > Demo3AnimParams.MoveEpsilon;
            lastFacing = Demo3AnimParams.FacingFromVelocity(v, lastFacing);
            var posture = session.Gait.Posture;

            if (directDrive)
            {
                states.Play(animator, this, moving, lastFacing,
                    kneeling: posture == Demo3Posture.Kneel,
                    coverEars: posture == Demo3Posture.CoverEars || posture == Demo3Posture.Crouch);
                return;
            }

            animator.SetFloat(Demo3AnimParams.Speed, v.magnitude);
            animator.SetBool(Demo3AnimParams.IsMoving, moving);
            animator.SetInteger(Demo3AnimParams.Posture, (int)posture);
            animator.SetInteger(Demo3AnimParams.Facing, lastFacing);

            if (v.sqrMagnitude > 0.01f)
            {
                animator.SetFloat(Demo3AnimParams.DirX, v.normalized.x);
                animator.SetFloat(Demo3AnimParams.DirY, v.normalized.y);
            }
        }

        private void FixedUpdate()
        {
            if (session != null) rb.MovePosition(pendingPos);
        }
    }
}
