using System;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 追击时避开树的一步。冲刺不走这里，冲刺撞树要结算伤害。
    /// </summary>
    public static class Demo2BossSteering
    {
        /// <summary>
        /// 整步被挡住时，优先保留更大的轴向分量；两轴都挡住则停住。
        /// </summary>
        public static void ChooseStep(
            float desiredX,
            float desiredY,
            bool fullBlocked,
            bool xBlocked,
            bool yBlocked,
            out float stepX,
            out float stepY)
        {
            if (!fullBlocked)
            {
                stepX = desiredX;
                stepY = desiredY;
                return;
            }

            bool preferX = Math.Abs(desiredX) >= Math.Abs(desiredY);
            if (preferX && !xBlocked)
            {
                stepX = desiredX;
                stepY = 0f;
                return;
            }

            if (!yBlocked)
            {
                stepX = 0f;
                stepY = desiredY;
                return;
            }

            if (!xBlocked)
            {
                stepX = desiredX;
                stepY = 0f;
                return;
            }

            stepX = 0f;
            stepY = 0f;
        }
    }
}
