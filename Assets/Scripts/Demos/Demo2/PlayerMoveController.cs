using System;
using Spotlight.Input;
using UnityEngine;
using UnityEngine.Serialization;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 处理 Demo2 玩家在侧视和俯视模式下的输入与 Rigidbody2D 移动规则。
    /// </summary>
    public sealed class PlayerMoveController : MonoBehaviour
    {
        /// <summary>
        /// Demo2 当前采用的地图呈现与玩家移动规则。
        /// </summary>
        public enum Demo2ViewMode
        {
            Side,
            Top
        }

        private enum TopMovementAxis
        {
            Horizontal,
            Vertical
        }

        [Header("移动参数")]
        [FormerlySerializedAs("moveSpeed")]
        [SerializeField] private float _moveSpeed = 1.5f;

        [Header("跳跃参数")]
        [FormerlySerializedAs("jumpHeight")]
        [SerializeField] private float _jumpHeight = 3.0f;

        [Header("接地检测")]
        [FormerlySerializedAs("groundCheck")]
        [SerializeField] private Transform _groundCheck;
        [FormerlySerializedAs("groundCheckRadius")]
        [SerializeField] private float _groundCheckRadius = 0.2f;
        [FormerlySerializedAs("groundLayer")]
        [SerializeField] private LayerMask _groundLayer;

        [Header("运行状态")]
        [SerializeField] private Demo2SimulationController _simulationController;

        private Rigidbody2D _rigidbody;
        private GameInputActions _input;
        private float _horizontalDirection;
        private float _sideGravityScale;
        private Vector2 _topMovementDirection;
        private Vector2 _previousMoveInput;
        private bool _jumpRequested;
        private bool _isGrounded;
        private bool _isFacingRight = true;
        private bool _isConfigured;
        private Demo2ViewMode _viewMode = Demo2ViewMode.Side;
        private TopMovementAxis _topMovementAxis = TopMovementAxis.Horizontal;
        
        public Demo2ViewMode ViewMode => _viewMode; // 当前视角模式

        /// <summary>
        /// 玩家移动规则已经切换到指定视图模式的事实通知。
        /// </summary>
        public event Action<Demo2ViewMode> ViewModeChanged;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _input = new GameInputActions();

            if (_rigidbody == null || _groundCheck == null)
            {
                Debug.LogError("PlayerMoveController 缺少 Rigidbody2D 或 GroundCheck 引用。", this);
                enabled = false;
                return;
            }

            _sideGravityScale = _rigidbody.gravityScale;
            _isConfigured = true;
        }

        private void OnEnable()
        {
            if (_isConfigured)
            {
                _input.Demo2.Enable();
            }
        }

        private void OnDisable()
        {
            _input?.Demo2.Disable();
        }

        private void OnDestroy()
        {
            _input?.Dispose();
        }

        private void Update()
        {
            if (!_isConfigured)
            {
                return;
            }

            if (IsGameplayFrozen)
            {
                ClearMovementState();
                return;
            }

            Vector2 moveInput = _input.Demo2.Move.ReadValue<Vector2>();

            if (_viewMode == Demo2ViewMode.Side)
            {
                UpdateSideMovement(moveInput);
                return;
            }

            UpdateTopMovement(moveInput);
        }

        private void FixedUpdate()
        {
            if (!_isConfigured || IsGameplayFrozen)
            {
                return;
            }

            if (_viewMode == Demo2ViewMode.Top)
            {
                _rigidbody.velocity = _topMovementDirection * _moveSpeed;
                return;
            }

            _isGrounded = Physics2D.OverlapCircle(
                _groundCheck.position,
                _groundCheckRadius,
                _groundLayer);

            Vector2 velocity = _rigidbody.velocity;
            velocity.x = _horizontalDirection * _moveSpeed;

            if (_isGrounded && _jumpRequested)
            {
                velocity.y = CalculateJumpSpeed();
            }

            _rigidbody.velocity = velocity;
            _jumpRequested = false;
        }

        private bool IsGameplayFrozen => _simulationController != null && _simulationController.IsGameplayFrozen;

        private void ClearMovementState()
        {
            _rigidbody.velocity = Vector2.zero;
            _horizontalDirection = 0f;
            _topMovementDirection = Vector2.zero;
            _previousMoveInput = Vector2.zero;
            _jumpRequested = false;
            _isGrounded = false;
        }

        private void SetPositionImmediately(Vector2 position)
        {
            Vector3 transformPosition = transform.position;
            transformPosition.x = position.x;
            transformPosition.y = position.y;
            transform.position = transformPosition;
            _rigidbody.position = position;
            Physics2D.SyncTransforms();
        }

        /// <summary>
        /// 以指定位置进入目标视图，确保侧视重力恢复前玩家已位于有效落点。
        /// </summary>
        public void SetViewMode(Demo2ViewMode viewMode, Vector2 position)
        {
            if (_viewMode == viewMode)
            {
                return;
            }

            _viewMode = viewMode;
            ClearMovementState();
            SetPositionImmediately(position);
            _rigidbody.gravityScale = _viewMode == Demo2ViewMode.Side ? _sideGravityScale : 0f;
            ViewModeChanged?.Invoke(_viewMode);
        }

        private void UpdateSideMovement(Vector2 moveInput)
        {
            _horizontalDirection = moveInput.x > 0f ? 1f : moveInput.x < 0f ? -1f : 0f;

            if (_horizontalDirection > 0f)
            {
                SetFacingDirection(true);
            }
            else if (_horizontalDirection < 0f)
            {
                SetFacingDirection(false);
            }

            if (_input.Demo2.Jump.WasPressedThisFrame())
            {
                _jumpRequested = true;
            }
        }

        private void UpdateTopMovement(Vector2 moveInput)
        {
            bool hasHorizontalInput = !Mathf.Approximately(moveInput.x, 0f);
            bool hasVerticalInput = !Mathf.Approximately(moveInput.y, 0f);

            if (hasHorizontalInput && Mathf.Approximately(_previousMoveInput.x, 0f))
            {
                _topMovementAxis = TopMovementAxis.Horizontal;
            }
            else if (hasVerticalInput && Mathf.Approximately(_previousMoveInput.y, 0f))
            {
                _topMovementAxis = TopMovementAxis.Vertical;
            }

            if (_topMovementAxis == TopMovementAxis.Horizontal && !hasHorizontalInput)
            {
                _topMovementAxis = TopMovementAxis.Vertical;
            }
            else if (_topMovementAxis == TopMovementAxis.Vertical && !hasVerticalInput)
            {
                _topMovementAxis = TopMovementAxis.Horizontal;
            }

            if (_topMovementAxis == TopMovementAxis.Horizontal && hasHorizontalInput)
            {
                float direction = moveInput.x > 0f ? 1f : -1f;
                _topMovementDirection = new Vector2(direction, 0f);
                SetFacingDirection(direction > 0f);
            }
            else if (_topMovementAxis == TopMovementAxis.Vertical && hasVerticalInput)
            {
                float direction = moveInput.y > 0f ? 1f : -1f;
                _topMovementDirection = new Vector2(0f, direction);
            }
            else
            {
                _topMovementDirection = Vector2.zero;
            }

            _previousMoveInput = moveInput;
        }

        private float CalculateJumpSpeed()
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y * _rigidbody.gravityScale);
            return Mathf.Sqrt(2f * gravity * _jumpHeight);
        }

        private void SetFacingDirection(bool isFacingRight)
        {
            if (_isFacingRight == isFacingRight)
            {
                return;
            }

            _isFacingRight = isFacingRight;

            Vector3 localScale = transform.localScale;
            localScale.x = Mathf.Abs(localScale.x) * (_isFacingRight ? 1f : -1f);
            transform.localScale = localScale;
        }
    }
}
