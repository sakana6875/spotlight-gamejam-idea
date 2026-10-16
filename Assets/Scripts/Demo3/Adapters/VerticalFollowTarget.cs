using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// Cinemachine 纵向跟随代理（适配层小工具，无玩法逻辑）：
    /// 只把目标的 Y 同步给自己，X 固定在出生点。
    /// 用法：把它设为 Virtual Camera 的 Follow 目标（Body 用 Framing Transposer），
    /// 相机即"只上下跟随、绝不左右移动"；红框范围用 CinemachineConfiner2D 套 PolygonCollider2D 限制。
    /// </summary>
    public sealed class VerticalFollowTarget : MonoBehaviour
    {
        [SerializeField] private Transform target;   // 跟随目标（玩家）
        [Tooltip("勾选后 X 固定在出生位置（相机永不左右移动）")]
        [SerializeField] private bool lockX = true;

        private float fixedX;

        private void Start() => fixedX = transform.position.x;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 p = transform.position;
            p.y = target.position.y;
            p.x = lockX ? fixedX : target.position.x;
            transform.position = p;
        }
    }
}
