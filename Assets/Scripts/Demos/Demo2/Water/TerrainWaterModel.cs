using System;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 表示手工源格持续供水、且每条水道只在原行内向右扩展的离散水体状态。
    /// </summary>
    public sealed class TerrainWaterModel
    {
        private readonly bool[] _sourceCells;
        private readonly bool[] _blockedCells;
        private readonly bool[] _waterCells;
        private readonly bool[] _nextWaterCells;
        private int _sourceCount;

        /// <summary>
        /// 创建指定固定行范围内的离散水格模型。
        /// </summary>
        public TerrainWaterModel(int width, int height)
        {
            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "水体范围至少需要一列。");
            }

            if (height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "水体范围至少需要一行。");
            }

            Width = width;
            Height = height;
            _sourceCells = new bool[width * height];
            _blockedCells = new bool[width * height];
            _waterCells = new bool[width * height];
            _nextWaterCells = new bool[width * height];
        }

        public int Width { get; }

        public int Height { get; }

        public int SourceCount => _sourceCount;

        public void SetSource(int column, int row, bool isSource)
        {
            int index = GetIndex(column, row);
            if (_sourceCells[index] == isSource)
            {
                return;
            }

            _sourceCells[index] = isSource;
            _sourceCount += isSource ? 1 : -1;
        }

        public bool IsSource(int column, int row)
        {
            return _sourceCells[GetIndex(column, row)];
        }

        public bool SetBlocked(int column, int row, bool isBlocked)
        {
            int index = GetIndex(column, row);
            if (_blockedCells[index] == isBlocked)
            {
                return false;
            }

            _blockedCells[index] = isBlocked;
            if (!isBlocked || !_waterCells[index])
            {
                return true;
            }

            _waterCells[index] = false;
            _nextWaterCells[index] = false;
            return true;
        }

        public void ResetToSources()
        {
            Array.Clear(_waterCells, 0, _waterCells.Length);
            Array.Clear(_nextWaterCells, 0, _nextWaterCells.Length);
            for (int index = 0; index < _sourceCells.Length; index++)
            {
                if (_sourceCells[index] && !_blockedCells[index])
                {
                    _waterCells[index] = true;
                }
            }
        }

        public bool IsWater(int column, int row)
        {
            return _waterCells[GetIndex(column, row)];
        }

        /// <summary>
        /// 推进一步；每个水格最多向同一行的右侧扩展一格，阻挡不会触发跨行蓄积。
        /// </summary>
        public bool Step()
        {
            Array.Copy(_waterCells, _nextWaterCells, _waterCells.Length);
            bool hasNewWater = false;

            for (int index = 0; index < _sourceCells.Length; index++)
            {
                if (_sourceCells[index] && !_blockedCells[index] && !_nextWaterCells[index])
                {
                    _nextWaterCells[index] = true;
                    hasNewWater = true;
                }
            }

            for (int row = 0; row < Height; row++)
            {
                for (int column = 0; column < Width - 1; column++)
                {
                    if (IsWater(column, row) && !_blockedCells[GetIndex(column + 1, row)])
                    {
                        hasNewWater |= MarkWater(column + 1, row);
                    }
                }
            }

            Array.Copy(_nextWaterCells, _waterCells, _waterCells.Length);
            return hasNewWater;
        }

        private bool MarkWater(int column, int row)
        {
            int index = GetIndex(column, row);
            if (_blockedCells[index] || _nextWaterCells[index])
            {
                return false;
            }

            _nextWaterCells[index] = true;
            return true;
        }

        private int GetIndex(int column, int row)
        {
            if (column < 0 || column >= Width)
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }

            if (row < 0 || row >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }

            return row * Width + column;
        }
    }
}
