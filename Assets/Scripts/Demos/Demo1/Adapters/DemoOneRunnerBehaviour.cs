using UnityEngine;
using Spotlight.Demos.Demo1.Application;

namespace Spotlight.Demos.Demo1.Adapters
{
    /// <summary>
    /// 每帧把歌曲时间和 Pulse 输入送入 Demo1 单局控制器。
    /// </summary>
    public sealed class DemoOneRunnerBehaviour : MonoBehaviour
    {
        private IDemoOneRunController _controller;
        private SongClockAdapter _clock;
        private IInputPulseReader _pulse;
        private bool _isInitialized;

        /// <summary>
        /// 绑定本局控制器、歌曲时钟和输入读取器。
        /// </summary>
        public void Initialize(
            IDemoOneRunController controller,
            SongClockAdapter clock,
            IInputPulseReader pulse)
        {
            _controller = controller;
            _clock = clock;
            _pulse = pulse;
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            _controller.Tick(_clock.GetSongTimeSeconds(), _pulse.PulsePressed);
        }
    }
}
