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

        private Demo2MonsterStateMachine _stateMachine;
        private Color _normalColor;
        private Demo2MonsterState _appliedAnimatorState = (Demo2MonsterState)(-1);
        private bool _isConfigured;
        private bool _isRegisteredAsAttacker;
        private bool _hasPublishedDeath;

        /// <summary>
        /// 怪物是否已完成配置并仍处于存活状态。
        /// </summary>
        public bool IsAlive => _isConfigured && _stateMachine.IsAlive;

        /// <summary>
        /// 当前剩余生命；无效配置不会伪造生命值。
        /// </summary>
        public int CurrentHealth => _isConfigured ? _stateMachine.CurrentHealth : 0;

        /// <summary>
        /// 当前由纯状态规则确定的玩法状态。
        /// </summary>
        public Demo2MonsterState State => _stateMachine?.State ?? Demo2MonsterState.Dead;

        /// <summary>
        /// 生命首次降至零时发布的事实通知。
        /// </summary>
        public event Action<IMonster> Died;

        private void Awake()
        {
            if (!TryValidateConfiguration(out string error))
            {
                DisableForInvalidConfiguration(error);
                return;
            }

            _stateMachine = new Demo2MonsterStateMachine(
                _config.MaxHealth,
                _config.HitStunDurationSeconds,
                _config.DeathDurationSeconds);
            _normalColor = _spriteRenderer.color;
            _isConfigured = true;
            ApplyAnimatorState();
        }

        private void OnEnable()
        {
            if (!_isConfigured)
            {
                return;
            }

            _appliedAnimatorState = (Demo2MonsterState)(-1);
            _stateMachine.Advance(0f, IsWithinBiteDistance());
            ApplyStateEffects();
        }

        private void OnDisable()
        {
            if (!_isConfigured)
            {
                return;
            }

            UnregisterFromTree();
            SetHorizontalVelocity(0f);
            _spriteRenderer.color = _normalColor;
        }

        private void OnDestroy()
        {
            if (!_isConfigured)
            {
                return;
            }

            UnregisterFromTree();
        }

        private void Update()
        {
            if (!_isConfigured || IsGameplayFrozen)
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
            if (!_isConfigured || IsGameplayFrozen || !_rigidbody.simulated)
            {
                return;
            }

            if (_stateMachine.State != Demo2MonsterState.Moving || IsWithinBiteDistance())
            {
                SetHorizontalVelocity(0f);
                return;
            }

            float direction = _treeTarget.transform.position.x > transform.position.x ? 1f : -1f;
            SetHorizontalVelocity(direction * _config.MoveSpeed);
            SetFacingDirection(direction);
        }

        /// <summary>
        /// 尝试扣除生命并立即同步受击或死亡表现。
        /// </summary>
        public MonsterDamageResult TryTakeDamage(int damage)
        {
            if (!_isConfigured)
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

            _spriteRenderer.color = currentState == Demo2MonsterState.HitStun
                ? Color.Lerp(_normalColor, Color.white, 0.75f)
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

        private bool TryValidateConfiguration(out string error)
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

            if (_treeTarget == null || _simulationController == null)
            {
                error = "必须配置树苗和玩法暂停控制器。";
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

        private void DisableForInvalidConfiguration(string reason)
        {
            Debug.LogError("Demo2MonsterController 配置无效：" + reason, this);
            enabled = false;
        }
    }
}
