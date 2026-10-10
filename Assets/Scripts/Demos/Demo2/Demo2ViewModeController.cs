using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 根据玩家已切换的视图模式，启用对应地图根对象。
    /// </summary>
    public sealed class Demo2ViewModeController : MonoBehaviour
    {
        [SerializeField] private PlayerMoveController _playerMoveController;
        [SerializeField] private GameObject _sideView;
        [SerializeField] private GameObject _topView;

        private bool _isConfigured;

        private void Awake()
        {
            if (_playerMoveController == null || _sideView == null || _topView == null || _sideView == _topView)
            {
                Debug.LogError("Demo2ViewModeController 缺少有效的玩家、SideView 或 TopView 引用。", this);
                enabled = false;
                return;
            }

            _isConfigured = true;
        }

        private void OnEnable()
        {
            if (!_isConfigured)
            {
                return;
            }

            _playerMoveController.ViewModeChanged += ApplyViewMode;
            ApplyViewMode(_playerMoveController.ViewMode);
        }

        private void OnDisable()
        {
            if (_isConfigured)
            {
                _playerMoveController.ViewModeChanged -= ApplyViewMode;
            }
        }

        private void ApplyViewMode(PlayerMoveController.Demo2ViewMode viewMode)
        {
            bool isSideView = viewMode == PlayerMoveController.Demo2ViewMode.Side;
            _sideView.SetActive(isSideView);
            _topView.SetActive(!isSideView);
        }
    }
}
