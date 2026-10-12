using System;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 表示手工源格持续供水、仅向右扩展并在障碍前蓄积的离散水体状态。
    /// </summary>
    public sealed class TerrainWaterModel
    {
        private readonly bool[] _sourceCells;
        private readonly bool[] _blockedCells;
        private readonly bool[] _waterCells;
        private readonly bool[] _nextWaterCells;
        private int _sourceCount;

        /// <summary>
        /// 创建指定范围内的离散水格模型。
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

        /// <summary>
        /// 模拟区域的列数。
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// 模拟区域的行数。
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// 模拟范围内的格数量，用于有界稳定求解。
        /// </summary>
        public int CellCount => _waterCells.Length;

        /// <summary>
        /// 当前模拟范围内手工源格的数量。
        /// </summary>
        public int SourceCount => _sourceCount;

        /// <summary>
        /// 设置用户手工铺设的永久源格。
        /// </summary>
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

        /// <summary>
        /// 查询指定格是否为永久源格。
        /// </summary>
        public bool IsSource(int column, int row)
        {
            return _sourceCells[GetIndex(column, row)];
        }

        /// <summary>
        /// 设置边界格是否阻挡水。阻挡格不能保留水。
        /// </summary>
        /// <returns>边界变化或水格被清除时返回 <see langword="true"/>。</returns>
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

        /// <summary>
        /// 以当前未被阻挡的源格重置水体。
        /// </summary>
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

        /// <summary>
        /// 查询指定格是否阻挡水。
        /// </summary>
        public bool IsBlocked(int column, int row)
        {
            return _blockedCells[GetIndex(column, row)];
        }

        /// <summary>
        /// 查询指定格是否已被水占据。
        /// </summary>
        public bool IsWater(int column, int row)
        {
            return _waterCells[GetIndex(column, row)];
        }

        /// <summary>
        /// 推进一步。源格持续补水，水仅向右扩展；右侧受阻时在连续水体上方蓄积。
        /// </summary>
        /// <returns>本步是否新增了水格。</returns>
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
                    if (!IsWater(column, row))
                    {
                        continue;
                    }

                    if (IsBlocked(column + 1, row))
                    {
                        hasNewWater |= RaiseWaterBeforeBarrier(column, row);
                        continue;
                    }

                    hasNewWater |= MarkWater(column + 1, row);
                }
            }

            Array.Copy(_nextWaterCells, _waterCells, _waterCells.Length);
            return hasNewWater;
        }

        private bool RaiseWaterBeforeBarrier(int lastWaterColumn, int row)
        {
            int raisedRow = row + 1;
            if (raisedRow >= Height)
            {
                return false;
            }

            bool hasNewWater = false;
            for (int column = lastWaterColumn; column >= 0; column--)
            {
                if (IsBlocked(column, raisedRow) || !IsWater(column, row))
                {
                    break;
                }

                hasNewWater |= MarkWater(column, raisedRow);
            }

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
