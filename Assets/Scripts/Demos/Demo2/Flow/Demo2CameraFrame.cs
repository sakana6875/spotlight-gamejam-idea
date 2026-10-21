
namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 把镜头中心夹进世界矩形，使画面边缘不超出该矩形。
    /// 某一轴上画面比矩形更大时，该轴锁在矩形中心。规则与侧视的窗口夹取一致。
    /// </summary>
    public static class Demo2CameraFrame
    {
        /// <summary>
        /// 夹取镜头中心。半宽和半高是当前画面的一半，不是矩形的一半。
        /// </summary>
        public static void ClampCenter(
            float centerX,
            float centerY,
            float minX,
            float maxX,
            float minY,
            float maxY,
            float halfWidth,
            float halfHeight,
            out float clampedX,
            out float clampedY)
        {
            clampedX = ClampAxis(centerX, minX, maxX, halfWidth);
            clampedY = ClampAxis(centerY, minY, maxY, halfHeight);
        }

        /// <summary>
        /// 夹取单轴中心。画面比区间更宽时返回区间中点。
        /// </summary>
        public static float ClampAxis(float center, float min, float max, float halfSize)
        {
            float minCenter = min + halfSize;
            float maxCenter = max - halfSize;
            if (minCenter > maxCenter)
            {
                return (min + max) * 0.5f;
            }

            if (center < minCenter)
            {
                return minCenter;
            }

            if (center > maxCenter)
            {
                return maxCenter;
            }

            return center;
        }
    }
}
