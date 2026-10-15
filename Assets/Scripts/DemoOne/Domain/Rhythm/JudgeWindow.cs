using System;

namespace Spotlight.Features.DemoOne.Domain.Rhythm
{
    /// <summary>
    /// 判定窗口配置。时间单位为秒，均为正数且要求 Perfect &lt;= Good。
    /// 边界包含：delta 的绝对值 &lt;= PerfectSeconds 判 Perfect，&lt;= GoodSeconds 判 Good。
    /// </summary>
    public readonly struct JudgeWindow
    {
        /// <summary>Perfect 判定窗口（秒）。</summary>
        public readonly float PerfectSeconds;

        /// <summary>Good 判定窗口（秒）。超过此窗口将被判定为 Miss。</summary>
        public readonly float GoodSeconds;

        public JudgeWindow(float perfectSeconds, float goodSeconds)
        {
            if (perfectSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(perfectSeconds));
            }
            if (goodSeconds < perfectSeconds)
            {
                throw new ArgumentException("GoodSeconds 必须大于等于 PerfectSeconds。");
            }
            PerfectSeconds = perfectSeconds;
            GoodSeconds = goodSeconds;
        }
    }
}