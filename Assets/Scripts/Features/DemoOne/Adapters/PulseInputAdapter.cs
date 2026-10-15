using UnityEngine;
using UnityEngine.InputSystem;
using Spotlight.Contracts;

namespace Spotlight.Features.DemoOne.Adapters
{
    /// <summary>
    /// 读取玩家 Pulse 动作的适配器。
    /// 通过 InputActionReference 绑定到具体的按键（例如 Space），
    /// 玩法层不感知具体键位。
    /// </summary>
    public sealed class PulseInputAdapter : MonoBehaviour, IInputPulseReader
    {
        [SerializeField] private InputActionReference _pulseAction;

        public bool PulsePressed
        {
            get
            {
                if (_pulseAction == null || _pulseAction.action == null)
                {
                    return false;
                }
                return _pulseAction.action.WasPerformedThisFrame();
            }
        }

        private void OnEnable()
        {
            if (_pulseAction != null && _pulseAction.action != null)
            {
                _pulseAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (_pulseAction != null && _pulseAction.action != null)
            {
                _pulseAction.action.Disable();
            }
        }
    }
}