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

        [SerializeField] private PlayerMoveController _playerMoveController;
        [SerializeField] private Demo2CameraFollowController _cameraFollowController;
        [SerializeField] private Demo2SimulationController _simulationController;
        [SerializeField] private CanvasGroup _blackoutCanvasGroup;
        [SerializeField] private GameObject _sideView;
        [SerializeField] private GameObject _topView;
        [SerializeField] private Transform _sideGameplayAnchor;
        [SerializeField] private Transform _sideCloseupAnchor;
        [SerializeField] private Transform _topGameplayAnchor;
        [SerializeField] private Transform _topCloseupAnchor;

        [Header("玩家落点")]
        [SerializeField] private float _sideLandingY;

        private GameInputActions _input;
        private Coroutine _transitionRoutine;
        private bool _isConfigured;
        private bool _isTransitioning;

        private void Awake()
        {
            if (_playerMoveController == null || _cameraFollowController == null || _simulationController == null ||
                _blackoutCanvasGroup == null || _sideView == null || _topView == null || _sideView == _topView ||
                _sideGameplayAnchor == null || _sideCloseupAnchor == null || _topGameplayAnchor == null ||
                _topCloseupAnchor == null)
            {
                Debug.LogError("Demo2ViewModeController 缺少有效的转场配置引用。", this);
                enabled = false;
                return;
            }

            _input = new GameInputActions();
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
            if (!_isConfigured || _isTransitioning || _simulationController.IsGameplayFrozen)
            {
                return false;
            }

            _isTransitioning = true;
            _transitionRoutine = StartCoroutine(TransitionView());
            return true;
        }

        private IEnumerator TransitionView()
        {
            PlayerMoveController.Demo2ViewMode currentMode = _playerMoveController.ViewMode;
            PlayerMoveController.Demo2ViewMode targetMode = currentMode == PlayerMoveController.Demo2ViewMode.Side
                ? PlayerMoveController.Demo2ViewMode.Top
                : PlayerMoveController.Demo2ViewMode.Side;

            Transform sourceCloseup = currentMode == PlayerMoveController.Demo2ViewMode.Side
                ? _sideCloseupAnchor
                : _topCloseupAnchor;

            Transform targetGameplay = targetMode == PlayerMoveController.Demo2ViewMode.Side
                ? _sideGameplayAnchor
                : _topGameplayAnchor;

            _simulationController.AcquirePause(Demo2PauseReason.ViewTransition);
            _cameraFollowController.SetFollowing(false);
            SetBlackout(0f, true);

            yield return MoveCamera(
                _cameraFollowController.transform.position,
                sourceCloseup.position,
                TransitionSegmentDuration);

            yield return FadeBlackout(0f, 1f, TransitionSegmentDuration);

            Vector2 targetPosition = _playerMoveController.transform.position;
            if (targetMode == PlayerMoveController.Demo2ViewMode.Side)
            {
                targetPosition.y = _sideLandingY;
            }

            _playerMoveController.SetViewMode(targetMode, targetPosition);

            yield return MoveCameraAndFadeOut(
                sourceCloseup.position,
                targetGameplay.position,
                TransitionSegmentDuration);

            CompleteTransition();
        }

        private IEnumerator MoveCamera(Vector3 startPosition, Vector3 endPosition, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                _cameraFollowController.transform.position = Vector3.Lerp(startPosition, endPosition, progress);
                yield return null;
            }

            _cameraFollowController.transform.position = endPosition;
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

        private IEnumerator MoveCameraAndFadeOut(Vector3 startPosition, Vector3 endPosition, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                _cameraFollowController.transform.position = Vector3.Lerp(startPosition, endPosition, progress);
                _blackoutCanvasGroup.alpha = 1f - progress;
                yield return null;
            }

            _cameraFollowController.transform.position = endPosition;
            _blackoutCanvasGroup.alpha = 0f;
        }

        private void ApplyViewMode(PlayerMoveController.Demo2ViewMode viewMode)
        {
            bool isSideView = viewMode == PlayerMoveController.Demo2ViewMode.Side;
            _sideView.SetActive(isSideView);
            _topView.SetActive(!isSideView);
        }

        private void CompleteTransition()
        {
            SetBlackout(0f, false);
            _simulationController.ReleasePause(Demo2PauseReason.ViewTransition);
            _cameraFollowController.SetFollowing(true);
            _transitionRoutine = null;
            _isTransitioning = false;
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
