using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 表示 Demo2 侧视玩法中可以被玩家水平推动的动态障碍物。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Demo2PushableObstacle : MonoBehaviour
    {
        [Header("推动参数")]
        [SerializeField] private float _pushForce = 40f;
        [SerializeField] private float _maximumHorizontalSpeed = 2.5f;

        private Rigidbody2D _rigidbody;
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

            if (_pushForce <= 0f || float.IsNaN(_pushForce) || float.IsInfinity(_pushForce) ||
                _maximumHorizontalSpeed <= 0f || float.IsNaN(_maximumHorizontalSpeed) ||
                float.IsInfinity(_maximumHorizontalSpeed))
            {
                Debug.LogError("Demo2PushableObstacle 的推动参数必须是有限正数。", this);
                enabled = false;
                return;
            }

            _isConfigured = true;
        }

        /// <summary>
        /// 沿指定水平朝向持续施加一次推动力。
        /// </summary>
        public bool TryPush(float horizontalDirection)
        {
            if (!_isConfigured || Mathf.Approximately(horizontalDirection, 0f))
            {
                return false;
            }

            float direction = horizontalDirection > 0f ? 1f : -1f;
            float speedInDirection = _rigidbody.velocity.x * direction;
            float availableSpeed = _maximumHorizontalSpeed - speedInDirection;
            if (availableSpeed <= 0f)
            {
                return false;
            }

            float maximumForce = availableSpeed * _rigidbody.mass / Time.fixedDeltaTime;
            float appliedForce = Mathf.Min(_pushForce, maximumForce);
            _rigidbody.AddForce(Vector2.right * direction * appliedForce, ForceMode2D.Force);
            return true;
        }
    }
}
