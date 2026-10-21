using System;
using UnityEngine;
using Spotlight.Demos.Demo1.Application;

namespace Spotlight.Demos.Demo1.Adapters
{
    /// <summary>
    /// 每帧把歌曲时间和 Pulse 输入送入 Demo1 单局控制器。
    /// </summary>
    public sealed class DemoOneRunnerBehaviour : MonoBehaviour
    {
        // 这些引用由运行时装配，不能进场景序列化。域重载会丢掉它们，但会保留普通 bool。
        [NonSerialized] private IDemoOneRunController _controller;
        [NonSerialized] private SongClockAdapter _clock;
        [NonSerialized] private IInputPulseReader _pulse;
        [NonSerialized] private bool _isInitialized;

        /// <summary>
        /// 绑定本局控制器、歌曲时钟和输入读取器。
        /// </summary>
        public void Initialize(
            IDemoOneRunController controller,
            SongClockAdapter clock,
            IInputPulseReader pulse)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _pulse = pulse ?? throw new ArgumentNullException(nameof(pulse));
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
