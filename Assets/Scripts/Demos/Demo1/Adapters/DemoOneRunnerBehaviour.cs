using UnityEngine;

namespace Spotlight.Demos.Demo1.Adapters
{
    /// <summary>
    /// Demo1 场景主驱动。每帧把歌曲时间和 Pulse 输入送入 Controller。
    /// 不承载玩法规则，也不直接操作 UI、音频或粒子。
    /// </summary>
    public sealed class DemoOneRunnerBehaviour : MonoBehaviour
    {
        private Application.IDemoOneRunController _controller;
        private SongClockAdapter _clock;
        private Contracts.IInputPulseReader _pulse;
        private bool _initialized;

        public void Initialize(
            Application.IDemoOneRunController controller,
            SongClockAdapter clock,
            Contracts.IInputPulseReader pulse)
        {
            _controller = controller;
            _clock = clock;
            _pulse = pulse;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;
            float songTime = _clock.GetSongTimeSeconds();
            bool pulse = _pulse.PulsePressed;
            _controller.Tick(songTime, pulse);
        }
    }
}