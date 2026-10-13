using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 单个看客的表现体：位置、动画、透明度（被黑暗吞没用）、男女外观。
    /// 动画两种模式（Inspector 上 Direct Drive 勾选框）：
    /// 勾选（默认）= 代码直驱：男女两个控制器里都只需放好 Idle/MoveDown/MoveUp/MoveLeft/MoveRight
    ///               五个状态，不需要任何转换线，状态名可在 States 折叠栏里改；
    /// 不勾 = 参数模式：Speed / IsMoving / Facing（0=向前👇 1=向后👆 2=向左👈 3=向右👉），自行拉线。
    /// 美术已交付左右两个方向的走动动画，因此不再做 flipX 翻转。
    /// </summary>
    public sealed class NpcAgentView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Animator animator;
        [Header("动画：勾选=代码直驱（控制器只放状态、不拉线）；不勾=参数+转换线")]
        [SerializeField] private bool directDrive = true;
        [SerializeField] private Demo3AnimStateDriver states = new Demo3AnimStateDriver();

        private int lastFacing = Demo3AnimParams.FacingDown;

        /// <summary>是否代码直驱（CrowdViewAdapter 据此决定预制体自检走状态名还是参数）</summary>
        public bool UsesDirectDrive => directDrive;

        /// <summary>直驱模式启动预检：五个基础状态缺失当场报警（只报一次）</summary>
        public void PreflightStates() => states.Preflight(animator, this, needKneel: false);

        /// <summary>设置看客外观（男/女形象，由模拟层的 Variant 决定，终身不变）</summary>
        public void SetAppearance(Sprite s)
        {
            if (body != null && s != null) body.sprite = s;
        }

        public void SetPosition(Vector2 pos) => transform.position = pos;

        public void SetVelocity(Vector2 v)
        {
            if (animator == null) return;
            bool moving = v.magnitude > Demo3AnimParams.MoveEpsilon;
            lastFacing = Demo3AnimParams.FacingFromVelocity(v, lastFacing);

            if (directDrive)
            {
                states.Play(animator, this, moving, lastFacing);
                return;
            }

            animator.SetFloat(Demo3AnimParams.Speed, v.magnitude);
            animator.SetBool(Demo3AnimParams.IsMoving, moving);
            animator.SetInteger(Demo3AnimParams.Facing, lastFacing);
        }

        public void SetAlpha(float a)
        {
            if (body == null) return;
            var c = body.color;
            c.a = a;
            body.color = c;
        }
    }
}
