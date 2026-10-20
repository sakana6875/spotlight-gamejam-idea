using Spotlight.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 处理第一幕TopView中的E拾取、携带、前方预览和网格放置。
    /// </summary>
    [RequireComponent(typeof(PlayerMoveController))]
    public sealed class Demo2PipeCarryController : MonoBehaviour
    {
        private const int PickupHitCapacity = 16;
        private const int PlacementHitCapacity = 16;

        [Header("运行状态")]
        [SerializeField] private Demo2SimulationController _simulationController;
        [SerializeField] private Transform _carryAnchor;
        [SerializeField] private Demo2PipePlacementPreview _placementPreview;

        [Header("拾取")]
        [SerializeField] private LayerMask _carryablePipeLayers;
        [SerializeField] private float _pickupRadius = 1f;

        [Header("放置")]
        [SerializeField] private LayerMask _placementBlockingLayers;
        [SerializeField] private float _placementForwardDistance = 1f;
        [SerializeField] private Vector2 _placementCellCheckSize = new Vector2(0.9f, 0.9f);

        private readonly Collider2D[] _pickupHits = new Collider2D[PickupHitCapacity];
        private readonly Collider2D[] _placementHits = new Collider2D[PlacementHitCapacity];

        private PlayerMoveController _playerMoveController;
        private GameInputActions _input;
        private InputAction _interactAction;
        private Demo2CarryablePipe _carriedPipe;
        private Vector3Int _previewOrigin;
        private bool _isPreviewValid;
        private bool _isConfigured;

        /// <summary>
        /// 玩家当前是否正在搬运一根水管。
        /// </summary>
        public bool IsCarrying => _carriedPipe != null;

        private void Awake()
        {
            _playerMoveController = GetComponent<PlayerMoveController>();
            _input = new GameInputActions();
            _interactAction = _input.Demo2.Get().FindAction("Interact", false);

            if (_playerMoveController == null || _simulationController == null || _carryAnchor == null ||
                _placementPreview == null)
            {
                Debug.LogError("Demo2PipeCarryController缺少玩家、运行状态、CarryAnchor或Preview引用。", this);
                enabled = false;
                return;
            }

            if (_interactAction == null)
            {
                Debug.LogError("Demo2 Input Actions缺少名为Interact的动作；请绑定E并重新生成输入代码。", this);
                enabled = false;
                return;
            }

            if (!IsPositiveFinite(_pickupRadius) || !IsPositiveFinite(_placementForwardDistance) ||
                !IsPositiveFinite(_placementCellCheckSize.x) || !IsPositiveFinite(_placementCellCheckSize.y))
            {
                Debug.LogError("拾取半径、前方距离和放置检查尺寸必须是有限正数。", this);
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
            _placementPreview?.Hide();
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

            if (IsCarrying)
            {
                _carriedPipe.Segment.SetCarryPosition(_carryAnchor.position);
                UpdatePlacementPreview();
            }

            if (_simulationController.IsGameplayFrozen || !_interactAction.WasPressedThisFrame())
            {
                return;
            }

            if (IsCarrying)
            {
                TryPlaceCarriedPipe();
            }
            else
            {
                TryPickupNearestPipe();
            }
        }

        /// <summary>
        /// 尝试拾取玩家中心1米范围内最近的合法水管。
        /// </summary>
        public bool TryPickupNearestPipe()
        {
            if (!_isConfigured || IsCarrying || !CanStartCarry())
            {
                return false;
            }

            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useLayerMask = true;
            contactFilter.SetLayerMask(_carryablePipeLayers);
            contactFilter.useTriggers = false;

            int hitCount = Physics2D.OverlapCircle(
                transform.position,
                _pickupRadius,
                contactFilter,
                _pickupHits);

            Demo2CarryablePipe nearest = null;
            float nearestDistanceSquared = float.PositiveInfinity;
            for (int index = 0; index < hitCount; index++)
            {
                Collider2D hit = _pickupHits[index];
                Demo2CarryablePipe candidate = hit != null
                    ? hit.GetComponentInParent<Demo2CarryablePipe>()
                    : null;
                if (candidate == null || candidate.Segment == null || candidate.Segment.IsCarried)
                {
                    continue;
                }

                Vector2 closestPoint = hit.ClosestPoint(transform.position);
                float distanceSquared = (closestPoint - (Vector2)transform.position).sqrMagnitude;
                if (distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistanceSquared = distanceSquared;
            }

            if (nearest == null || !nearest.Segment.TryBeginCarry(_carryAnchor))
            {
                return false;
            }

            _carriedPipe = nearest;
            UpdatePlacementPreview();
            return true;
        }

        /// <summary>
        /// 在当前绿色预览网格尝试放置正在搬运的水管。
        /// </summary>
        public bool TryPlaceCarriedPipe()
        {
            if (!_isConfigured || !IsCarrying)
            {
                return false;
            }

            UpdatePlacementPreview();
            if (!_isPreviewValid || !_carriedPipe.Segment.TryPlace(_previewOrigin))
            {
                return false;
            }

            _carriedPipe = null;
            _placementPreview.Hide();
            return true;
        }

        private bool CanStartCarry()
        {
            Demo2PlayerState state = _playerMoveController.State;
            return _playerMoveController.ViewMode == PlayerMoveController.Demo2ViewMode.Top &&
                !_simulationController.IsGameplayFrozen &&
                (state == Demo2PlayerState.Idle || state == Demo2PlayerState.Moving);
        }

        private void UpdatePlacementPreview()
        {
            Demo2PipeSegment segment = _carriedPipe.Segment;
            Demo2PipeWaterController waterController = segment.WaterController;
            Vector2 facingDirection = _playerMoveController.FacingDirection;
            Vector2 targetPosition = (Vector2)transform.position + facingDirection * _placementForwardDistance;
            _previewOrigin = waterController.WorldToCell(targetPosition);
            _isPreviewValid = IsPlacementPhysicallyValid(segment, _previewOrigin);
            _placementPreview.Show(
                segment.Shape,
                waterController,
                _previewOrigin,
                segment.QuarterTurns,
                _isPreviewValid);
        }

        private bool IsPlacementPhysicallyValid(Demo2PipeSegment segment, Vector3Int origin)
        {
            Demo2PipeShape shape = segment.Shape;
            Demo2PipeWaterController waterController = segment.WaterController;
            if (!waterController.CanRegisterPipe(segment, shape, origin, segment.QuarterTurns))
            {
                return false;
            }

            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useLayerMask = true;
            contactFilter.SetLayerMask(_placementBlockingLayers);
            contactFilter.useTriggers = false;

            for (int index = 0; index < shape.CellCount; index++)
            {
                Demo2PipeCellDefinition definition = shape.GetRotatedCell(index, segment.QuarterTurns);
                Vector3Int cell = origin + new Vector3Int(definition.Offset.x, definition.Offset.y, 0);
                Vector2 cellCenter = waterController.GetCellCenterWorld(cell);
                int hitCount = Physics2D.OverlapBox(
                    cellCenter,
                    _placementCellCheckSize,
                    0f,
                    contactFilter,
                    _placementHits);

                for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
                {
                    Collider2D hit = _placementHits[hitIndex];
                    if (hit == null || hit.transform.IsChildOf(transform) ||
                        hit.transform.IsChildOf(segment.transform))
                    {
                        continue;
                    }

                    return false;
                }
            }

            return true;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _pickupRadius);
        }
    }
}
