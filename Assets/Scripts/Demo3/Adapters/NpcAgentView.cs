using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>单个看客的表现体：位置、动画速度、透明度（被黑暗吞没用）。</summary>
    public sealed class NpcAgentView : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Animator animator;

        public void SetPosition(Vector2 pos) => transform.position = pos;

        public void SetVelocity(Vector2 v)
        {
            if (animator != null) animator.SetFloat(SpeedHash, v.magnitude);
            if (body != null && Mathf.Abs(v.x) > 0.05f)
                body.flipX = v.x < 0f;
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
