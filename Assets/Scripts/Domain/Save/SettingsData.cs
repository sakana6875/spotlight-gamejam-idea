namespace Spotlight.Domain.Save
{
    /// <summary>
    /// 保存可跨场景使用的音量设置；字段名保持稳定以供后续 Audio 服务使用。
    /// </summary>
    public sealed class SettingsData
    {
        public const string MasterVolumeKey = "masterVolume";
        public const string BgmVolumeKey = "bgmVolume";
        public const string SfxVolumeKey = "sfxVolume";
        public const string NarrationVolumeKey = "narrationVolume";

        public float MasterVolume { get; }
        public float BgmVolume { get; }
        public float SfxVolume { get; }
        public float NarrationVolume { get; }

        public SettingsData(
            float masterVolume,
            float bgmVolume,
            float sfxVolume,
            float narrationVolume)
        {
            MasterVolume = masterVolume;
            BgmVolume = bgmVolume;
            SfxVolume = sfxVolume;
            NarrationVolume = narrationVolume;
        }

        public SettingsData Clone()
        {
            return new SettingsData(MasterVolume, BgmVolume, SfxVolume, NarrationVolume);
        }
    }
}
