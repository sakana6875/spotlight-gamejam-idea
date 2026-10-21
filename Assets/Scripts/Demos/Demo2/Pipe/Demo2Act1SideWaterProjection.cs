using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 将共享TopView管网的有水事实投影为SideView水视觉。
    /// </summary>
    public sealed class Demo2Act1SideWaterProjection : MonoBehaviour
    {
        [SerializeField] private Demo2PipeWaterController _waterController;
        [SerializeField] private Demo2SideWaterVisualBinding[] _bindings;

        private bool _isConfigured;
        private bool _hasStarted;

        private void Awake()
        {
            if (_waterController == null || _bindings == null || _bindings.Length == 0)
            {
                DisableForInvalidConfiguration("必须配置PipeWaterController和至少一个水视觉绑定。");
                return;
            }

            for (int index = 0; index < _bindings.Length; index++)
            {
                Demo2SideWaterVisualBinding binding = _bindings[index];
                if (binding == null)
                {
                    DisableForInvalidConfiguration($"绑定{index}不能为空。");
                    return;
                }

                if (!binding.TryValidate(out string error))
                {
                    DisableForInvalidConfiguration($"绑定{index}无效：{error}");
                    return;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (_bindings[previous].WetVisual == binding.WetVisual)
                    {
                        DisableForInvalidConfiguration(
                            $"绑定{index}与绑定{previous}不能控制同一个Wet Visual。");
                        return;
                    }
                }

                binding.WetVisual.SetActive(false);
            }

            _isConfigured = true;
        }

        private void OnEnable()
        {
            if (!_isConfigured)
            {
                return;
            }

            _waterController.WaterStateChanged += HandleWaterStateChanged;
            if (_hasStarted)
            {
                RefreshVisuals();
            }
        }

        private void Start()
        {
            if (!_isConfigured)
            {
                return;
            }

            _hasStarted = true;
            RefreshVisuals();
        }

        private void OnDisable()
        {
            if (_waterController != null)
            {
                _waterController.WaterStateChanged -= HandleWaterStateChanged;
            }
        }

        private void HandleWaterStateChanged()
        {
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            for (int index = 0; index < _bindings.Length; index++)
            {
                Demo2SideWaterVisualBinding binding = _bindings[index];
                binding.WetVisual.SetActive(binding.IsWet(_waterController));
            }
        }

        private void DisableForInvalidConfiguration(string reason)
        {
            Debug.LogError("第一幕SideView水投影配置无效：" + reason, this);
            _isConfigured = false;
            enabled = false;
        }
    }
}
