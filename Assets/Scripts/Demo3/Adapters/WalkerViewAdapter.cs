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
            rb.simulated = true;                 // Simulated 被关掉时 MovePosition 完全无效（角色纹丝不动）
            if (animator != null) animator.applyRootMotion = false; // 根运动会和 MovePosition 抢 transform

            // 拖线自检：Sprite 字段必须拖"视觉子物体"，拖成根物体自己时，
            // SetVisualLift 每帧会把根位置写回出生点——角色永远走不动、动画却正常（最难查的一种死法）。
            if (sprite == transform)
            {
                Debug.LogError($"[Demo3] {name} 的 WalkerViewAdapter：Sprite 字段拖成了根物体自己！" +
                               "应改拖视觉子物体（sprite）的 Transform——否则每帧位置都被写回出生点，" +
                               "角色原地踏步。已自动忽略该字段（跳跃腾空偏移暂时失效），请改拖子物体。", this);
                sprite = null;
            }
            if (sprite != null) baseSpriteLocalPos = sprite.localPosition;
            if (sprite != null && ((Vector2)sprite.localPosition).magnitude > 1.5f)
                Debug.LogWarning($"[Demo3] {name} 的 Sprite 子物体本地偏移 {sprite.localPosition} 很大——" +
                                 "视觉位置与判定用的逻辑位置（根物体）会相差这么多：看着站进触发区、逻辑上其实没进" +
                                 "（走进触发区没反应的经典假象）。建议把 Sprite 本地坐标归零，" +
                                 "偏移改由 SpriteRenderer 的 Pivot/图集设置承担。", this);

            // 排他自检：根物体上不允许再挂 Animator——它会按自己控制器的默认状态每帧覆写根物体位置，
            // 把刚体移动整个顶掉（症状：动画正常播、角色原地踏步、相机与跟随者全都不动）。
            // 正确结构：Animator 只挂在视觉子物体 Sprite 上，根物体只有 Rigidbody2D + WalkerViewAdapter。
            var rootAnim = GetComponent<Animator>();
            if (rootAnim != null && rootAnim != animator)
            {
                Debug.LogError($"[Demo3] {name} 的根物体上挂着一个多余的 Animator（控制器 " +
                               $"{(rootAnim.runtimeAnimatorController != null ? rootAnim.runtimeAnimatorController.name : "无")}）" +
                               "——它每帧把根物体位置写回动画录制值，角色永远走不动！已自动禁用兜底；" +
                               "请把它从根物体上删掉（Animator 只应留在 Sprite 子物体上）。", this);
                rootAnim.enabled = false;
            }
            Debug.Log("[Demo3] WalkerViewAdapter v10.18f 已加载（视觉偏移过大警告版）", this);
        }

        public void Bind(Vector2 startPos)
        {
            // 两种"组件在刚体在，但角色永远不动"的静默死法，必须当场报出来：
            if (!enabled)
                Debug.LogError($"[Demo3] {name} 的 WalkerViewAdapter 组件被禁用了（Inspector 左侧勾勾被去掉）" +
                               "——FixedUpdate 不会运行，角色永远无法移动。请勾回来。", this);
            else if (!rb.simulated)
                Debug.LogError($"[Demo3] {name} 的 Rigidbody2D 没有参与模拟（Simulated 未勾）" +
                               "——MovePosition 对它无效，角色永远无法移动。请勾上 Simulated。", this);
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

        private bool jumpAnim;  // 跳跃中：填了 States→Jump 状态名就播跳跃动画，没填回退方向动画

        /// <summary>跳跃动画开关（预留）：跳跃期间由场景根置 true、落地置 false。</summary>
        public void SetJumpAnim(bool active)
        {
            if (jumpAnim == active) return;
            jumpAnim = active;
            states.ResetCache(); // 起跳/落地都强制重选状态，避免停一拍旧动画
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
                if (!jumpAnim || !states.PlayJump(animator, this, vel))
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
                if (!jumpAnim || !states.PlayJump(animator, this, vel))
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

        /// <summary>调试：刚体当前位置与兜底开关状态（供场景根打走位诊断链）</summary>
        public Vector2 DebugRbPosition => rb != null ? rb.position : (Vector2)transform.position;
        public bool DebugLateOverride => lateOverride;

        private void PushFlags()
        {
            if (animator == null) return;
            animator.SetBool(Demo3AnimParams.IsHoldingHands, holdingHands);
            animator.SetInteger(Demo3AnimParams.Appearance, appearance);
            animator.SetInteger(Demo3AnimParams.Facing, lastFacing);
        }

        // pendingPos 只会被一条路径写入：平时是场景根 Apply；演出接管后 Apply 被忽略、由 ApplyExternal 写入。
        private void FixedUpdate() => rb.MovePosition(pendingPos);

        // 哨兵：身体(transform)落后刚体(rb)超过 0.5m 达 1 秒，说明 transform 被每帧覆写。
        // 最常见凶手：动画片段里录制了 Position 曲线——Animator 每帧把 transform 写回录制值。
        // 处理：报错指出根因，同时自动开启 LateUpdate 兜底（每帧把身体强制对齐刚体，先保证能动）。
        private float mismatchTimer;
        private bool lateOverride;

        private void LateUpdate()
        {
            if (!lateOverride)
            {
                if (Vector2.Distance(transform.position, rb.position) > 0.5f)
                {
                    mismatchTimer += Time.deltaTime;
                    if (mismatchTimer > 1f)
                    {
                        lateOverride = true;
                        Debug.LogError($"[Demo3] {name} 的身体被钉在 {transform.position}，刚体却已走到 {rb.position}——" +
                                       "transform 被每帧覆写（最大嫌疑：走路片段里录了 Position/LocalPosition 曲线，" +
                                       "Animator 每帧把位置写回录制值）。已自动开启兜底：每帧强制身体对齐刚体，角色恢复移动；" +
                                       "但请尽快删掉片段里的位置关键帧（位移只应作用在视觉子物体 sprite 上），" +
                                       "并确认 Apply Root Motion 保持关闭。",
                                       this);
                    }
                }
                else mismatchTimer = 0f;
            }
            if (lateOverride)
            {
                Vector2 p = rb.position;
                transform.position = new Vector3(p.x, p.y, transform.position.z);
            }
        }
    }
}
