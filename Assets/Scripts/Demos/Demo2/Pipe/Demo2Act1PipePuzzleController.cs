using System;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 在SideView与TopView之外维护第一幕稳定Slot的共享占用和通水状态。
    /// </summary>
    public sealed class Demo2Act1PipePuzzleController : MonoBehaviour
    {
        [SerializeField] private Demo2PipeWaterController _waterController;
        [SerializeField] private Demo2PipeSegment[] _topViewPipeSegments;
        [SerializeField] private Demo2Act1PipeSlotDefinition[] _slots;

        private bool[] _occupiedStates;
        private bool[] _wetStates;
        private bool _isConfigured;
        private bool _isDirty;

        /// <summary>
        /// Slot占用或通水状态改变时发布的事实。
        /// </summary>
        public event Action<Demo2Act1PipeSlotChangedEvent> SlotChanged;

        public bool IsConfigured => _isConfigured;

        private void Awake()
        {
            if (_waterController == null || _slots == null || _slots.Length == 0 ||
                _topViewPipeSegments == null)
            {
                DisableForInvalidConfiguration("必须配置PipeWaterController、Slot和TopView管段数组。");
                return;
            }

            for (int index = 0; index < _slots.Length; index++)
            {
                Demo2Act1PipeSlotDefinition slot = _slots[index];
                if (slot == null)
                {
                    DisableForInvalidConfiguration($"Slot {index} 不能为空。");
                    return;
                }

                if (!slot.TryValidate(out string error))
                {
                    DisableForInvalidConfiguration($"Slot {index} 无效：{error}");
                    return;
                }
                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(_slots[previous].SlotId, slot.SlotId, StringComparison.Ordinal))
                    {
                        DisableForInvalidConfiguration("存在重复Slot ID：" + slot.SlotId);
                        return;
                    }
                }
            }

            for (int index = 0; index < _topViewPipeSegments.Length; index++)
            {
                if (_topViewPipeSegments[index] == null)
                {
                    DisableForInvalidConfiguration("TopView管段数组包含空引用。");
                    return;
                }
            }

            _occupiedStates = new bool[_slots.Length];
            _wetStates = new bool[_slots.Length];
            _isConfigured = true;
            _isDirty = true;
        }

        private void OnEnable()
        {
            if (_isConfigured)
            {
                _waterController.PipePlacementChanged += HandlePipePlacementChanged;
            }
        }

        private void Start()
        {
            if (!_isConfigured)
            {
                return;
            }

            for (int index = 0; index < _topViewPipeSegments.Length; index++)
            {
                if (!_topViewPipeSegments[index].EnsureInitializedAndRegistered())
                {
                    DisableForInvalidConfiguration(
                        "无法初始化TopView管段：" + _topViewPipeSegments[index].name);
                    return;
                }
            }

            RefreshSlots(true);
        }

        private void FixedUpdate()
        {
            if (_isConfigured)
            {
                RefreshSlots(_isDirty);
            }
        }

        private void OnDisable()
        {
            if (_waterController != null)
            {
                _waterController.PipePlacementChanged -= HandlePipePlacementChanged;
            }
        }

        /// <summary>
        /// 查询稳定Slot当前的占用和通水状态。
        /// </summary>
        public bool TryGetSlotState(string slotId, out bool isOccupied, out bool isWet)
        {
            int index = FindSlotIndex(slotId);
            if (!_isConfigured || index < 0)
            {
                isOccupied = false;
                isWet = false;
                return false;
            }

            isOccupied = _occupiedStates[index];
            isWet = _wetStates[index];
            return true;
        }

        private void HandlePipePlacementChanged(Demo2PipePlacementChangedEvent _)
        {
            _isDirty = true;
        }

        private void RefreshSlots(bool forcePublish)
        {
            _isDirty = false;
            for (int slotIndex = 0; slotIndex < _slots.Length; slotIndex++)
            {
                Demo2Act1PipeSlotDefinition slot = _slots[slotIndex];
                bool isOccupied = false;
                for (int pipeIndex = 0; pipeIndex < _topViewPipeSegments.Length; pipeIndex++)
                {
                    if (slot.Matches(_topViewPipeSegments[pipeIndex]))
                    {
                        isOccupied = true;
                        break;
                    }
                }

                bool isWet = isOccupied && slot.IsFullyWet(_waterController);
                if (!forcePublish && _occupiedStates[slotIndex] == isOccupied &&
                    _wetStates[slotIndex] == isWet)
                {
                    continue;
                }

                _occupiedStates[slotIndex] = isOccupied;
                _wetStates[slotIndex] = isWet;
                SlotChanged?.Invoke(new Demo2Act1PipeSlotChangedEvent(slot.SlotId, isOccupied, isWet));
            }
        }

        private int FindSlotIndex(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId) || _slots == null)
            {
                return -1;
            }

            for (int index = 0; index < _slots.Length; index++)
            {
                if (string.Equals(_slots[index].SlotId, slotId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private void DisableForInvalidConfiguration(string reason)
        {
            Debug.LogError("第一幕共享管道配置无效：" + reason, this);
            _isConfigured = false;
            enabled = false;
        }
    }
}
