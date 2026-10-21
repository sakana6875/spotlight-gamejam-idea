using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 幕内相机规则（第二/三/四幕共用）：
    ///  UpOnlyNoSides —— 需求规则：纵向向上跟随、向下不跟随（玩家无法回头）、左右不跟随（撞侧墙）。
    ///  Free          —— 双轴跟随（隐藏区域等）。
    /// 场景根每帧把 CameraBottomY 传给会话，形成"动态底墙"。
    /// </summary>
    public sealed class ActCameraRig : MonoBehaviour
    {
        public enum Mode { Free = 0, UpOnlyNoSides = 1 }

        [SerializeField] private Mode mode = Mode.UpOnlyNoSides;
        [SerializeField] private Camera cam;
        [SerializeField] private float followLerp = 6f;
        [Tooltip("勾选后本组件不再驱动相机位置（交给 Cinemachine），CameraBottomY 仍按相机实际位置计算")]
        [SerializeField] private bool externalControl;

        private float fixedX;
        private float maxReachedY;

        public float CameraBottomY =>
            cam.transform.position.y - cam.orthographicSize;

        private void Start()
        {
            if (cam == null)
            {
                Debug.LogError("[Demo3] ActCameraRig：Cam 没拖入——相机规则不生效，CameraBottomY 也算不出来。", this);
                enabled = false;
                return;
            }
            fixedX = cam.transform.position.x;
            maxReachedY = cam.transform.position.y;
            if (externalControl)
            {
                // 勾了 External Control 就是把方向盘交给 Cinemachine；相机上没 Brain 就没人开车
                bool hasBrain = false;
                foreach (var c in cam.GetComponents<Component>())
                    if (c != null && c.GetType().Name == "CinemachineBrain") { hasBrain = true; break; }
                if (!hasBrain)
                    Debug.LogError("[Demo3] ActCameraRig 勾了 External Control（相机交给 Cinemachine），" +
                                   "但 Main Camera 上没有 CinemachineBrain——没人驱动相机，镜头永远不会动！" +
                                   "二选一：①给 Main Camera 加 CinemachineBrain 并配好 vcam（Follow=FollowProxy）；②把 External Control 勾掉，由本组件直接驱动相机。", this);
            }
        }

        /// <param name="target">跟随目标（通常是玩家位置）</param>
        public void Apply(Vector2 target)
        {
            if (externalControl) return; // 相机由 Cinemachine 驱动
            Vector3 p = cam.transform.position;
            if (mode == Mode.Free)
            {
                p.x = Mathf.Lerp(p.x, target.x, followLerp * Time.deltaTime);
                p.y = Mathf.Lerp(p.y, target.y, followLerp * Time.deltaTime);
            }
            else
            {
                // 左右不跟随：x 固定；纵向只上不下：只取历史最高
                maxReachedY = Mathf.Max(maxReachedY, target.y);
                p.x = fixedX;
                p.y = Mathf.Lerp(p.y, maxReachedY, followLerp * Time.deltaTime);
            }
            cam.transform.position = p;
        }
    }
}
