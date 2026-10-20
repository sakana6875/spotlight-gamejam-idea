using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 从单一Marker路线、唯一水源和已放置管段驱动第一幕俯视管内水流。
    /// </summary>
    public sealed class Demo2PipeWaterController : Demo2WaterPresenceSource
    {
        [Header("管网Tilemap")]
        [SerializeField] private Tilemap _routeTilemap;
        [SerializeField] private TileBase _routeMarkerTile;
        [SerializeField] private Vector3Int _sourceCell;
        [SerializeField] private Tilemap _waterTilemap;
        [SerializeField] private TileBase _waterTile;

        [Header("阻挡与时序")]
        [SerializeField] private Collider2D[] _blockingColliders;
        [SerializeField] private float _flowStepSeconds = 0.15f;

        private Demo2PipeWaterModel _model;
        private Demo2PipeSegment[] _pipeOwners;
        private TileBase[] _renderTiles;
        private bool[] _sampledBlockedCells;
        private BoundsInt _networkBounds;
        private float _stepElapsed;
        private bool _isConfigured;

        public bool IsConfigured => _isConfigured;

        /// <summary>
        /// 管段完成登记或移除时发布的事实。
        /// </summary>
        public event Action<Demo2PipePlacementChangedEvent> PipePlacementChanged;

        /// <summary>
        /// Water Tilemap完成初始化或水状态变化后发布的事实。
        /// </summary>
        public event Action WaterStateChanged;

        private void Awake()
        {
            if (_routeTilemap == null || _routeMarkerTile == null || _waterTilemap == null || _waterTile == null)
            {
                DisableForInvalidConfiguration("必须指定Route Tilemap、Route Marker Tile、Water Tilemap和Water Tile。");
                return;
            }

            if (!IsPositiveFinite(_flowStepSeconds))
            {
                DisableForInvalidConfiguration("水流步长必须是有限正数。");
                return;
            }

            if (!TryCalculateNetworkBounds(out _networkBounds))
            {
                DisableForInvalidConfiguration("Route Tilemap中没有有效格。");
                return;
            }

            _model = new Demo2PipeWaterModel(_networkBounds.size.x, _networkBounds.size.y);
            int cellCount = _model.Width * _model.Height;
            _pipeOwners = new Demo2PipeSegment[cellCount];
            _renderTiles = new TileBase[cellCount];
            _sampledBlockedCells = new bool[cellCount];

            if (!CaptureAuthoredNetwork(out string error))
            {
                DisableForInvalidConfiguration(error);
                return;
            }

            SampleBlockers();
            _model.ResetToSource();
            RenderWater();
            _isConfigured = true;
        }

        private void FixedUpdate()
        {
            if (!_isConfigured)
            {
                return;
            }

            bool hasChanged = SampleBlockers();
            _stepElapsed += Time.fixedDeltaTime;
            if (_stepElapsed >= _flowStepSeconds)
            {
                _stepElapsed -= _flowStepSeconds;
                hasChanged |= _model.Step();
            }

            if (hasChanged)
            {
                RenderWater();
            }
        }

        /// <summary>
        /// 查询世界坐标所在管网格当前是否有水。
        /// </summary>
        public override bool IsWaterAtWorldPosition(Vector2 worldPosition)
        {
            if (!_isConfigured)
            {
                return false;
            }

            Vector3Int cell = _routeTilemap.WorldToCell(worldPosition);
            return TryToModelCoordinates(cell, out int column, out int row) && _model.IsWater(column, row);
        }

        /// <summary>
        /// 查询指定Route网格当前是否有水。
        /// </summary>
        public bool IsWaterAtCell(Vector3Int cell)
        {
            return _isConfigured && TryToModelCoordinates(cell, out int column, out int row) &&
                _model.IsWater(column, row);
        }

        /// <summary>
        /// 查询指定格是否由目标管段登记。
        /// </summary>
        public bool IsPipeRegisteredAt(Vector3Int cell, Demo2PipeSegment segment)
        {
            return _isConfigured && segment != null &&
                TryToModelCoordinates(cell, out int column, out int row) &&
                _pipeOwners[GetIndex(column, row)] == segment;
        }

        /// <summary>
        /// 将世界坐标吸附为Route Tilemap网格坐标。
        /// </summary>
        public Vector3Int WorldToCell(Vector2 worldPosition)
        {
            return _routeTilemap.WorldToCell(worldPosition);
        }

        /// <summary>
        /// 返回指定管网格中心的世界坐标。
        /// </summary>
        public Vector3 GetCellCenterWorld(Vector3Int cell)
        {
            return _routeTilemap.GetCellCenterWorld(cell);
        }

        /// <summary>
        /// 判断指定格是否位于管网范围并绘制了Marker路线。
        /// </summary>
        public bool IsPlacementCellAllowed(Vector3Int cell)
        {
            return _isConfigured && IsRouteMarker(cell);
        }

        /// <summary>
        /// 判断Shape全部格是否可由指定管段占用；只检查网格和其他管段占用。
        /// </summary>
        public bool CanRegisterPipe(
            Demo2PipeSegment segment,
            Demo2PipeShape shape,
            Vector3Int origin,
            int quarterTurns)
        {
            if (!_isConfigured || segment == null || shape == null || !shape.TryValidate(out _))
            {
                return false;
            }

            for (int index = 0; index < shape.CellCount; index++)
            {
                Demo2PipeCellDefinition definition = shape.GetRotatedCell(index, quarterTurns);
                Vector3Int cell = origin + new Vector3Int(definition.Offset.x, definition.Offset.y, 0);
                if (!TryToModelCoordinates(cell, out int column, out int row) || !IsRouteMarker(cell))
                {
                    return false;
                }

                Demo2PipeSegment owner = _pipeOwners[GetIndex(column, row)];
                if (owner != null && owner != segment)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 原子登记管段的全部占格和端口。
        /// </summary>
        public bool TryRegisterPipe(
            Demo2PipeSegment segment,
            Demo2PipeShape shape,
            Vector3Int origin,
            int quarterTurns)
        {
            if (!CanRegisterPipe(segment, shape, origin, quarterTurns))
            {
                return false;
            }

            for (int index = 0; index < shape.CellCount; index++)
            {
                Demo2PipeCellDefinition definition = shape.GetRotatedCell(index, quarterTurns);
                Vector3Int cell = origin + new Vector3Int(definition.Offset.x, definition.Offset.y, 0);
                TryToModelCoordinates(cell, out int column, out int row);
                _pipeOwners[GetIndex(column, row)] = segment;
                _model.SetPipe(column, row, true, definition.Ports);
            }

            ResetFlowAfterTopologyChange();
            PipePlacementChanged?.Invoke(new Demo2PipePlacementChangedEvent(
                segment,
                shape,
                origin,
                Demo2WaterPortUtility.NormalizeQuarterTurns(quarterTurns),
                true));
            return true;
        }

        /// <summary>
        /// 移除当前由指定管段持有的全部格。
        /// </summary>
        public void UnregisterPipe(
            Demo2PipeSegment segment,
            Demo2PipeShape shape,
            Vector3Int origin,
            int quarterTurns)
        {
            if (!_isConfigured || segment == null || shape == null)
            {
                return;
            }

            bool hasChanged = false;
            for (int index = 0; index < shape.CellCount; index++)
            {
                Demo2PipeCellDefinition definition = shape.GetRotatedCell(index, quarterTurns);
                Vector3Int cell = origin + new Vector3Int(definition.Offset.x, definition.Offset.y, 0);
                if (!TryToModelCoordinates(cell, out int column, out int row))
                {
                    continue;
                }

                int modelIndex = GetIndex(column, row);
                if (_pipeOwners[modelIndex] != segment)
                {
                    continue;
                }

                _pipeOwners[modelIndex] = null;
                _model.SetPipe(column, row, false, Demo2WaterPort.None);
                hasChanged = true;
            }

            if (!hasChanged)
            {
                return;
            }

            ResetFlowAfterTopologyChange();
            PipePlacementChanged?.Invoke(new Demo2PipePlacementChangedEvent(
                segment,
                shape,
                origin,
                Demo2WaterPortUtility.NormalizeQuarterTurns(quarterTurns),
                false));
        }

        private bool CaptureAuthoredNetwork(out string error)
        {
            foreach (Vector3Int cell in _networkBounds.allPositionsWithin)
            {
                TileBase routeTile = _routeTilemap.GetTile(cell);
                if (routeTile != null && routeTile != _routeMarkerTile)
                {
                    error = $"Route Tilemap的{cell}不是配置的Route Marker Tile。";
                    return false;
                }

                if (routeTile == _routeMarkerTile)
                {
                    ToModelCoordinates(cell, out int column, out int row);
                    _model.SetRoute(column, row, GetAuthoredRoutePorts(cell));
                }
            }

            if (!IsRouteMarker(_sourceCell))
            {
                error = $"水源格{_sourceCell}没有配置的Route Marker Tile。";
                return false;
            }

            ToModelCoordinates(_sourceCell, out int sourceColumn, out int sourceRow);
            _model.SetSource(sourceColumn, sourceRow);
            error = string.Empty;
            return true;
        }

        private bool TryCalculateNetworkBounds(out BoundsInt bounds)
        {
            bool hasCell = false;
            Vector3Int minimum = new Vector3Int(int.MaxValue, int.MaxValue, 0);
            Vector3Int maximum = new Vector3Int(int.MinValue, int.MinValue, 0);
            AccumulateOccupiedBounds(_routeTilemap, ref hasCell, ref minimum, ref maximum);
            bounds = hasCell
                ? new BoundsInt(minimum, maximum - minimum + Vector3Int.one)
                : new BoundsInt();
            return hasCell;
        }

        private static void AccumulateOccupiedBounds(
            Tilemap tilemap,
            ref bool hasCell,
            ref Vector3Int minimum,
            ref Vector3Int maximum)
        {
            foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(cell))
                {
                    continue;
                }

                minimum = Vector3Int.Min(minimum, cell);
                maximum = Vector3Int.Max(maximum, cell);
                hasCell = true;
            }
        }

        private bool IsRouteMarker(Vector3Int cell)
        {
            return _routeTilemap.GetTile(cell) == _routeMarkerTile;
        }

        private Demo2WaterPort GetAuthoredRoutePorts(Vector3Int cell)
        {
            Demo2WaterPort ports = Demo2WaterPort.None;
            if (IsRouteMarker(cell + Vector3Int.up))
            {
                ports |= Demo2WaterPort.Up;
            }

            if (IsRouteMarker(cell + Vector3Int.right))
            {
                ports |= Demo2WaterPort.Right;
            }

            if (IsRouteMarker(cell + Vector3Int.down))
            {
                ports |= Demo2WaterPort.Down;
            }

            if (IsRouteMarker(cell + Vector3Int.left))
            {
                ports |= Demo2WaterPort.Left;
            }

            return ports;
        }

        private bool SampleBlockers()
        {
            bool hasChanged = false;
            foreach (Vector3Int cell in _networkBounds.allPositionsWithin)
            {
                ToModelCoordinates(cell, out int column, out int row);
                int index = GetIndex(column, row);
                bool isBlocked = IsBlockedAt(_routeTilemap.GetCellCenterWorld(cell));
                if (_sampledBlockedCells[index] == isBlocked)
                {
                    continue;
                }

                _sampledBlockedCells[index] = isBlocked;
                _model.SetBlocked(column, row, isBlocked);
                hasChanged = true;
            }

            return hasChanged;
        }

        private bool IsBlockedAt(Vector3 worldCenter)
        {
            if (_blockingColliders == null)
            {
                return false;
            }

            for (int index = 0; index < _blockingColliders.Length; index++)
            {
                Collider2D blocker = _blockingColliders[index];
                if (blocker != null && blocker.enabled && blocker.gameObject.activeInHierarchy &&
                    blocker.OverlapPoint(worldCenter))
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetFlowAfterTopologyChange()
        {
            _stepElapsed = 0f;
            _model.ResetToSource();
            SampleBlockers();
            RenderWater();
        }

        private void RenderWater()
        {
            for (int row = 0; row < _model.Height; row++)
            {
                for (int column = 0; column < _model.Width; column++)
                {
                    _renderTiles[GetIndex(column, row)] = _model.IsWater(column, row) ? _waterTile : null;
                }
            }

            _waterTilemap.SetTilesBlock(_networkBounds, _renderTiles);
            WaterStateChanged?.Invoke();
        }

        private bool TryToModelCoordinates(Vector3Int cell, out int column, out int row)
        {
            column = cell.x - _networkBounds.xMin;
            row = cell.y - _networkBounds.yMin;
            return column >= 0 && column < _model.Width && row >= 0 && row < _model.Height;
        }

        private void ToModelCoordinates(Vector3Int cell, out int column, out int row)
        {
            column = cell.x - _networkBounds.xMin;
            row = cell.y - _networkBounds.yMin;
        }

        private int GetIndex(int column, int row)
        {
            return row * _model.Width + column;
        }

        private void DisableForInvalidConfiguration(string reason)
        {
            Debug.LogError("俯视管道水配置无效：" + reason, this);
            enabled = false;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
