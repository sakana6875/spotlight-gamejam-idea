using UnityEngine;
using UnityEngine.InputSystem;
using Spotlight.Demos.Demo1.Application;

namespace Spotlight.Demos.Demo1.Adapters
{
    /// <summary>
    /// 将统一 Input System 的 Pulse 动作转换为当前帧输入状态。
    /// </summary>
    public sealed class PulseInputAdapter : MonoBehaviour, IInputPulseReader
    {
        private const string PulseActionPath = "Gameplay/Pulse";

        [SerializeField] private InputActionAsset _inputActions;

        private InputAction _pulseAction;

        /// <inheritdoc />
        public bool PulsePressed => _pulseAction != null && _pulseAction.WasPerformedThisFrame();

        private void OnEnable()
        {
            if (_inputActions == null)
            {
                Debug.LogError("Demo1 Pulse 输入缺少统一 Input Actions 资产。", this);
                return;
            }

            _pulseAction = _inputActions.FindAction(PulseActionPath, true);
            _pulseAction.Enable();
        }

        private void OnDisable()
        {
            if (_pulseAction != null)
            {
                _pulseAction.Disable();
                _pulseAction = null;
            }
        }
    }
}
