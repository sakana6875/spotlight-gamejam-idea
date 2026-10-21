using Cinemachine;
using System.Collections;
using Spotlight.Input;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 处理 Demo2 双视图根节点切换与固定时序的相机转场。
    /// </summary>
    public sealed class Demo2ViewModeController : MonoBehaviour
    {
        private const float TransitionSegmentDuration = 0.5f;
        private const int GameplayPriority = 20;
        private const int TransitionInactivePriority = 10;
        private const int TransitionActivePriority = 30;

        [SerializeField] private PlayerMoveController _playerMoveController;
        [SerializeField] private Demo2SimulationController _simulationController;
        [SerializeField] private CanvasGroup _blackoutCanvasGroup;
        [SerializeField] private GameObject _sideView;
        [SerializeField] private GameObject _topView;
        [SerializeField] private CinemachineBrain _cameraBrain;
        [SerializeField] private CinemachineVirtualCamera _gameplayCamera;
        [SerializeField] private CinemachineVirtualCamera _viewTransitionCamera;

        [Header("玩家落点")]
        [SerializeField] private float _sideLandingY;
        [SerializeField] private float _topLandingY;

        private GameInputActions _input;
        private Coroutine _transitionRoutine;
        private bool _isConfigured;
        private bool _isTransitioning;

        private void Awake()
        {
            if (_playerMoveController == null || _simulationController == null || _blackoutCanvasGroup == null ||
                _sideView == null || _topView == null || _sideView == _topView || _cameraBrain == null ||
                _gameplayCamera == null || _viewTransitionCamera == null || _gameplayCamera == _viewTransitionCamera)
            {
                Debug.LogError("Demo2ViewModeController 缺少有效的转场配置引用。", this);
                enabled = false;
                return;
            }

            _input = new GameInputActions();
            RestoreGameplayCamera();
            SetBlackout(0f, false);
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
            _input.Demo2.Enable();
        }

        private void OnDisable()
        {
            if (!_isConfigured)
            {
                return;
            }

            _input.Demo2.Disable();
            _playerMoveController.ViewModeChanged -= ApplyViewMode;
            CancelTransition();
        }

        private void OnDestroy()
        {
            _input?.Dispose();
        }

        private void Update()
        {
            if (_isConfigured && _input.Demo2.SwitchView.WasPressedThisFrame())
            {
                TrySwitchView();
            }
        }

        /// <summary>
        /// 尝试启动一次视图转场；冻结期间与进行中的请求会被吞掉。
        /// </summary>
        public bool TrySwitchView()
        {
            if (!_isConfigured || _isTransitioning || _simulationController.IsGameplayFrozen ||
                !_playerMoveController.CanSwitchView)
            {
                return false;
            }

            _playerMoveController.CancelCurrentActionForViewTransition();

            _isTransitioning = true;
            _transitionRoutine = StartCoroutine(TransitionView());
            return true;
        }

        private IEnumerator TransitionView()
        {
            PlayerMoveController.Demo2ViewMode targetMode =
                _playerMoveController.ViewMode == PlayerMoveController.Demo2ViewMode.Side
                    ? PlayerMoveController.Demo2ViewMode.Top
                    : PlayerMoveController.Demo2ViewMode.Side;

            _simulationController.AcquirePause(Demo2PauseReason.ViewTransition);
            SetBlackout(0f, true);

            _viewTransitionCamera.Priority = TransitionActivePriority;
            yield return WaitForCameraBlend();
            yield return FadeBlackout(0f, 1f, TransitionSegmentDuration);

            Vector2 targetPosition = _playerMoveController.transform.position;
            targetPosition.y = targetMode == PlayerMoveController.Demo2ViewMode.Side
                ? _sideLandingY
                : _topLandingY;

            _playerMoveController.SetViewMode(targetMode, targetPosition);
            _gameplayCamera.PreviousStateIsValid = false;
            _viewTransitionCamera.Priority = TransitionInactivePriority;

            yield return FadeBlackoutAndWaitForBlend(1f, 0f, TransitionSegmentDuration);
            CompleteTransition();
        }

        private IEnumerator WaitForCameraBlend()
        {
            // Brain在LateUpdate选择新镜头，先等待一帧再检查实际Blend状态。
            yield return null;

            while (_cameraBrain.IsBlending)
            {
                yield return null;
            }
        }

        private IEnumerator FadeBlackout(float startAlpha, float endAlpha, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                _blackoutCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
                yield return null;
            }

            _blackoutCanvasGroup.alpha = endAlpha;
        }

        private IEnumerator FadeBlackoutAndWaitForBlend(float startAlpha, float endAlpha, float duration)
        {
            yield return FadeBlackout(startAlpha, endAlpha, duration);

            while (_cameraBrain.IsBlending)
            {
                yield return null;
            }
        }

        private void ApplyViewMode(PlayerMoveController.Demo2ViewMode viewMode)
        {
            bool isSideView = viewMode == PlayerMoveController.Demo2ViewMode.Side;
            _sideView.SetActive(isSideView);
            _topView.SetActive(!isSideView);
        }

        private void CompleteTransition()
        {
            RestoreGameplayCamera();
            SetBlackout(0f, false);
            _simulationController.ReleasePause(Demo2PauseReason.ViewTransition);
            _transitionRoutine = null;
            _isTransitioning = false;
        }

        private void RestoreGameplayCamera()
        {
            _gameplayCamera.Priority = GameplayPriority;
            _gameplayCamera.PreviousStateIsValid = false;
            _viewTransitionCamera.Priority = TransitionInactivePriority;
        }

        private void CancelTransition()
        {
            if (!_isTransitioning)
            {
                return;
            }

            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
            }

            CompleteTransition();
        }

        private void SetBlackout(float alpha, bool blocksRaycasts)
        {
            _blackoutCanvasGroup.alpha = alpha;
            _blackoutCanvasGroup.blocksRaycasts = blocksRaycasts;
        }
    }
}
