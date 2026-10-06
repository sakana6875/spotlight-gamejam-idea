using UnityEngine;

namespace Healing.Demo3.Domain
{
    /// <summary>
    /// 交互式跳跃的抛物线解算（纯函数）。
    /// 位置在 XY 平面做线性插值，"腾空高度"是一条独立的 0→1→0 抛物线，
    /// 由适配层换算成角色精灵的视觉 Y 偏移（倾斜俯视角下不需要真实 Z 轴）。
    /// </summary>
    public static class JumpTrajectory
    {
        public static float Duration(float distance, float planarSpeed)
            => distance / Mathf.Max(0.01f, planarSpeed);

        /// <param name="t01">0~1 进度</param>
        /// <param name="height01">腾空高度系数：0 起 1 顶点 0 落</param>
        public static Vector2 Eval(Vector2 from, Vector2 to, float t01, out float height01)
        {
            t01 = Mathf.Clamp01(t01);
            height01 = 4f * t01 * (1f - t01);
            return Vector2.LerpUnclamped(from, to, t01);
        }
    }
}
