using System;
using Healing.Demo3.Application;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 输入适配层：读取 Input Actions 资产中的统一动作，转换为 IInputService。
    /// 玩法代码只依赖 IInputService，不读 Keyboard.current / Input.GetKey。
    ///
    /// 用法：在场景中新建空物体挂本组件，
    /// 把 InputControls 资产里 "Demo 3 Gameplay" 动作表下的 Move 动作
    /// 直接拖到 Move Action 字段（InputActionReference 支持选资产内的子动作），
    /// Pause 可选。正式接入全局架构后，此组件由全局 InputManager 替代。
    /// </summary>
    public sealed class Demo3InputAdapter : MonoBehaviour, IInputService
    {
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference pauseAction;

        public Vector2 Move { get; private set; }

        public event Action InteractPressed;
        public event Action PulsePressed;
        public event Action SwitchModePressed;
        public event Action PausePressed;

        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
            if (pauseAction != null)
            {
                pauseAction.action.Enable();
                pauseAction.action.performed += OnPause;
            }
        }

        private void OnDisable()
        {
            if (pauseAction != null) pauseAction.action.performed -= OnPause;
        }

        private void Update()
        {
            Move = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        }

        private void OnPause(InputAction.CallbackContext _) => PausePressed?.Invoke();
    }
}
