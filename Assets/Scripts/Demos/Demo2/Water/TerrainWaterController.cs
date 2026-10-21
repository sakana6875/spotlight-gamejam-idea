using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 从手工水源行和地面覆盖推导固定行水道，并逐格向右推进水流。
    /// </summary>
    public sealed class TerrainWaterController : Demo2WaterPresenceSource
    {
        [SerializeField] private Tilemap _sideWallTilemap;
        [SerializeField] private Tilemap _groundTilemap;
        [SerializeField] private Collider2D[] _movingBlockers;
        [SerializeField] private float _flowStepSeconds = 0.15f;
        [SerializeField] private WaterTilemapRenderer _waterRenderer;

        private TerrainWaterModel _model;
        private BoundsInt _waterBounds;
        private int[] _lowestGroundRows;
        private float _stepElapsed;

        /// <summary>
        /// 当前固定行水道模型；配置无效时为 <see langword="null"/>。
        /// </summary>
        public TerrainWaterModel Model => _model;

        private void Awake()
        {
            if (_sideWallTilemap == null || _groundTilemap == null || _waterRenderer == null ||
                !_waterRenderer.IsConfigured)
            {
                DisableForInvalidConfiguration("必须指定 SideWallTilemap、GroundTilemap 和完整的 WaterTilemapRenderer 引用。");
                return;
            }

            if (!IsPositiveFinite(_flowStepSeconds))
            {
                DisableForInvalidConfiguration("流动步长无效。");
                return;
            }

            if (!_waterRenderer.TryGetAuthoredSourceBounds(out BoundsInt sourceBounds))
            {
                DisableForInvalidConfiguration("WaterInteriorTilemap 中没有手工水源格。");
                return;
            }

            if (!TryGetHighestGroundColumn(out int highestGroundColumn))
            {
                DisableForInvalidConfiguration("GroundTilemap 没有可支撑水道的地面格。");
                return;
            }

            _waterBounds = CreateWaterBounds(sourceBounds, highestGroundColumn);
            try
            {
                _model = new TerrainWaterModel(_waterBounds.size.x, _waterBounds.size.y);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                DisableForInvalidConfiguration(exception.Message);
                return;
            }

            _waterRenderer.CaptureAuthoredSources(_model, _waterBounds);
            if (_model.SourceCount == 0)
            {
                DisableForInvalidConfiguration("WaterInteriorTilemap 中没有手工水源格。");
                return;
            }

            SampleBoundaries();
            if (!AreAllSourceCellsSupported())
            {
                DisableForInvalidConfiguration("WaterInteriorTilemap 中存在未被 GroundTilemap 支撑的手工水源格。");
                return;
            }

            WaterDampingArea dampingArea = GetComponent<WaterDampingArea>();
            if (dampingArea == null || !dampingArea.TryConfigureWaterBounds(_waterRenderer.WaterTilemap, _waterBounds))
            {
                DisableForInvalidConfiguration("WaterDampingArea 无法配置固定行水道 Trigger。");
                return;
            }

            _model.ResetToSources();
            _waterRenderer.Render(_model, _waterBounds);
        }

        private void Update()
        {
            if (_model != null)
            {
                _waterRenderer.UpdateFlowAnimation(_model, _waterBounds, Time.time);
            }
        }

        private void FixedUpdate()
        {
            if (_model == null)
            {
                return;
            }

            bool hasChanged = SampleBoundaries();
            _stepElapsed += Time.fixedDeltaTime;
            if (_stepElapsed >= _flowStepSeconds)
            {
                _stepElapsed -= _flowStepSeconds;
                hasChanged |= _model.Step();
            }

            if (hasChanged)
            {
                _waterRenderer.Render(_model, _waterBounds);
            }
        }

        /// <summary>
        /// 查询世界坐标当前位置是否被固定行水道中的水占据。
        /// </summary>
        public override bool IsWaterAtWorldPosition(Vector2 worldPosition)
        {
            if (_model == null)
            {
                return false;
            }

            Vector3Int cell = _waterRenderer.WorldToCell(worldPosition);
            int column = cell.x - _waterBounds.xMin;
            int row = cell.y - _waterBounds.yMin;
            return column >= 0 && column < _model.Width && row >= 0 && row < _model.Height &&
                _model.IsWater(column, row);
        }

        private static BoundsInt CreateWaterBounds(BoundsInt sourceBounds, int highestGroundColumn)
        {
            int width = highestGroundColumn - sourceBounds.xMin + 1;
            return new BoundsInt(
                new Vector3Int(sourceBounds.xMin, sourceBounds.yMin, sourceBounds.zMin),
                new Vector3Int(width, sourceBounds.size.y, 1));
        }

        private bool TryGetHighestGroundColumn(out int highestGroundColumn)
        {
            highestGroundColumn = int.MinValue;
            BoundsInt groundBounds = _groundTilemap.cellBounds;
            bool hasGround = false;

            for (int z = groundBounds.zMin; z < groundBounds.zMax; z++)
            {
                for (int y = groundBounds.yMin; y < groundBounds.yMax; y++)
                {
                    for (int x = groundBounds.xMin; x < groundBounds.xMax; x++)
                    {
                        Vector3Int groundCell = new Vector3Int(x, y, z);
                        if (!_groundTilemap.HasTile(groundCell))
                        {
                            continue;
                        }

                        Vector3Int waterCell = _waterRenderer.WorldToCell(_groundTilemap.GetCellCenterWorld(groundCell));
                        highestGroundColumn = Mathf.Max(highestGroundColumn, waterCell.x);
                        hasGround = true;
                    }
                }
            }

            return hasGround;
        }

        private bool SampleBoundaries()
        {
            SampleLowestGroundRows();
            bool hasChanged = false;
            for (int row = 0; row < _model.Height; row++)
            {
                for (int column = 0; column < _model.Width; column++)
                {
                    Vector3Int waterCell = new Vector3Int(
                        _waterBounds.xMin + column,
                        _waterBounds.yMin + row,
                        _waterBounds.zMin);
                    Vector3 worldCenter = _waterRenderer.GetCellCenterWorld(waterCell);
                    Vector3Int wallCell = _sideWallTilemap.WorldToCell(worldCenter);
                    bool hasGroundSupport = _lowestGroundRows[column] < waterCell.y;
                    bool isBlocked = !hasGroundSupport || _sideWallTilemap.HasTile(wallCell) ||
                        IsBlockedByMovingCollider(worldCenter);
                    hasChanged |= _model.SetBlocked(column, row, isBlocked);
                }
            }

            return hasChanged;
        }

        private void SampleLowestGroundRows()
        {
            if (_lowestGroundRows == null || _lowestGroundRows.Length != _model.Width)
            {
                _lowestGroundRows = new int[_model.Width];
            }

            for (int column = 0; column < _lowestGroundRows.Length; column++)
            {
                _lowestGroundRows[column] = int.MaxValue;
            }

            BoundsInt groundBounds = _groundTilemap.cellBounds;
            for (int z = groundBounds.zMin; z < groundBounds.zMax; z++)
            {
                for (int y = groundBounds.yMin; y < groundBounds.yMax; y++)
                {
                    for (int x = groundBounds.xMin; x < groundBounds.xMax; x++)
                    {
                        Vector3Int groundCell = new Vector3Int(x, y, z);
                        if (!_groundTilemap.HasTile(groundCell))
                        {
                            continue;
                        }

                        Vector3Int waterCell = _waterRenderer.WorldToCell(_groundTilemap.GetCellCenterWorld(groundCell));
                        int column = waterCell.x - _waterBounds.xMin;
                        if (column >= 0 && column < _lowestGroundRows.Length &&
                            waterCell.y < _lowestGroundRows[column])
                        {
                            _lowestGroundRows[column] = waterCell.y;
                        }
                    }
                }
            }
        }

        private bool AreAllSourceCellsSupported()
        {
            for (int row = 0; row < _model.Height; row++)
            {
                for (int column = 0; column < _model.Width; column++)
                {
                    if (_model.IsSource(column, row) &&
                        _lowestGroundRows[column] >= _waterBounds.yMin + row)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsBlockedByMovingCollider(Vector3 worldCenter)
        {
            if (_movingBlockers == null)
            {
                return false;
            }

            for (int index = 0; index < _movingBlockers.Length; index++)
            {
                Collider2D blocker = _movingBlockers[index];
                if (blocker != null && blocker.enabled && blocker.gameObject.activeInHierarchy &&
                    blocker.OverlapPoint(worldCenter))
                {
                    return true;
                }
            }

            return false;
        }

        private void DisableForInvalidConfiguration(string reason)
        {
            Debug.LogError("固定行水道配置无效：" + reason, this);
            enabled = false;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
