using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 全部角色的 Animator 参数约定（表现层唯一入口）。
    /// 与美术交付的动画资源一一对应，三个角色（女孩/男孩/看客）共用同一套参数名，
    /// 方便 Animator Controller 复用状态机结构。
    ///
    /// ┌─────────────────┬───────┬──────────────────────────────────────────────┐
    /// │ 参数名          │ 类型  │ 含义 / 对应美术资源                          │
    /// ├─────────────────┼───────┼──────────────────────────────────────────────┤
    /// │ Speed           │ Float │ 当前移动速度（单位/秒）；>0.1 即移动中       │
    /// │ IsMoving        │ Bool  │ 是否在移动（Idle ↔ 跑动 的切换条件）         │
    /// │ DirX / DirY     │ Float │ 连续移动方向（-1~1，八向混合树用）           │
    /// │ Facing          │ Int   │ 四向离散朝向（四方向跑动动画的切换条件）：   │
    /// │                 │       │   0=向前👇(Down) 1=向后👆(Up)               │
    /// │                 │       │   2=向左👈(Left) 3=向右👉(Right)            │
    /// │                 │       │   斜向按主轴吸附；静止时保持上一次朝向       │
    /// │ Posture         │ Int   │ 女孩姿态（对应 Demo3Posture 枚举）：         │
    /// │                 │       │   0=正常 1=轻微低头 2=抱头 3=蜷缩 4=跪姿     │
    /// │                 │       │   "抱头跑动"资源用 Posture==2 做切换条件     │
    /// │ IsHoldingHands  │ Bool  │ 牵手状态（男孩/女孩的"牵手跑动"资源）        │
    /// │ Appearance      │ Int   │ 女孩形象变体（切换整套形象层/精灵）：        │
    /// │                 │       │   0=带帽子 1=不戴帽子 2=不戴帽子不带披风     │
    /// └─────────────────┴───────┴──────────────────────────────────────────────┘
    /// </summary>
    public static class Demo3AnimParams
    {
        public const string SpeedName          = "Speed";
        public const string IsMovingName       = "IsMoving";
        public const string DirXName           = "DirX";
        public const string DirYName           = "DirY";
        public const string FacingName         = "Facing";
        public const string PostureName        = "Posture";
        public const string IsHoldingHandsName = "IsHoldingHands";
        public const string AppearanceName     = "Appearance";

        public static readonly int Speed          = Animator.StringToHash(SpeedName);
        public static readonly int IsMoving       = Animator.StringToHash(IsMovingName);
        public static readonly int DirX           = Animator.StringToHash(DirXName);
        public static readonly int DirY           = Animator.StringToHash(DirYName);
        public static readonly int Facing         = Animator.StringToHash(FacingName);
        public static readonly int Posture        = Animator.StringToHash(PostureName);
        public static readonly int IsHoldingHands = Animator.StringToHash(IsHoldingHandsName);
        public static readonly int Appearance     = Animator.StringToHash(AppearanceName);

        /// <summary>朝向取值（与上面 Facing 注释一致）</summary>
        public const int FacingDown = 0, FacingUp = 1, FacingLeft = 2, FacingRight = 3;

        /// <summary>速度低于此值视为静止（IsMoving=false，Facing 保持）</summary>
        public const float MoveEpsilon = 0.1f;

        /// <summary>
        /// 由移动向量求四向朝向：斜向按主轴吸附（水平分量更大 → 左/右，否则 → 前/后）；
        /// 输入近似静止时返回 lastFacing（保持朝向，不会闪回默认方向）。
        /// </summary>
        public static int FacingFromVelocity(Vector2 v, int lastFacing)
        {
            if (v.sqrMagnitude < MoveEpsilon * MoveEpsilon) return lastFacing;
            if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
                return v.x >= 0f ? FacingRight : FacingLeft;
            return v.y >= 0f ? FacingUp : FacingDown;
        }

        /// <summary>
        /// 启动自检：动画切换不动时先看 Console——转换条件引用了不存在的参数是最高频原因
        /// （Animator 的参数名区分大小写，必须与上面的常量逐字一致）。
        /// </summary>
        public static void Validate(Animator anim, Object owner, bool needPosture, bool needHandhold)
        {
            if (anim == null)
            {
                Debug.LogWarning($"[Demo3] {owner.name} 的 Animator 字段未拖入，动画不会播放。", owner);
                return;
            }
            Check(anim, SpeedName, AnimatorControllerParameterType.Float, owner);
            Check(anim, IsMovingName, AnimatorControllerParameterType.Bool, owner);
            Check(anim, FacingName, AnimatorControllerParameterType.Int, owner);
            Check(anim, DirXName, AnimatorControllerParameterType.Float, owner);
            Check(anim, DirYName, AnimatorControllerParameterType.Float, owner);
            if (needPosture) Check(anim, PostureName, AnimatorControllerParameterType.Int, owner);
            if (needHandhold) Check(anim, IsHoldingHandsName, AnimatorControllerParameterType.Bool, owner);
        }

        private static void Check(Animator anim, string name, AnimatorControllerParameterType type, Object owner)
        {
            var ps = anim.parameters;
            for (int i = 0; i < ps.Length; i++)
                if (ps[i].name == name && ps[i].type == type) return;
            Debug.LogWarning($"[Demo3] {owner.name} 的 Animator 缺少参数 {name}（{type}），" +
                             "引用它的转换条件永远不会触发。请在 Animator 窗口 Parameters 里按同名添加。", owner);
        }
    }
}
