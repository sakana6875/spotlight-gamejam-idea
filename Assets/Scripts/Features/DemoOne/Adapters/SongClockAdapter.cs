using UnityEngine;

namespace Spotlight.Features.DemoOne.Adapters
{
    /// <summary>
    /// 音频时钟适配器：把音乐开始时的 DSP 时间固定下来，
    /// 每帧通过 GetSongTimeSeconds 计算当前歌曲播放时间。
    /// 使用 AudioSettings.dspTime 而非 Time.time，避免帧率波动导致的判定漂移。
    /// </summary>
    public sealed class SongClockAdapter
    {
        private readonly double _startDspTime;

        public SongClockAdapter(double startDspTime)
        {
            _startDspTime = startDspTime;
        }

        /// <summary>当前歌曲播放时间（秒）。音乐尚未开始时可能为负。</summary>
        public float GetSongTimeSeconds()
        {
            return (float)(AudioSettings.dspTime - _startDspTime);
        }
    }
}