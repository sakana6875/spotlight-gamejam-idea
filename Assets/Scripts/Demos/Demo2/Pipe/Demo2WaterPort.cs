using System;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 1米网格单元可连接的四个正交方向。
    /// </summary>
    [Flags]
    public enum Demo2WaterPort : byte
    {
        None = 0,
        Up = 1 << 0,
        Right = 1 << 1,
        Down = 1 << 2,
        Left = 1 << 3,
        All = Up | Right | Down | Left
    }
}
