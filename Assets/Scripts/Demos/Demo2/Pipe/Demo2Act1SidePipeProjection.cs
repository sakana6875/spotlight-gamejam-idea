using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 将第一幕稳定Slot投影为SideView中的干管或通水视觉。
    /// </summary>
    public sealed class Demo2Act1SidePipeProjection : MonoBehaviour
    {
        [SerializeField] private Demo2Act1PipePuzzleController _puzzleController;
        [SerializeField] private string _slotId;
        [SerializeField] private GameObject _dryVisual;
        [SerializeField] private GameObject _wetVisual;

        private bool _isConfigured;

        private void Awake()
        {
            if (_puzzleController == null || string.IsNullOrWhiteSpace(_slotId) ||
                _dryVisual == null || _wetVisual == null || _dryVisual == _wetVisual)
            {
                Debug.LogError("Demo2Act1SidePipeProjection缺少有效的Controller、slotId或视觉引用。", this);
                enabled = false;
                return;
            }

            SetVisualState(false, false);
            _isConfigured = true;
        }

        private void OnEnable()
        {
            if (!_isConfigured)
            {
                return;
            }

            _puzzleController.SlotChanged += HandleSlotChanged;
            if (_puzzleController.TryGetSlotState(_slotId, out bool occupied, out bool wet))
            {
                SetVisualState(occupied, wet);
            }
        }

        private void Start()
        {
            if (!_isConfigured || !_puzzleController.TryGetSlotState(_slotId, out bool occupied, out bool wet))
            {
                Debug.LogError("SideView投影引用了不存在的Slot：" + _slotId, this);
                enabled = false;
                return;
            }

            SetVisualState(occupied, wet);
        }

        private void OnDisable()
        {
            if (_puzzleController != null)
            {
                _puzzleController.SlotChanged -= HandleSlotChanged;
            }
        }

        private void HandleSlotChanged(Demo2Act1PipeSlotChangedEvent change)
        {
            if (change.SlotId == _slotId)
            {
                SetVisualState(change.IsOccupied, change.IsWet);
            }
        }

        private void SetVisualState(bool isOccupied, bool isWet)
        {
            _dryVisual.SetActive(isOccupied && !isWet);
            _wetVisual.SetActive(isOccupied && isWet);
        }
    }
}
