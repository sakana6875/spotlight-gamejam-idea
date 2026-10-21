using System;
using Spotlight.Domain.Story;
using UnityEngine;

namespace Spotlight.Story
{
    /// <summary>
    /// 说话人稳定 ID 到显示名和立绘的映射。位置列为空，不在这里发明站位。
    /// </summary>
    public sealed class StorySpeakerCatalog : ScriptableObject
    {
        [SerializeField] private Sprite _hallBackground;
        [SerializeField] private StorySpeakerEntry[] _speakers = Array.Empty<StorySpeakerEntry>();

        public Sprite HallBackground => _hallBackground;

        public bool TryGet(string speakerId, out StorySpeakerEntry entry)
        {
            if (_speakers != null)
            {
                for (int index = 0; index < _speakers.Length; index++)
                {
                    StorySpeakerEntry candidate = _speakers[index];
                    if (candidate != null && candidate.SpeakerId == speakerId)
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }
    }

    /// <summary>
    /// 一个可被 UI 显示的说话人。
    /// </summary>
    [Serializable]
    public sealed class StorySpeakerEntry
    {
        [SerializeField] private string _speakerId;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _portrait;

        public string SpeakerId => _speakerId;
        public string DisplayName => _displayName;
        public Sprite Portrait => _portrait;
    }
}
