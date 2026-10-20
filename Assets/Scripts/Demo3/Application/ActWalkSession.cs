using UnityEngine;

namespace Healing.Demo3.Application
{
    /// <summary>
    /// 二/三/四幕共用的平面行走会话（应用层纯逻辑）：
    /// 恒定速度、恒定响应（区别于第一幕的压迫迟钝）、边界与"动态底墙"。
    /// 动态底墙：相机只向上跟随，玩家无法回头——可行走下沿 = max(场景静态下沿, 相机视野下沿)。
    /// </summary>
    public class ActWalkSession
    {
        public float Speed { get; set; }
        public Vector2 Position { get; protected set; }
        public Vector2 Velocity { get; private set; }
        public Rect Bounds = new Rect(-10000f, -10000f, 20000f, 20000f); // 静态场景边界（默认近似无限大）
        public float DynamicFloorY = float.NegativeInfinity; // 相机下沿抬升的动态墙
        public bool ControlLocked;                   // 播片/跳跃中锁操作

        public ActWalkSession(Vector2 startPos, float speed)
        {
            Position = startPos;
            Speed = speed;
        }

        public virtual void Tick(float dt, Vector2 move)
        {
            if (ControlLocked) { Velocity = Vector2.zero; return; }
            Velocity = Vector2.ClampMagnitude(move, 1f) * Speed;
            Position += Velocity * dt;

            float floor = Mathf.Max(Bounds.yMin, DynamicFloorY);
            Position = new Vector2(
                Mathf.Clamp(Position.x, Bounds.xMin, Bounds.xMax),
                Mathf.Clamp(Position.y, floor, Bounds.yMax));
        }

        /// <summary>
        /// 系统接管移动：无视玩家输入，以恒定速度走向目标点（用于第二幕进蘑菇段前的自动走位）。
        /// 返回是否已到达。
        /// </summary>
        public bool AutoStepTo(Vector2 target, float dt)
        {
            Vector2 to = target - Position;
            float step = Speed * dt;
            if (to.magnitude <= Mathf.Max(step, 1e-4f))
            {
                Position = target;
                Velocity = Vector2.zero;
                return true;
            }
            Velocity = to.normalized * Speed;
            Position += Velocity * dt;
            return false;
        }

        public void Teleport(Vector2 pos) { Position = pos; Velocity = Vector2.zero; }
    }
}
