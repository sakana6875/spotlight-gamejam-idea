using UnityEngine;

namespace Spotlight.Demos.Demo1.Application
{
    /// <summary>
    /// 为 Demo1 提供基于 DSP 时间的音乐播放边界。
    /// </summary>
    public interface IMusicPlaybackService
    {
        /// <summary>
        /// 调度音乐并返回实际采用的 DSP 开始时间。
        /// </summary>
        double PlayMusicAtDspTime(AudioClip clip, double dspTime);

        /// <summary>
        /// 停止当前音乐。
        /// </summary>
        void StopMusic();
    }
}
