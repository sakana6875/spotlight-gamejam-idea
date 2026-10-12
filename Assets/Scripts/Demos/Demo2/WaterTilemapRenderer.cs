using UnityEngine;
using UnityEngine.Tilemaps;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 保留手工源格，并将模型扩张水格写入同一专用 Tilemap。
    /// </summary>
    public sealed class WaterTilemapRenderer : MonoBehaviour
    {
        [SerializeField] private Tilemap _waterTilemap;
        [SerializeField] private TileBase _waterTile;
        [SerializeField] private float _flowTimeScale = 1f;
        [SerializeField] private float _flowPhaseSecondsPerCell = 0.1f;

        private TileBase[] _sourceTiles;
        private BoundsInt _sourceBufferBounds;
        private int _sourceWidth;
        private int _sourceHeight;
        private TileBase[] _tiles;
        private int _width;
        private int _height;

        /// <summary>
        /// 完整扫描手工铺设的水格，捕获原始 Tile 并返回其包围范围。
        /// </summary>
        /// <remarks>运行时扩张格尚未写入前调用，避免将其误认为持续源格。</remarks>
        public bool TryGetAuthoredSourceBounds(out BoundsInt sourceBounds)
        {
            sourceBounds = new BoundsInt();
            if (_waterTilemap == null)
            {
                return false;
            }

            BoundsInt tilemapBounds = _waterTilemap.cellBounds;
            EnsureSourceBuffer(tilemapBounds);
            bool hasSource = false;
            Vector3Int minimum = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            Vector3Int maximum = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            for (int z = tilemapBounds.zMin; z < tilemapBounds.zMax; z++)
            {
                for (int y = tilemapBounds.yMin; y < tilemapBounds.yMax; y++)
                {
                    for (int x = tilemapBounds.xMin; x < tilemapBounds.xMax; x++)
                    {
                        Vector3Int cell = new Vector3Int(x, y, z);
                        TileBase sourceTile = _waterTilemap.GetTile(cell);
                        _sourceTiles[GetSourceIndex(cell)] = sourceTile;
                        if (sourceTile == null)
                        {
                            continue;
                        }

                        hasSource = true;
                        minimum = Vector3Int.Min(minimum, cell);
                        maximum = Vector3Int.Max(maximum, cell);
                    }
                }
            }

            if (!hasSource)
            {
                return false;
            }

            sourceBounds = new BoundsInt(minimum, maximum - minimum + Vector3Int.one);
            return true;
        }

        /// <summary>
        /// 将完整扫描时捕获的永久源格写入推导水道域的模型。
        /// </summary>
        public void CaptureAuthoredSources(TerrainWaterModel model, BoundsInt waterBounds)
        {
            if (model == null)
            {
                return;
            }

            EnsureRenderBuffer(model.Width, model.Height);
            for (int row = 0; row < model.Height; row++)
            {
                for (int column = 0; column < model.Width; column++)
                {
                    Vector3Int cell = new Vector3Int(
                        waterBounds.xMin + column,
                        waterBounds.yMin + row,
                        waterBounds.zMin);
                    model.SetSource(column, row, GetSourceTile(cell) != null);
                }
            }
        }

        /// <summary>
        /// 使用模型占据状态更新水道范围内的水 Tile，并保留手工源格的原始 Tile。
        /// </summary>
        public void Render(TerrainWaterModel model, BoundsInt waterBounds)
        {
            if (model == null || _waterTilemap == null || _waterTile == null)
            {
                return;
            }

            EnsureRenderBuffer(model.Width, model.Height);
            for (int row = 0; row < model.Height; row++)
            {
                for (int column = 0; column < model.Width; column++)
                {
                    int index = row * model.Width + column;
                    Vector3Int cell = new Vector3Int(
                        waterBounds.xMin + column,
                        waterBounds.yMin + row,
                        waterBounds.zMin);
                    _tiles[index] = model.IsSource(column, row)
                        ? GetSourceTile(cell)
                        : model.IsWater(column, row) ? _waterTile : null;
                }
            }

            _waterTilemap.SetTilesBlock(waterBounds, _tiles);
        }

        /// <summary>
        /// 以列间相位差推进已占水格的 AnimatedTile，使水纹持续向右移动。
        /// </summary>
        public void UpdateFlowAnimation(TerrainWaterModel model, BoundsInt waterBounds, float time)
        {
            if (model == null || _waterTilemap == null || _waterTile == null || _flowTimeScale <= 0f ||
                _flowPhaseSecondsPerCell < 0f)
            {
                return;
            }

            float animationTime = time * _flowTimeScale;
            for (int row = 0; row < model.Height; row++)
            {
                for (int column = 0; column < model.Width; column++)
                {
                    if (!model.IsWater(column, row))
                    {
                        continue;
                    }

                    Vector3Int cell = new Vector3Int(
                        waterBounds.xMin + column,
                        waterBounds.yMin + row,
                        waterBounds.zMin);
                    if (model.IsSource(column, row) && GetSourceTile(cell) != _waterTile)
                    {
                        continue;
                    }

                    _waterTilemap.SetAnimationTime(cell, animationTime - column * _flowPhaseSecondsPerCell);
                }
            }
        }

        /// <summary>
        /// 水格所属 Tilemap，用于将水道域转换为其他组件的坐标系。
        /// </summary>
        public Tilemap WaterTilemap => _waterTilemap;

        /// <summary>
        /// 将 Tilemap 单元中心转换为世界坐标，用于动态 Collider2D 边界采样。
        /// </summary>
        public Vector3 GetCellCenterWorld(Vector3Int cell)
        {
            return _waterTilemap.GetCellCenterWorld(cell);
        }

        /// <summary>
        /// 将世界坐标转换为水 Tilemap 单元。
        /// </summary>
        public Vector3Int WorldToCell(Vector2 worldPosition)
        {
            return _waterTilemap.WorldToCell(worldPosition);
        }

        /// <summary>
        /// 当前引用是否完整。
        /// </summary>
        public bool IsConfigured => _waterTilemap != null && _waterTile != null;

        private void EnsureSourceBuffer(BoundsInt sourceBufferBounds)
        {
            int sourceDepth = sourceBufferBounds.size.z;
            int sourceTileCount = sourceBufferBounds.size.x * sourceBufferBounds.size.y * sourceDepth;
            if (_sourceTiles == null || _sourceBufferBounds != sourceBufferBounds)
            {
                _sourceBufferBounds = sourceBufferBounds;
                _sourceWidth = sourceBufferBounds.size.x;
                _sourceHeight = sourceBufferBounds.size.y;
                _sourceTiles = new TileBase[sourceTileCount];
                return;
            }

            System.Array.Clear(_sourceTiles, 0, _sourceTiles.Length);
        }

        private void EnsureRenderBuffer(int width, int height)
        {
            if (_tiles != null && _width == width && _height == height)
            {
                return;
            }

            _width = width;
            _height = height;
            _tiles = new TileBase[width * height];
        }

        private TileBase GetSourceTile(Vector3Int cell)
        {
            if (_sourceTiles == null || !_sourceBufferBounds.Contains(cell))
            {
                return null;
            }

            return _sourceTiles[GetSourceIndex(cell)];
        }

        private int GetSourceIndex(Vector3Int cell)
        {
            int column = cell.x - _sourceBufferBounds.xMin;
            int row = cell.y - _sourceBufferBounds.yMin;
            int layer = cell.z - _sourceBufferBounds.zMin;
            return (layer * _sourceHeight + row) * _sourceWidth + column;
        }
    }
}
