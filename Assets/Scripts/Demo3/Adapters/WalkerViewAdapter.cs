using Healing.Demo3.Application;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 通用行走者视图（男孩/女孩复用）：把会话位置落到 Rigidbody2D 与 Animator。
    /// Animator 参数约定见 <see cref="Demo3AnimParams"/>：
    /// Speed / IsMoving / DirX / DirY / Facing / IsHoldingHands / Appearance；
    /// 女孩若复用第一幕姿态资源，可另加 Posture(int)。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class WalkerViewAdapter : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform sprite;      // 视觉子物体（跳跃高度偏移作用在它身上）

        private Rigidbody2D rb;
        private Vector2 pendingPos;
        private float visualLift;
        private Vector2 baseSpriteLocalPos;
        private int lastFacing = Demo3AnimParams.FacingDown;
        private bool holdingHands;
        private int appearance;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
            if (sprite != null) baseSpriteLocalPos = sprite.localPosition;
        }

        public void Bind(Vector2 startPos)
        {
            pendingPos = startPos;
            rb.position = startPos;
            Demo3AnimParams.Validate(animator, this, needPosture: false, needHandhold: true);
            PushFlags();
        }

        /// <summary>牵手状态（第二幕牵手段由场景根设置；美术资源：男孩/女孩"牵手跑动"）</summary>
        public void SetHandholding(bool value)
        {
            holdingHands = value;
            if (animator != null) animator.SetBool(Demo3AnimParams.IsHoldingHands, value);
        }

        /// <summary>形象变体：0=带帽子 1=不戴帽子 2=不戴帽子不带披风（结局二摘帽戴上时用）</summary>
        public void SetAppearance(int index)
        {
            appearance = index;
            if (animator != null) animator.SetInteger(Demo3AnimParams.Appearance, index);
        }

        /// <summary>每帧由场景根调用</summary>
        public void Apply(Vector2 pos, Vector2 vel)
        {
            pendingPos = pos;
            if (animator == null) return;

            animator.SetFloat(Demo3AnimParams.Speed, vel.magnitude);
            animator.SetBool(Demo3AnimParams.IsMoving, vel.magnitude > Demo3AnimParams.MoveEpsilon);

            lastFacing = Demo3AnimParams.FacingFromVelocity(vel, lastFacing);
            animator.SetInteger(Demo3AnimParams.Facing, lastFacing);

            if (vel.sqrMagnitude > 0.01f)
            {
                animator.SetFloat(Demo3AnimParams.DirX, vel.normalized.x);
                animator.SetFloat(Demo3AnimParams.DirY, vel.normalized.y);
            }
        }

        /// <summary>跳跃腾空高度（纯视觉，作用于精灵子物体，不动碰撞位置）</summary>
        public void SetVisualLift(float lift)
        {
            visualLift = lift;
            if (sprite != null)
                sprite.localPosition = baseSpriteLocalPos + new Vector2(0f, visualLift);
        }

        private void PushFlags()
        {
            if (animator == null) return;
            animator.SetBool(Demo3AnimParams.IsHoldingHands, holdingHands);
            animator.SetInteger(Demo3AnimParams.Appearance, appearance);
            animator.SetInteger(Demo3AnimParams.Facing, lastFacing);
        }

        private void FixedUpdate() => rb.MovePosition(pendingPos);
    }
}
