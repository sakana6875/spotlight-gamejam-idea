using System;
using UnityEngine;
using Spotlight.Demos.Demo1.Application;

namespace Spotlight.Audio
{
    /// <summary>
    /// 持有跨场景音乐源并提供基于 DSP 时间的播放能力。
    /// </summary>
    public sealed class AudioManager : IMusicPlaybackService
    {
        private readonly AudioSource _musicSource;

        /// <summary>
        /// 获取音乐源是否正在播放已调度音乐。
        /// </summary>
        public bool IsMusicPlaying => _musicSource.isPlaying;

        /// <summary>
        /// 使用组合根持有的音乐源创建音频服务。
        /// </summary>
        public AudioManager(AudioSource musicSource)
        {
            _musicSource = musicSource ?? throw new ArgumentNullException(nameof(musicSource));
            _musicSource.playOnAwake = false;
        }

        /// <inheritdoc />
        public double PlayMusicAtDspTime(AudioClip clip, double dspTime)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            _musicSource.Stop();
            _musicSource.clip = clip;

            double currentDspTime = AudioSettings.dspTime;
            if (dspTime <= currentDspTime)
            {
                _musicSource.Play();
                return currentDspTime;
            }

            _musicSource.PlayScheduled(dspTime);
            return dspTime;
        }

        /// <inheritdoc />
        public void StopMusic()
        {
            _musicSource.Stop();
        }
    }
}
