using Spotlight.Input;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 读取 Demo2 的 Push 动作，并在侧视模式下将玩家接触到的障碍物交给物理推动。
    /// </summary>
    [RequireComponent(typeof(PlayerMoveController))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Demo2PushController : MonoBehaviour
    {
        private const int CastHitCapacity = 8;

        [Header("运行状态")]
        [SerializeField] private Demo2SimulationController _simulationController;

        [Header("检测参数")]
        [SerializeField] private LayerMask _pushableLayer = ~0;
        [SerializeField] private float _castDistance = 0.05f;

        private readonly RaycastHit2D[] _castHits = new RaycastHit2D[CastHitCapacity];

        private PlayerMoveController _playerMoveController;
        private Collider2D _playerCollider;
        private GameInputActions _input;
        private bool _isConfigured;

        private void Awake()
        {
            _playerMoveController = GetComponent<PlayerMoveController>();
            _playerCollider = GetComponent<Collider2D>();
            _input = new GameInputActions();

            if (_playerMoveController == null || _playerCollider == null || _simulationController == null)
            {
                Debug.LogError(
                    "Demo2PushController 缺少 PlayerMoveController、Collider2D 或 Demo2SimulationController。",
                    this);
                enabled = false;
                return;
            }

            if (_castDistance < 0f || float.IsNaN(_castDistance) || float.IsInfinity(_castDistance))
            {
                Debug.LogError("Demo2PushController 的检测距离必须是有限非负数。", this);
                enabled = false;
                return;
            }

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

        private void FixedUpdate()
        {
            if (_input.Demo2.Push.IsPressed())
            {
                TryPush();
            }
        }

        /// <summary>
        /// 尝试沿玩家当前水平朝向推动前方接触的障碍物。
        /// </summary>
        public bool TryPush()
        {
            if (!_isConfigured || _simulationController.IsGameplayFrozen ||
                _playerMoveController.ViewMode != PlayerMoveController.Demo2ViewMode.Side)
            {
                return false;
            }

            float horizontalDirection = _playerMoveController.IsFacingRight ? 1f : -1f;
            Vector2 castDirection = Vector2.right * horizontalDirection;
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useLayerMask = true;
            contactFilter.SetLayerMask(_pushableLayer);

            int hitCount = _playerCollider.Cast(castDirection, contactFilter, _castHits, _castDistance);
            for (int index = 0; index < hitCount; index++)
            {
                Demo2PushableObstacle obstacle = _castHits[index].collider
                    .GetComponentInParent<Demo2PushableObstacle>();
                if (obstacle != null && obstacle.TryPush(horizontalDirection))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
