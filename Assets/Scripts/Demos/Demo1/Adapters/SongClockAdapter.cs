using UnityEngine;

namespace Spotlight.Demos.Demo1.Adapters
{
    /// <summary>
    /// 使用实际音乐 DSP 起点计算当前歌曲时间。
    /// </summary>
    public sealed class SongClockAdapter
    {
        private readonly double _startDspTime;

        public SongClockAdapter(double startDspTime)
        {
            _startDspTime = startDspTime;
        }

        /// <summary>
        /// 获取相对音乐起点的秒数；尚未开始时返回负数。
        /// </summary>
        public float GetSongTimeSeconds()
        {
            return (float)(AudioSettings.dspTime - _startDspTime);
        }
    }
}
