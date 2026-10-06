using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 小女孩的 Unity 适配：把 Session 的纯数据状态落到 Rigidbody2D 与 Animator。
    /// 移动逻辑（速度、迟钝、姿态）全在 Session/Domain，这里只做表现同步。
    /// Animator 约定参数：Speed(float)、Posture(int，对应 Demo3Posture)、DirX/DirY(float)。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class GirlViewAdapter : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private static readonly int SpeedHash   = Animator.StringToHash("Speed");
        private static readonly int PostureHash = Animator.StringToHash("Posture");
        private static readonly int DirXHash    = Animator.StringToHash("DirX");
        private static readonly int DirYHash    = Animator.StringToHash("DirY");

        private Rigidbody2D rb;
        private Demo3Session session;
        private Vector2 pendingPos;

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
        }

        public void Apply()
        {
            if (session == null) return;
            pendingPos = session.PlayerPosition;

            Vector2 v = session.PlayerVelocity;
            animator.SetFloat(SpeedHash, v.magnitude);
            animator.SetInteger(PostureHash, (int)session.Gait.Posture);
            if (v.sqrMagnitude > 0.01f)
            {
                animator.SetFloat(DirXHash, v.normalized.x);
                animator.SetFloat(DirYHash, v.normalized.y);
            }
        }

        private void FixedUpdate()
        {
            if (session != null) rb.MovePosition(pendingPos);
        }
    }
}
