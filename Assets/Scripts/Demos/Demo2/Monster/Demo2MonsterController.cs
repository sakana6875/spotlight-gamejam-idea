using System;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 将侧视小怪状态适配到物理、树苗登记、启停生命周期和 Animator。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Demo2MonsterController : MonoBehaviour, IMonster
    {
        private static readonly int StateParameter = Animator.StringToHash("State");
        private static readonly Color HitStunTint = new Color(1f, 0.55f, 0.55f, 1f);
        private const int ContactCapacity = 8;
        private const int ObstacleCapacity = 8;

        [Header("规则")]
        [SerializeField] private Demo2MonsterConfig _config;
        [SerializeField] private Demo2TreeController _treeTarget;
        [SerializeField] private Demo2SimulationController _simulationController;

        [Header("自身组件")]
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Collider2D _bodyCollider;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _visualRoot;

        private readonly ContactPoint2D[] _contactResults = new ContactPoint2D[ContactCapacity];
        private readonly RaycastHit2D[] _obstacleResults = new RaycastHit2D[ObstacleCapacity];

        private Demo2MonsterStateMachine _stateMachine;
        private Color _normalColor;
        private Demo2MonsterState _appliedAnimatorState = (Demo2MonsterState)(-1);
        private bool _isSelfConfigured;
        private bool _isInitialized;
        private bool _isRegisteredAsAttacker;
        private bool _hasPublishedDeath;
        private ContactFilter2D _solidContactFilter;
        private float _landingRetryRemaining;
        private bool _hasGroundSample;
        private bool _wasGrounded;
        private bool _isAutoJumpInProgress;
        private bool _hasLeftGroundSinceJump;

        /// <summary>
        /// 怪物是否已完成配置并仍处于存活状态。
        /// </summary>
        public bool IsAlive => _isInitialized && _stateMachine.IsAlive;

        /// <summary>
        /// 当前剩余生命；无效配置不会伪造生命值。
        /// </summary>
        public int CurrentHealth => _isInitialized ? _stateMachine.CurrentHealth : 0;

        /// <summary>
        /// 当前由纯状态规则确定的玩法状态。
        /// </summary>
        public Demo2MonsterState State => _stateMachine?.State ?? Demo2MonsterState.Dead;

        /// <summary>
        /// 生命首次降至零时发布的事实通知。
        /// </summary>
        public event Action<IMonster> Died;

        /// <summary>
        /// 为运行时生成的怪物一次性注入场景树苗和暂停控制器。
        /// </summary>
        public bool Initialize(
            Demo2TreeController treeTarget,
            Demo2SimulationController simulationController)
        {
            if (!_isSelfConfigured || _isInitialized || treeTarget == null || simulationController == null)
            {
                return false;
            }

            _treeTarget = treeTarget;
            _simulationController = simulationController;
            _isInitialized = true;
            _appliedAnimatorState = (Demo2MonsterState)(-1);
            _stateMachine.Advance(0f, IsWithinBiteDistance());
            ApplyStateEffects();
            return true;
        }

        private void Awake()
        {
            if (!TryValidateSelfConfiguration(out string error))
            {
                DisableForInvalidConfiguration(error);
                return;
            }

            _stateMachine = new Demo2MonsterStateMachine(
                _config.MaxHealth,
                _config.HitStunDurationSeconds,
                _config.DeathDurationSeconds);
            _normalColor = _spriteRenderer.color;
            _solidContactFilter = new ContactFilter2D();
            _solidContactFilter.NoFilter();
            _solidContactFilter.useTriggers = false;
            _isSelfConfigured = true;
            ApplyAnimatorState();
        }

        private void Start()
        {
            if (!_isSelfConfigured || _isInitialized)
            {
                return;
            }

            if (!Initialize(_treeTarget, _simulationController))
            {
                DisableForInvalidConfiguration("必须配置树苗和玩法暂停控制器。");
            }
        }

        private void OnEnable()
        {
            if (!_isInitialized)
            {
                return;
            }

            ResetJumpTracking();
            _appliedAnimatorState = (Demo2MonsterState)(-1);
            _stateMachine.Advance(0f, IsWithinBiteDistance());
            ApplyStateEffects();
        }

        private void OnDisable()
        {
            if (!_isInitialized)
            {
                return;
            }

            UnregisterFromTree();
            SetHorizontalVelocity(0f);
            _spriteRenderer.color = _normalColor;
            ResetJumpTracking();
        }

        private void OnDestroy()
        {
            if (!_isInitialized)
            {
                return;
            }

            UnregisterFromTree();
        }

        private void Update()
        {
            if (!_isInitialized || IsGameplayFrozen)
            {
                return;
            }

            _stateMachine.Advance(Time.deltaTime, IsWithinBiteDistance());
            ApplyStateEffects();

            if (_stateMachine.ShouldDisappear)
            {
                Destroy(gameObject);
            }
        }

        private void FixedUpdate()
        {
            if (!_isInitialized || IsGameplayFrozen || !_rigidbody.simulated)
            {
                return;
            }

            _landingRetryRemaining = Mathf.Max(
                0f,
                _landingRetryRemaining - Time.fixedDeltaTime);
            bool isGrounded = UpdateGroundState();

            if (_stateMachine.State != Demo2MonsterState.Moving || IsWithinBiteDistance())
            {
                SetHorizontalVelocity(0f);
                return;
            }

            float direction = _treeTarget.transform.position.x > transform.position.x ? 1f : -1f;
            SetHorizontalVelocity(direction * _config.MoveSpeed);
            SetFacingDirection(direction);

            if (isGrounded && !_isAutoJumpInProgress && _landingRetryRemaining <= 0f &&
                HasForwardObstacle(direction))
            {
                ApplyJumpVelocity();
            }
        }

        /// <summary>
        /// 尝试扣除生命并立即同步受击或死亡表现。
        /// </summary>
        public MonsterDamageResult TryTakeDamage(int damage)
        {
            if (!_isInitialized)
            {
                return MonsterDamageResult.AlreadyDead;
            }

            MonsterDamageResult result = _stateMachine.TryTakeDamage(damage);
            if (result != MonsterDamageResult.Applied && result != MonsterDamageResult.Killed)
            {
                return result;
            }

            ApplyStateEffects();
            if (result == MonsterDamageResult.Killed && !_hasPublishedDeath)
            {
                _hasPublishedDeath = true;
                Died?.Invoke(this);
            }

            return result;
        }

        private bool IsGameplayFrozen => _simulationController.IsGameplayFrozen;


        private void ApplyStateEffects()
        {
            Demo2MonsterState currentState = _stateMachine.State;
            if (currentState == Demo2MonsterState.Dead)
            {
                UnregisterFromTree();
                SetHorizontalVelocity(0f);
                _bodyCollider.enabled = false;
                _rigidbody.simulated = false;
            }
            else if (currentState == Demo2MonsterState.Biting)
            {
                RegisterWithTree();
                SetHorizontalVelocity(0f);
            }
            else
            {
                UnregisterFromTree();
                if (currentState == Demo2MonsterState.HitStun)
                {
                    SetHorizontalVelocity(0f);
                }
            }

            Color hitStunColor = HitStunTint;
            hitStunColor.a = _normalColor.a;
            _spriteRenderer.color = currentState == Demo2MonsterState.HitStun
                ? hitStunColor
                : _normalColor;

            ApplyAnimatorState();
        }

        private void RegisterWithTree()
        {
            if (_isRegisteredAsAttacker)
            {
                return;
            }

            _isRegisteredAsAttacker = _treeTarget.RegisterAttacker(this);
        }

        private void UnregisterFromTree()
        {
            if (!_isRegisteredAsAttacker)
            {
                return;
            }

            _treeTarget.UnregisterAttacker(this);
            _isRegisteredAsAttacker = false;
        }

        private bool IsWithinBiteDistance()
        {
            float horizontalDistance = Mathf.Abs(_treeTarget.transform.position.x - transform.position.x);
            return horizontalDistance <= _config.BiteDistance;
        }

        private void ResetJumpTracking()
        {
            _landingRetryRemaining = 0f;
            _hasGroundSample = false;
            _wasGrounded = false;
            _isAutoJumpInProgress = false;
            _hasLeftGroundSinceJump = false;
        }

        private bool UpdateGroundState()
        {
            int contactCount = _bodyCollider.GetContacts(_solidContactFilter, _contactResults);
            bool isGrounded = false;
            for (int index = 0; index < contactCount; index++)
            {
                if (_contactResults[index].normal.y > 0.5f)
                {
                    isGrounded = true;
                    break;
                }
            }

            if (_isAutoJumpInProgress)
            {
                if (!isGrounded)
                {
                    _hasLeftGroundSinceJump = true;
                }
                else if (_hasGroundSample && !_wasGrounded && _hasLeftGroundSinceJump)
                {
                    _isAutoJumpInProgress = false;
                    _hasLeftGroundSinceJump = false;
                    _landingRetryRemaining = _config.LandingRetrySeconds;
                }
            }

            _hasGroundSample = true;
            _wasGrounded = isGrounded;
            return isGrounded;
        }

        private bool HasForwardObstacle(float direction)
        {
            int hitCount = _bodyCollider.Cast(
                new Vector2(direction, 0f),
                _solidContactFilter,
                _obstacleResults,
                _config.ObstacleDetectionDistance);

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = _obstacleResults[index];
                if (hit.collider == null || hit.normal.x * direction > -0.5f)
                {
                    continue;
                }

                Demo2TreeController hitTree = hit.collider.GetComponentInParent<Demo2TreeController>();
                if (hitTree != _treeTarget)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyJumpVelocity()
        {
            float downwardAcceleration = -(Physics2D.gravity.y * _rigidbody.gravityScale);
            float jumpSpeed = Mathf.Sqrt(2f * downwardAcceleration * _config.JumpHeight);
            Vector2 velocity = _rigidbody.velocity;
            velocity.y = jumpSpeed;
            _rigidbody.velocity = velocity;
            _isAutoJumpInProgress = true;
            _hasLeftGroundSinceJump = false;
        }

        private void SetHorizontalVelocity(float velocityX)
        {
            if (_rigidbody == null || !_rigidbody.simulated)
            {
                return;
            }

            Vector2 velocity = _rigidbody.velocity;
            velocity.x = velocityX;
            _rigidbody.velocity = velocity;
        }

        private void SetFacingDirection(float direction)
        {
            if (Mathf.Approximately(direction, 0f))
            {
                return;
            }

            Vector3 scale = _visualRoot.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (direction > 0f ? 1f : -1f);
            _visualRoot.transform.localScale = scale;
        }

        private void ApplyAnimatorState()
        {
            if (_appliedAnimatorState == _stateMachine.State)
            {
                return;
            }

            _appliedAnimatorState = _stateMachine.State;
            _animator.SetInteger(StateParameter, (int)_appliedAnimatorState);
        }

        private bool TryValidateSelfConfiguration(out string error)
        {
            if (_config == null)
            {
                error = "必须配置 Demo2MonsterConfig。";
                return false;
            }

            if (!_config.TryValidate(out error))
            {
                return false;
            }


            if (_rigidbody == null || _bodyCollider == null || _spriteRenderer == null ||
                _animator == null || _visualRoot == null)
            {
                error = "必须配置 Rigidbody2D、身体 Collider2D、SpriteRenderer、Animator 和视觉根节点。";
                return false;
            }

            if (_visualRoot == gameObject)
            {
                error = "视觉根节点必须是子对象，不能停用承载控制器的根对象。";
                return false;
            }

            float downwardAcceleration = -(Physics2D.gravity.y * _rigidbody.gravityScale);
            if (!IsPositiveFinite(downwardAcceleration))
            {
                error = "Rigidbody2D必须具有有限的向下重力。";
                return false;
            }

            if (_animator.runtimeAnimatorController == null)
            {
                error = "Animator 必须配置 RuntimeAnimatorController。";
                return false;
            }

            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == StateParameter && parameter.type == AnimatorControllerParameterType.Int)
                {
                    error = string.Empty;
                    return true;
                }
            }

            error = "Animator 缺少 Int 类型的 State 参数。";
            return false;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void DisableForInvalidConfiguration(string reason)
        {
            Debug.LogError("Demo2MonsterController 配置无效：" + reason, this);
            enabled = false;
        }
    }
}
