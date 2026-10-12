using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 小女孩的 Unity 适配：把 Session 的纯数据状态落到 Rigidbody2D 与 Animator。
    /// 移动逻辑（速度、迟钝、姿态）全在 Session/Domain，这里只做表现同步。
    /// Animator 参数约定见 <see cref="Demo3AnimParams"/>：
    /// Speed / IsMoving / DirX / DirY / Facing / Posture / IsHoldingHands / Appearance。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class GirlViewAdapter : MonoBehaviour
    {
        [SerializeField] private Animator animator;

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
        }

        public void Bind(Demo3Session s)
        {
            session = s;
            pendingPos = s.PlayerPosition;
            rb.position = s.PlayerPosition;
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
            if (animator != null) animator.SetBool(Demo3AnimParams.IsHoldingHands, value);
        }

        /// <summary>形象变体：0=带帽子 1=不戴帽子 2=不戴帽子不带披风</summary>
        public void SetAppearance(int index)
        {
            appearance = index;
            if (animator != null) animator.SetInteger(Demo3AnimParams.Appearance, index);
        }

        public void Apply()
        {
            if (session == null) return;
            pendingPos = session.PlayerPosition;
            if (animator == null) return;

            Vector2 v = session.PlayerVelocity;
            animator.SetFloat(Demo3AnimParams.Speed, v.magnitude);
            animator.SetBool(Demo3AnimParams.IsMoving, v.magnitude > Demo3AnimParams.MoveEpsilon);
            animator.SetInteger(Demo3AnimParams.Posture, (int)session.Gait.Posture);

            lastFacing = Demo3AnimParams.FacingFromVelocity(v, lastFacing);
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
