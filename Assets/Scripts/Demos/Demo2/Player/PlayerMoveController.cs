using System;
using Spotlight.Input;
using UnityEngine;
using UnityEngine.Serialization;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 处理 Demo2 玩家在侧视和俯视模式下的输入与 Rigidbody2D 移动规则。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
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

        [Header("攻击判定")]
        [SerializeField] private Demo2PlayerMeleeAttack _meleeAttack;

        private static readonly int StateParameter = Animator.StringToHash("State");
        private static readonly int IdleState = Animator.StringToHash("Base Layer.PlayerIdle");
        private static readonly int MovingState = Animator.StringToHash("Base Layer.PlayerRun");
        private static readonly int Attack1State = Animator.StringToHash("Base Layer.PlayerAttack1");
        private static readonly int Attack2State = Animator.StringToHash("Base Layer.PlayerAttack2");
        private static readonly int HitStunState = Animator.StringToHash("Base Layer.PlayerHitted");

        private Rigidbody2D _rigidbody;
        private Animator _animator;
        private GameInputActions _input;
        private Demo2PlayerStateMachine _playerStateMachine;
        private float _horizontalDirection;
        private float _sideGravityScale;
        private Vector2 _topMovementDirection;
        private Vector2 _previousMoveInput;
        private bool _jumpRequested;
        private bool _isGrounded;
        private bool _isFacingRight = true;
        private bool _isConfigured;
        private bool _isAnimatorConfigured;
        private Demo2PlayerState _appliedAnimatorState = (Demo2PlayerState)(-1);
        private Demo2ViewMode _viewMode = Demo2ViewMode.Side;
        private TopMovementAxis _topMovementAxis = TopMovementAxis.Horizontal;
        public Demo2ViewMode ViewMode => _viewMode; // 当前视角模式
        /// <summary>
        /// 玩家当前在侧视规则下朝向右侧。
        /// </summary>
        public bool IsFacingRight => _isFacingRight;

        /// <summary>
        /// 玩家当前由玩法规则确定的状态。
        /// </summary>
        public Demo2PlayerState State => _playerStateMachine?.State ?? Demo2PlayerState.Idle;

        /// <summary>
        /// 当前玩家状态是否允许推动石头。
        /// </summary>
        public bool CanPush => _playerStateMachine != null && _playerStateMachine.CanPush;

        /// <summary>
        /// 玩家移动规则已经切换到指定视图模式的事实通知。
        /// </summary>
        public event Action<Demo2ViewMode> ViewModeChanged;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _input = new GameInputActions();
            _playerStateMachine = new Demo2PlayerStateMachine();

            if (_rigidbody == null || _animator == null || _groundCheck == null || _meleeAttack == null)
            {
                Debug.LogError(
                    "PlayerMoveController 缺少 Rigidbody2D、Animator、GroundCheck 或近战组件引用。",
                    this);
                enabled = false;
                return;
            }

            _isAnimatorConfigured = ValidateAnimatorConfiguration();
            _sideGravityScale = _rigidbody.gravityScale;
            _isConfigured = true;
            ApplyAnimatorState();
        }

        private void OnEnable()
        {
            if (_isConfigured)
            {
                _input.Demo2.Enable();
                _appliedAnimatorState = (Demo2PlayerState)(-1);
                ApplyAnimatorState();
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
            bool isMoving = _viewMode == Demo2ViewMode.Side
                ? !Mathf.Approximately(moveInput.x, 0f)
                : !Mathf.Approximately(moveInput.x, 0f) || !Mathf.Approximately(moveInput.y, 0f);
            bool attackPressed = _input.Demo2.Attack.WasPressedThisFrame();

            if (_viewMode == Demo2ViewMode.Side && !Mathf.Approximately(moveInput.x, 0f))
            {
                SetFacingDirection(moveInput.x > 0f);
            }

            Demo2PlayerAdvanceResult advanceResult =
                _playerStateMachine.Advance(Time.deltaTime, isMoving, attackPressed);
            ApplyAnimatorState();
            if (advanceResult != Demo2PlayerAdvanceResult.None)
            {
                _meleeAttack.ResolveStrike();
            }

            if (!_playerStateMachine.CanMove)
            {
                ClearControlInput();
                return;
            }

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
                _rigidbody.velocity = _playerStateMachine.CanMove
                    ? _topMovementDirection * _moveSpeed
                    : Vector2.zero;
                _jumpRequested = false;
                return;
            }

            _isGrounded = Physics2D.OverlapCircle(
                _groundCheck.position,
                _groundCheckRadius,
                _groundLayer);

            Vector2 velocity = _rigidbody.velocity;
            velocity.x = _playerStateMachine.CanMove ? _horizontalDirection * _moveSpeed : 0f;

            if (_playerStateMachine.CanMove && _isGrounded && _jumpRequested)
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
            ClearControlInput();
            _isGrounded = false;
        }

        private void ClearControlInput()
        {
            _horizontalDirection = 0f;
            _topMovementDirection = Vector2.zero;
            _previousMoveInput = Vector2.zero;
            _jumpRequested = false;
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
        /// 中断当前动作并进入玩家受击硬直。
        /// </summary>
        public bool TryEnterHitStun()
        {
            if (!_isConfigured || !_playerStateMachine.TryEnterHitStun())
            {
                return false;
            }

            ClearControlInput();
            ApplyAnimatorState();
            return true;
        }

        /// <summary>
        /// 在视角转场开始前取消攻击、受击和二段排队。
        /// </summary>
        public void CancelCurrentActionForViewTransition()
        {
            if (!_isConfigured)
            {
                return;
            }

            _playerStateMachine.CancelCurrentAction();
            ClearMovementState();
            ApplyAnimatorState();
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

        private bool ValidateAnimatorConfiguration()
        {
            if (_animator.runtimeAnimatorController == null)
            {
                return false;
            }

            bool hasStateParameter = false;
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == StateParameter && parameter.type == AnimatorControllerParameterType.Int)
                {
                    hasStateParameter = true;
                    break;
                }
            }

            bool hasRequiredStates = _animator.HasState(0, IdleState) &&
                _animator.HasState(0, MovingState) &&
                _animator.HasState(0, Attack1State) &&
                _animator.HasState(0, Attack2State) &&
                _animator.HasState(0, HitStunState);

            if (!hasStateParameter || !hasRequiredStates)
            {
                Debug.LogError("Demo2 玩家 Animator 缺少 State 参数或必需动画状态。", this);
                return false;
            }

            return true;
        }

        private void ApplyAnimatorState()
        {
            if (!_isAnimatorConfigured || _appliedAnimatorState == _playerStateMachine.State)
            {
                return;
            }

            _appliedAnimatorState = _playerStateMachine.State;
            _animator.SetInteger(StateParameter, (int)_appliedAnimatorState);
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
