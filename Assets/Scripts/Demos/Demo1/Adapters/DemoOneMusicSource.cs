using System;
using UnityEngine;
using Spotlight.Contracts;

namespace Spotlight.Demos.Demo1.Adapters
{
    /// <summary>
    /// Demo1 场景内的音乐播放适配器。
    /// 实现 IMusicPlaybackService，使用场景内独立 AudioSource。
    /// 不操作全局 AudioManager，也不修改全局音量分组。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class DemoOneMusicSource : MonoBehaviour, IMusicPlaybackService
    {
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        private void Awake()
        {
            if (_musicSource == null)
            {
                _musicSource = GetComponent<AudioSource>();
            }
            if (_musicSource == null)
            {
                throw new InvalidOperationException(
                    "DemoOneMusicSource 需要至少一个 AudioSource 组件。");
            }
        }

        public double PlayMusicAtDspTime(string audioCueId, double dspTime)
        {
            if (_musicSource.clip == null)
            {
                throw new InvalidOperationException(
                    $"DemoOneMusicSource 尚未指定 AudioClip，无法播放 '{audioCueId}'。");
            }
            if (dspTime < AudioSettings.dspTime)
            {
                // 已过调度时间，直接立即播放；返回当前 DSP 时间作为基准。
                _musicSource.Play();
                return AudioSettings.dspTime;
            }
            _musicSource.PlayScheduled(dspTime);
            return dspTime;
        }

        public void StopMusic()
        {
            _musicSource.Stop();
        }

        public void PlaySfx(string audioCueId)
        {
            // 阶段 4 先不挂音效资产；阶段 5 场景搭建时再补 _sfxSource 与 clip。
            // 保留空实现是为了让 IMusicPlaybackService 契约完整。
        }
    }
}