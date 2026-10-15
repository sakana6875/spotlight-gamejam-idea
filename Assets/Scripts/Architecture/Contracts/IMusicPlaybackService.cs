namespace Spotlight.Contracts
{
    /// <summary>
    /// 音乐播放与音效反馈端口。由音频适配器实现，玩法层不直接操作 AudioSource。
    /// 与全局 IAudioService 的分工：IAudioService 负责音量与 Mixer 分组，
    /// 本接口负责按 DSP 时间调度播放和播放一次性音效。
    /// </summary>
    public interface IMusicPlaybackService
    {
        /// <summary>
        /// 按 DSP 时间调度播放一段音乐。返回实际开始时间（DSP 秒），
        /// 调用方以此为基准计算歌曲播放时间。
        /// </summary>
        double PlayMusicAtDspTime(string audioCueId, double dspTime);

        /// <summary>停止当前正在播放的音乐。</summary>
        void StopMusic();

        /// <summary>播放一次性音效，用于判定反馈。</summary>
        void PlaySfx(string audioCueId);
    }
}