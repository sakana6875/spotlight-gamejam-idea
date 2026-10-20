using System;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 提供水端口的反向、偏移和90度旋转规则。
    /// </summary>
    public static class Demo2WaterPortUtility
    {
        private const int QuarterTurnCount = 4;

        /// <summary>
        /// 返回一个单方向端口的反向端口。
        /// </summary>
        public static Demo2WaterPort Opposite(Demo2WaterPort port)
        {
            switch (port)
            {
                case Demo2WaterPort.Up:
                    return Demo2WaterPort.Down;
                case Demo2WaterPort.Right:
                    return Demo2WaterPort.Left;
                case Demo2WaterPort.Down:
                    return Demo2WaterPort.Up;
                case Demo2WaterPort.Left:
                    return Demo2WaterPort.Right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(port), "反向查询只接受一个正交方向。");
            }
        }

        /// <summary>
        /// 返回一个单方向端口对应的网格偏移。
        /// </summary>
        public static Vector2Int ToOffset(Demo2WaterPort port)
        {
            switch (port)
            {
                case Demo2WaterPort.Up:
                    return Vector2Int.up;
                case Demo2WaterPort.Right:
                    return Vector2Int.right;
                case Demo2WaterPort.Down:
                    return Vector2Int.down;
                case Demo2WaterPort.Left:
                    return Vector2Int.left;
                default:
                    throw new ArgumentOutOfRangeException(nameof(port), "网格偏移只接受一个正交方向。");
            }
        }

        /// <summary>
        /// 将端口集合顺时针旋转指定个90度。
        /// </summary>
        public static Demo2WaterPort RotateClockwise(Demo2WaterPort ports, int quarterTurns)
        {
            if (!IsValidMask(ports))
            {
                throw new ArgumentOutOfRangeException(nameof(ports), "端口集合包含未定义的位。");
            }

            int turns = NormalizeQuarterTurns(quarterTurns);
            Demo2WaterPort rotated = ports;
            for (int turn = 0; turn < turns; turn++)
            {
                Demo2WaterPort next = Demo2WaterPort.None;
                if ((rotated & Demo2WaterPort.Up) != 0)
                {
                    next |= Demo2WaterPort.Right;
                }

                if ((rotated & Demo2WaterPort.Right) != 0)
                {
                    next |= Demo2WaterPort.Down;
                }

                if ((rotated & Demo2WaterPort.Down) != 0)
                {
                    next |= Demo2WaterPort.Left;
                }

                if ((rotated & Demo2WaterPort.Left) != 0)
                {
                    next |= Demo2WaterPort.Up;
                }

                rotated = next;
            }

            return rotated;
        }

        /// <summary>
        /// 将本地网格偏移顺时针旋转指定个90度。
        /// </summary>
        public static Vector2Int RotateClockwise(Vector2Int offset, int quarterTurns)
        {
            int turns = NormalizeQuarterTurns(quarterTurns);
            Vector2Int rotated = offset;
            for (int turn = 0; turn < turns; turn++)
            {
                rotated = new Vector2Int(rotated.y, -rotated.x);
            }

            return rotated;
        }

        /// <summary>
        /// 将任意整数圈数规范到0至3。
        /// </summary>
        public static int NormalizeQuarterTurns(int quarterTurns)
        {
            int normalized = quarterTurns % QuarterTurnCount;
            return normalized < 0 ? normalized + QuarterTurnCount : normalized;
        }

        /// <summary>
        /// 判断端口集合是否只包含已定义方向。
        /// </summary>
        public static bool IsValidMask(Demo2WaterPort ports)
        {
            return (ports & ~Demo2WaterPort.All) == 0;
        }
    }
}
