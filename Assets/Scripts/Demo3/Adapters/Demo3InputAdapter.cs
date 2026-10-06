using System;
using Healing.Demo3.Application;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 输入适配层：读取 Input Actions 资产中的统一动作，转换为 IInputService。
    /// 把 InputControls 资产中的子动作拖到对应字段即可：
    ///   Move（WASD，必需）、Jump（空格，交互跳跃）、Interact（E，石碑交互）、Pause（可选）。
    /// 挂在 Demo3FlowCarrier（跨场景）或各幕场景内均可。
    /// </summary>
    public sealed class Demo3InputAdapter : MonoBehaviour, IInputService
    {
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference interactAction;
        [SerializeField] private InputActionReference pauseAction;

        public Vector2 Move { get; private set; }

        public event Action InteractPressed;
        public event Action JumpPressed;
        public event Action PulsePressed;
        public event Action SwitchModePressed;
        public event Action PausePressed;

        private void OnEnable()
        {
            Enable(moveAction, null);
            Enable(jumpAction, _ => JumpPressed?.Invoke());
            Enable(interactAction, _ => InteractPressed?.Invoke());
            Enable(pauseAction, _ => PausePressed?.Invoke());
        }

        private void OnDisable()
        {
            Disable(jumpAction, _ => JumpPressed?.Invoke());
            Disable(interactAction, _ => InteractPressed?.Invoke());
            Disable(pauseAction, _ => PausePressed?.Invoke());
        }

        private void Update()
        {
            Move = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        }

        private void Enable(InputActionReference r, Action<InputAction.CallbackContext> cb)
        {
            if (r == null) return;
            r.action.Enable();
            if (cb != null) r.action.performed += cb;
        }

        private void Disable(InputActionReference r, Action<InputAction.CallbackContext> cb)
        {
            if (r == null || cb == null) return;
            r.action.performed -= cb;
        }
    }
}
