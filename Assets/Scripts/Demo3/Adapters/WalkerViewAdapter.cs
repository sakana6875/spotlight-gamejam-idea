using Healing.Demo3.Application;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 通用行走者视图（男孩/女孩复用）：把会话位置落到 Rigidbody2D 与 Animator。
    /// 参数约定：Speed(float)、DirX/DirY(float)；可选 Posture(int) 供女孩复用第一幕姿态。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class WalkerViewAdapter : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform sprite;      // 视觉子物体（跳跃高度偏移作用在它身上）

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int DirXHash = Animator.StringToHash("DirX");
        private static readonly int DirYHash = Animator.StringToHash("DirY");

        private Rigidbody2D rb;
        private Vector2 pendingPos;
        private float visualLift;
        private Vector2 baseSpriteLocalPos;

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
        }

        /// <summary>每帧由场景根调用</summary>
        public void Apply(Vector2 pos, Vector2 vel)
        {
            pendingPos = pos;
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, vel.magnitude);
                if (vel.sqrMagnitude > 0.01f)
                {
                    animator.SetFloat(DirXHash, vel.normalized.x);
                    animator.SetFloat(DirYHash, vel.normalized.y);
                }
            }
        }

        /// <summary>跳跃腾空高度（纯视觉，作用于精灵子物体，不动碰撞位置）</summary>
        public void SetVisualLift(float lift)
        {
            visualLift = lift;
            if (sprite != null)
                sprite.localPosition = baseSpriteLocalPos + new Vector2(0f, visualLift);
        }

        private void FixedUpdate() => rb.MovePosition(pendingPos);
    }
}
