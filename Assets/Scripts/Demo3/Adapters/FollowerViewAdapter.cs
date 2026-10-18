using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 跟随者视图（第二幕女孩专用）：不挂 Rigidbody2D、不参与物理。
    /// 两种驱动方式：
    ///  ①跟随模式（牵手上行/接管段）：SetFollow(男孩) 后，每帧在 LateUpdate 向
    ///    "男孩位置 + 牵手偏移"平滑靠拢，并按真实位移速度播 Move*/Idle 动画；
    ///  ②外部驱动（播片/蘑菇引导/树下领队）：场景根每帧 Apply 指定位置与速度。
    /// 位置统一在 LateUpdate 写 transform——写在 Animator 之后，动画片段里即使误录了
    /// 位置曲线也顶不动她。动画仍是直驱：控制器里只放同名状态、不拉转换线。
    /// </summary>
    public sealed class FollowerViewAdapter : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform sprite;      // 视觉子物体（跳跃高度偏移作用在它身上）
        [Header("动画：勾选=代码直驱（控制器只放状态、不拉线）；不勾=参数+转换线")]
        [SerializeField] private bool directDrive = true;
        [SerializeField] private Demo3AnimStateDriver states = new Demo3AnimStateDriver();

        [Header("跟随参数（SetFollow 激活后生效）")]
        [SerializeField] private Vector2 holdOffset = new Vector2(0.45f, -0.1f);
        [SerializeField] private float followLerp = 5f;

        private Transform followLeader;
        private Vector2 pendingPos;
        private float visualLift;
        private Vector2 baseSpriteLocalPos;
        private int lastFacing = Demo3AnimParams.FacingDown;
        private bool holdingHands;
        private bool externalAnimControl;   // 演出接管期间为 true：场景根的 Apply 被忽略

        private void Awake()
        {
            if (animator != null) animator.applyRootMotion = false;

            // 拖线自检：Sprite 字段必须拖"视觉子物体"，拖成根物体自己时 SetVisualLift 会每帧把根位置写回出生点
            if (sprite == transform)
            {
                Debug.LogError($"[Demo3] {name} 的 FollowerViewAdapter：Sprite 字段拖成了根物体自己！" +
                               "应改拖视觉子物体（sprite）的 Transform。已自动忽略该字段，请改拖子物体。", this);
                sprite = null;
            }
            if (sprite != null) baseSpriteLocalPos = sprite.localPosition;
            pendingPos = transform.position;

            // 排他自检：跟随者身上不允许再有别的移动组件——两套移动逻辑会互相把对方的位置写回去，
            // 结果是她被钉在原地、动画也不播（旧 WalkerViewAdapter 会把刚体每帧拖回出生点）。
            var rivalWalker = GetComponent<WalkerViewAdapter>();
            var rivalRb = GetComponent<Rigidbody2D>();
            if (rivalWalker != null || rivalRb != null)
                Debug.LogError($"[Demo3] {name} 身上同时挂着 FollowerViewAdapter 和 " +
                               (rivalWalker != null ? "WalkerViewAdapter" : "Rigidbody2D") +
                               "——两套移动逻辑互相顶位置，她会钉在原地不动。请把女孩身上的 WalkerViewAdapter " +
                               "和 Rigidbody2D 都删掉，只留 FollowerViewAdapter。", this);
            Debug.Log("[Demo3] FollowerViewAdapter v10.18d 已加载（跳跃动画预留位版）", this);
        }

        public void Bind(Vector2 startPos)
        {
            pendingPos = startPos;
            transform.position = startPos;
            if (directDrive) states.Preflight(animator, this, needKneel: false);
        }

        /// <summary>跟随模式开关：传入男孩 Transform 开始跟随；传 null 停止（交回外部驱动）。</summary>
        public void SetFollow(Transform leader)
        {
            followLeader = leader;
            states.ResetCache(); // 立刻按新的速度重选状态，避免停一拍旧动画
            followStuckTimer = 0f;
            Debug.Log(leader != null
                ? $"[Demo3] {name} 开始跟随 {leader.name}"
                : $"[Demo3] {name} 停止跟随，交回外部驱动", this);
        }

        /// <summary>牵手状态（直驱下仅占位；参数模式会推 IsHoldingHands）</summary>
        public void SetHandholding(bool value)
        {
            holdingHands = value;
            if (!directDrive && animator != null) animator.SetBool(Demo3AnimParams.IsHoldingHands, value);
        }

        /// <summary>演出导演接管/归还动画与位移（接管期间场景根的 Apply 被忽略）。</summary>
        public void SetExternalAnimControl(bool external)
        {
            externalAnimControl = external;
            if (!external) states.ResetCache();
        }

        /// <summary>外部驱动：每帧由场景根调用（跟随模式与演出接管期间忽略位置，只保留动画驱动交给跟随/导演）</summary>
        public void Apply(Vector2 pos, Vector2 vel)
        {
            if (externalAnimControl || followLeader != null) return;
            pendingPos = pos;
            PlayAnim(vel);
        }

        /// <summary>演出导演专用：接管期间驱动位移与动画</summary>
        public void ApplyExternal(Vector2 pos, Vector2 vel)
        {
            pendingPos = pos;
            PlayAnim(vel);
        }

        private bool jumpAnim;  // 跳跃中：填了 States→Jump 状态名就播跳跃动画，没填回退方向动画

        /// <summary>跳跃动画开关（预留）：跳跃期间由场景根置 true、落地置 false。</summary>
        public void SetJumpAnim(bool active)
        {
            if (jumpAnim == active) return;
            jumpAnim = active;
            states.ResetCache(); // 起跳/落地都强制重选状态，避免停一拍旧动画
        }

        /// <summary>播一个演出动作状态（按状态名直切，无需转换线）</summary>
        public void PlayAction(string stateName) => states.PlayAction(animator, this, stateName);

        /// <summary>跳跃腾空高度（纯视觉，作用于精灵子物体）</summary>
        public void SetVisualLift(float lift)
        {
            visualLift = lift;
            if (sprite != null)
                sprite.localPosition = baseSpriteLocalPos + new Vector2(0f, visualLift);
        }

        // 位置统一在 LateUpdate 写：Animator 每帧先写，我们后写——片段里误录的位置曲线顶不动跟随者。
        private void LateUpdate()
        {
            if (followLeader != null && !externalAnimControl)
            {
                Vector2 cur = transform.position;
                Vector2 target = (Vector2)followLeader.position + holdOffset;
                Vector2 next = Vector2.Lerp(cur, target, followLerp * Time.deltaTime);
                var vel = (next - cur) / Mathf.Max(Time.deltaTime, 1e-5f);
                pendingPos = next;
                PlayAnim(vel);

                // 跟随哨兵：激活后持续 2 秒没靠近领队，说明 transform 被别的东西钉住
                if (Vector2.Distance(cur, target) > 1f)
                {
                    followStuckTimer += Time.deltaTime;
                    if (followStuckTimer > 2f && !followStuckReported)
                    {
                        followStuckReported = true;
                        Debug.LogError($"[Demo3] {name} 跟随已激活，但 2 秒都没能靠近领队（目标 {target}，自己停在 {cur}）" +
                                       "——她的 transform 被钉住了。按顺序检查：①她身上是否还挂着 WalkerViewAdapter / " +
                                       "Rigidbody2D（必须删掉，只留 FollowerViewAdapter）；②她的动画片段里是否录了 " +
                                       "Position 曲线（删掉位置关键帧）；③是否有别的脚本在写她的位置。", this);
                    }
                }
                else followStuckTimer = 0f;
            }
            var p = transform.position;
            transform.position = new Vector3(pendingPos.x, pendingPos.y, p.z);
        }

        private float followStuckTimer;
        private bool followStuckReported;

        private void PlayAnim(Vector2 vel)
        {
            if (animator == null) return;
            bool moving = vel.magnitude > Demo3AnimParams.MoveEpsilon;
            lastFacing = Demo3AnimParams.FacingFromVelocity(vel, lastFacing);
            if (directDrive)
            {
                if (!jumpAnim || !states.PlayJump(animator, this))
                    states.Play(animator, this, moving, lastFacing);
                return;
            }
            animator.SetFloat(Demo3AnimParams.Speed, vel.magnitude);
            animator.SetBool(Demo3AnimParams.IsMoving, moving);
            animator.SetInteger(Demo3AnimParams.Facing, lastFacing);
        }
    }
}
