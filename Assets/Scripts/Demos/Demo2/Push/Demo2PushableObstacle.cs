using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 表示 Demo2 侧视玩法中只允许玩家授权水平移动、同时保留重力的动态障碍物。
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Demo2PushableObstacle : MonoBehaviour
    {
        [Header("推动参数")]
        [SerializeField] private float _pushSpeed = 1f;

        private Rigidbody2D _rigidbody;
        private RigidbodyConstraints2D _idleConstraints;
        private RigidbodyConstraints2D _pushConstraints;
        private float _requestedDirection;
        private bool _hasPushRequest;
        private bool _isConfigured;

        /// <summary>
        /// 障碍物使用的二维刚体。
        /// </summary>
        public Rigidbody2D Rigidbody => _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            if (_rigidbody.bodyType != RigidbodyType2D.Dynamic)
            {
                Debug.LogError("Demo2PushableObstacle 必须使用 Dynamic Rigidbody2D。", this);
                enabled = false;
                return;
            }

            if (!IsPositiveFinite(_pushSpeed))
            {
                Debug.LogError("Demo2PushableObstacle 的推动速度必须是有限正数。", this);
                enabled = false;
                return;
            }

            _pushConstraints = _rigidbody.constraints & ~RigidbodyConstraints2D.FreezePositionX;
            _idleConstraints = _pushConstraints | RigidbodyConstraints2D.FreezePositionX;
            _isConfigured = true;
            ApplyIdleLock();
        }

        private void OnEnable()
        {
            if (_isConfigured)
            {
                ApplyIdleLock();
            }
        }

        private void OnDisable()
        {
            if (_isConfigured)
            {
                ApplyIdleLock();
            }
        }

        private void FixedUpdate()
        {
            if (!_isConfigured)
            {
                return;
            }

            Vector2 velocity = _rigidbody.velocity;
            if (_hasPushRequest)
            {
                _rigidbody.constraints = _pushConstraints;
                velocity.x = _requestedDirection * _pushSpeed;
            }
            else
            {
                _rigidbody.constraints = _idleConstraints;
                velocity.x = 0f;
            }

            _rigidbody.velocity = velocity;
            _hasPushRequest = false;
            _requestedDirection = 0f;
        }

        /// <summary>
        /// 登记当前物理步的玩家水平推动方向；未登记的物理步保持X位置锁定。
        /// </summary>
        public bool TryPush(float horizontalDirection)
        {
            if (!_isConfigured || Mathf.Approximately(horizontalDirection, 0f))
            {
                return false;
            }

            _requestedDirection = horizontalDirection > 0f ? 1f : -1f;
            _hasPushRequest = true;
            return true;
        }

        private void ApplyIdleLock()
        {
            _hasPushRequest = false;
            _requestedDirection = 0f;
            _rigidbody.constraints = _idleConstraints;
            Vector2 velocity = _rigidbody.velocity;
            velocity.x = 0f;
            _rigidbody.velocity = velocity;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
