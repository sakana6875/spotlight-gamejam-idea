using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 通用行走者视图（男孩/女孩复用）：把会话位置落到 Rigidbody2D 与 Animator。
    /// 动画两种模式（Inspector 上 Direct Drive 勾选框）：
    /// 勾选（默认）= 代码直驱：控制器里只需放好 Idle/MoveDown/MoveUp/MoveLeft/MoveRight 五个状态，
    ///               不需要任何转换线，状态名可在 States 折叠栏里改；
    /// 不勾 = 参数模式：Animator 参数约定见 <see cref="Demo3AnimParams"/>，自行拉转换线。
    /// 演出导演（开场/结局）可通过 <see cref="SetExternalAnimControl"/> 临时接管 Animator。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class WalkerViewAdapter : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform sprite;      // 视觉子物体（跳跃高度偏移作用在它身上）
        [Header("动画：勾选=代码直驱（控制器只放状态、不拉线）；不勾=参数+转换线")]
        [SerializeField] private bool directDrive = true;
        [SerializeField] private Demo3AnimStateDriver states = new Demo3AnimStateDriver();

        private Rigidbody2D rb;
        private Vector2 pendingPos;
        private float visualLift;
        private Vector2 baseSpriteLocalPos;
        private int lastFacing = Demo3AnimParams.FacingDown;
        private bool holdingHands;
        private int appearance;
        private bool externalAnimControl;   // 演出接管期间为 true：不播移动动画、不抢位移

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
            if (directDrive) { states.Preflight(animator, this, needKneel: false); return; }
            Demo3AnimParams.Validate(animator, this, needPosture: false, needHandhold: true);
            PushFlags();
        }

        /// <summary>牵手状态（第二幕牵手段由场景根设置；美术资源：男孩/女孩"牵手跑动"）</summary>
        public void SetHandholding(bool value)
        {
            holdingHands = value;
            if (!directDrive && animator != null) animator.SetBool(Demo3AnimParams.IsHoldingHands, value);
        }

        /// <summary>形象变体：0=带帽子 1=不戴帽子 2=不戴帽子不带披风（结局二摘帽戴上时用）</summary>
        public void SetAppearance(int index)
        {
            appearance = index;
            if (!directDrive && animator != null) animator.SetInteger(Demo3AnimParams.Appearance, index);
        }

        /// <summary>
        /// 演出导演接管/归还 Animator 与位移（开场与结局演出用）。
        /// 接管期间场景根的 Apply 整体被忽略（位移与动画都交给导演的 ApplyExternal/PlayAction）；
        /// 归还时清直驱缓存，强制重播当前状态。
        /// </summary>
        public void SetExternalAnimControl(bool external)
        {
            externalAnimControl = external;
            if (!external) states.ResetCache();
        }

        /// <summary>每帧由场景根调用（演出接管期间整体被忽略）</summary>
        public void Apply(Vector2 pos, Vector2 vel)
        {
            if (externalAnimControl) return;
            pendingPos = pos;
            if (animator == null) return;

            bool moving = vel.magnitude > Demo3AnimParams.MoveEpsilon;
            lastFacing = Demo3AnimParams.FacingFromVelocity(vel, lastFacing);

            if (directDrive)
            {
                states.Play(animator, this, moving, lastFacing);
                return;
            }

            animator.SetFloat(Demo3AnimParams.Speed, vel.magnitude);
            animator.SetBool(Demo3AnimParams.IsMoving, moving);
            animator.SetInteger(Demo3AnimParams.Facing, lastFacing);

            if (vel.sqrMagnitude > 0.01f)
            {
                animator.SetFloat(Demo3AnimParams.DirX, vel.normalized.x);
                animator.SetFloat(Demo3AnimParams.DirY, vel.normalized.y);
            }
        }

        /// <summary>演出导演专用：外部接管期间驱动位移与行走动画（此时场景根的 Apply 被忽略）</summary>
        public void ApplyExternal(Vector2 pos, Vector2 vel)
        {
            pendingPos = pos;
            if (animator == null) return;

            bool moving = vel.magnitude > Demo3AnimParams.MoveEpsilon;
            lastFacing = Demo3AnimParams.FacingFromVelocity(vel, lastFacing);

            if (directDrive)
            {
                states.Play(animator, this, moving, lastFacing);
                return;
            }

            animator.SetFloat(Demo3AnimParams.Speed, vel.magnitude);
            animator.SetBool(Demo3AnimParams.IsMoving, moving);
            animator.SetInteger(Demo3AnimParams.Facing, lastFacing);
        }

        /// <summary>播一个演出动作状态（Extend/RaiseHead/Stand/KneelDown/SisterHat/SisterHug 等，按状态名直切，无需转换线）</summary>
        public void PlayAction(string stateName) => states.PlayAction(animator, this, stateName);

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

        // pendingPos 只会被一条路径写入：平时是场景根 Apply；演出接管后 Apply 被忽略、由 ApplyExternal 写入。
        private void FixedUpdate() => rb.MovePosition(pendingPos);
    }
}
