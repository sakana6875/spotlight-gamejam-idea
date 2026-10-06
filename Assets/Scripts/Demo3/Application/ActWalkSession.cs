using Healing.Demo3.Domain;
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
        public TrailRecorder Trails { get; set; }   // 可为 null（无轨迹阶段）
        public Rect Bounds;                          // 静态场景边界（左右墙）
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

            Trails?.Tick(dt, Position, Velocity);
        }

        public void Teleport(Vector2 pos) { Position = pos; }
    }
}
