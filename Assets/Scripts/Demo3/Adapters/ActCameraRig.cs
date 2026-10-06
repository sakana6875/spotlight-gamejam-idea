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

        private float fixedX;
        private float maxReachedY;

        public float CameraBottomY =>
            cam.transform.position.y - cam.orthographicSize;

        private void Start()
        {
            fixedX = cam.transform.position.x;
            maxReachedY = cam.transform.position.y;
        }

        /// <param name="target">跟随目标（通常是玩家位置）</param>
        public void Apply(Vector2 target)
        {
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
