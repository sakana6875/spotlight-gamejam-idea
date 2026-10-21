using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 将一个固定朝向的水管Shape登记到共享管网，并管理搬运时的实体碰撞。
    /// </summary>
    public sealed class Demo2PipeSegment : MonoBehaviour
    {
        [SerializeField] private Demo2PipeShape _shape;
        [SerializeField] private Demo2PipeWaterController _waterController;
        [SerializeField] private Collider2D[] _solidColliders;

        private Transform _placedParent;
        private Vector3Int _gridOrigin;
        private int _quarterTurns;
        private bool _isRegistered;
        private bool _isCarried;
        private bool _isConfigured;
        private bool _hasConfigured;
        private bool _hasAttemptedInitialRegistration;

        public Demo2PipeShape Shape => _shape;

        public Demo2PipeWaterController WaterController => _waterController;

        public Vector3Int GridOrigin => _gridOrigin;

        public int QuarterTurns => _quarterTurns;

        public bool IsRegistered => _isRegistered;

        public bool IsCarried => _isCarried;

        private void Awake()
        {
            Configure();
        }

        private void Start()
        {
            EnsureInitializedAndRegistered();
        }

        private void OnDestroy()
        {
            if (_isRegistered && _waterController != null && _waterController.IsConfigured)
            {
                _waterController.UnregisterPipe(this, _shape, _gridOrigin, _quarterTurns);
                _isRegistered = false;
            }
        }

        /// <summary>
        /// 幂等完成配置和初始登记；即使所属TopView当前inactive也可由场景控制器调用。
        /// </summary>
        public bool EnsureInitializedAndRegistered()
        {
            if (!Configure())
            {
                return false;
            }

            if (_hasAttemptedInitialRegistration)
            {
                return true;
            }

            _hasAttemptedInitialRegistration = true;
            _gridOrigin = _waterController.WorldToCell(transform.position);
            SnapTransformToGrid(_gridOrigin);
            if (_waterController.TryRegisterPipe(this, _shape, _gridOrigin, _quarterTurns))
            {
                _isRegistered = true;
                return true;
            }

            if (TryGetComponent(out Demo2CarryablePipe _))
            {
                return true;
            }

            Debug.LogError("固定水管初始位置必须完整占据方向路线格。", this);
            enabled = false;
            return false;
        }

        /// <summary>
        /// 从管网移除并移动到玩家携带点；不继承玩家翻面缩放或旋转。
        /// </summary>
        public bool TryBeginCarry(Transform carryAnchor)
        {
            if (!EnsureInitializedAndRegistered() || _isCarried || carryAnchor == null)
            {
                return false;
            }

            if (_isRegistered)
            {
                _waterController.UnregisterPipe(this, _shape, _gridOrigin, _quarterTurns);
                _isRegistered = false;
            }

            SetCollidersEnabled(false);
            transform.position = carryAnchor.position;
            _isCarried = true;
            return true;
        }

        /// <summary>
        /// 更新搬运中的世界位置，不继承玩家翻面缩放或旋转。
        /// </summary>
        public void SetCarryPosition(Vector3 worldPosition)
        {
            if (_isCarried)
            {
                transform.position = worldPosition;
            }
        }

        /// <summary>
        /// 在指定网格原点恢复管网登记和实体碰撞。
        /// </summary>
        public bool TryPlace(Vector3Int gridOrigin)
        {
            if (!Configure() || !_isCarried)
            {
                return false;
            }

            Vector3Int previousOrigin = _gridOrigin;
            _gridOrigin = gridOrigin;
            if (!_waterController.TryRegisterPipe(this, _shape, _gridOrigin, _quarterTurns))
            {
                _gridOrigin = previousOrigin;
                return false;
            }

            transform.SetParent(_placedParent, true);
            SnapTransformToGrid(_gridOrigin);
            _isRegistered = true;
            _isCarried = false;
            SetCollidersEnabled(true);
            return true;
        }

        private bool Configure()
        {
            if (_hasConfigured)
            {
                return _isConfigured;
            }

            _hasConfigured = true;
            _placedParent = transform.parent;
            if (_shape == null || _waterController == null)
            {
                Debug.LogError("Demo2PipeSegment缺少Shape或PipeWaterController引用。", this);
                enabled = false;
                return false;
            }

            if (!_shape.TryValidate(out string shapeError))
            {
                Debug.LogError("Demo2PipeSegment配置无效：" + shapeError, this);
                enabled = false;
                return false;
            }

            if (_solidColliders == null || _solidColliders.Length == 0)
            {
                Debug.LogError("水管必须配置至少一个实体Collider2D。", this);
                enabled = false;
                return false;
            }

            for (int index = 0; index < _solidColliders.Length; index++)
            {
                if (_solidColliders[index] == null)
                {
                    Debug.LogError("水管实体Collider2D数组包含空引用。", this);
                    enabled = false;
                    return false;
                }
            }

            if (!TryGetQuarterTurns(transform.eulerAngles.z, out _quarterTurns))
            {
                Debug.LogError("水管Z旋转必须是90度的整数倍。", this);
                enabled = false;
                return false;
            }

            _isConfigured = true;
            return true;
        }

        private void SnapTransformToGrid(Vector3Int gridOrigin)
        {
            Vector3 position = _waterController.GetCellCenterWorld(gridOrigin);
            position.z = transform.position.z;
            transform.position = position;
        }

        private void SetCollidersEnabled(bool isEnabled)
        {
            for (int index = 0; index < _solidColliders.Length; index++)
            {
                _solidColliders[index].enabled = isEnabled;
            }
        }

        private static bool TryGetQuarterTurns(float zDegrees, out int quarterTurns)
        {
            float normalized = Mathf.Repeat(zDegrees, 360f);
            int roundedTurns = Mathf.RoundToInt(normalized / 90f);
            float snapped = Mathf.Repeat(roundedTurns * 90f, 360f);
            if (Mathf.Abs(Mathf.DeltaAngle(normalized, snapped)) > 0.01f)
            {
                quarterTurns = 0;
                return false;
            }

            // Unity正Z旋转为逆时针，端口工具的正圈数定义为顺时针。
            quarterTurns = Demo2WaterPortUtility.NormalizeQuarterTurns(-roundedTurns);
            return true;
        }
    }
}
