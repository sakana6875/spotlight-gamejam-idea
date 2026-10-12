using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 单个看客的表现体：位置、动画、透明度（被黑暗吞没用）、男女外观。
    /// Animator 参数约定见 <see cref="Demo3AnimParams"/>：
    /// Speed / IsMoving / Facing（0=向前👇 1=向后👆 2=向左👈 3=向右👉）。
    /// 美术已交付左右两个方向的走动动画，因此不再做 flipX 翻转。
    /// </summary>
    public sealed class NpcAgentView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Animator animator;

        private int lastFacing = Demo3AnimParams.FacingDown;

        /// <summary>设置看客外观（男/女形象，由模拟层的 Variant 决定，终身不变）</summary>
        public void SetAppearance(Sprite s)
        {
            if (body != null && s != null) body.sprite = s;
        }

        public void SetPosition(Vector2 pos) => transform.position = pos;

        public void SetVelocity(Vector2 v)
        {
            if (animator == null) return;
            animator.SetFloat(Demo3AnimParams.Speed, v.magnitude);
            animator.SetBool(Demo3AnimParams.IsMoving, v.magnitude > Demo3AnimParams.MoveEpsilon);
            lastFacing = Demo3AnimParams.FacingFromVelocity(v, lastFacing);
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
