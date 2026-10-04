using System;
using System.Collections.Generic;
using Spotlight.Application.Services;

namespace Spotlight.Adapters.Session
{
    /// <summary>
    /// 记录音频通道音量的最小适配器，不直接操作 AudioSource 或 AudioMixer。
    /// </summary>
    public sealed class InMemoryAudioService : IAudioService
    {
        private readonly Dictionary<string, float> _volumes =
            new Dictionary<string, float>(StringComparer.Ordinal);

        public AudioServiceResult SetVolume(string channelId, float volume)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                return AudioServiceResult.Failed(AudioServiceResultCode.InvalidChannel);
            }

            if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0f || volume > 1f)
            {
                return AudioServiceResult.Failed(AudioServiceResultCode.InvalidVolume);
            }

            _volumes[channelId] = volume;
            return AudioServiceResult.Succeeded();
        }

        public float GetVolume(string channelId)
        {
            return channelId != null && _volumes.TryGetValue(channelId, out float volume)
                ? volume
                : 0f;
        }
    }
}
