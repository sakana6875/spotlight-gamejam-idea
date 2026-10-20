using System;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 维护第一幕有限网格中的双向路线、唯一水源、管端口、阻挡和逐格供水状态。
    /// </summary>
    public sealed class Demo2PipeWaterModel
    {
        private static readonly Demo2WaterPort[] CardinalPorts =
        {
            Demo2WaterPort.Up,
            Demo2WaterPort.Right,
            Demo2WaterPort.Down,
            Demo2WaterPort.Left
        };

        private readonly Demo2WaterPort[] _routePorts;
        private readonly Demo2WaterPort[] _pipePorts;
        private readonly bool[] _pipeOccupied;
        private readonly bool[] _blocked;
        private readonly bool[] _water;
        private readonly bool[] _nextWater;
        private int _sourceIndex = -1;

        public Demo2PipeWaterModel(int width, int height)
        {
            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
            int cellCount = width * height;
            _routePorts = new Demo2WaterPort[cellCount];
            _pipePorts = new Demo2WaterPort[cellCount];
            _pipeOccupied = new bool[cellCount];
            _blocked = new bool[cellCount];
            _water = new bool[cellCount];
            _nextWater = new bool[cellCount];
        }

        public int Width { get; }

        public int Height { get; }

        /// <summary>
        /// 设置一个路线格与正交相邻Marker形成的双向端口。
        /// </summary>
        public void SetRoute(int column, int row, Demo2WaterPort ports)
        {
            if (!Demo2WaterPortUtility.IsValidMask(ports))
            {
                throw new ArgumentOutOfRangeException(nameof(ports));
            }

            int index = GetIndex(column, row);
            if (_routePorts[index] == ports)
            {
                return;
            }

            _routePorts[index] = ports;
            ResetToSource();
        }

        /// <summary>
        /// 设置当前有限网格的唯一水源格。
        /// </summary>
        public void SetSource(int column, int row)
        {
            int sourceIndex = GetIndex(column, row);
            if (_sourceIndex == sourceIndex)
            {
                return;
            }

            _sourceIndex = sourceIndex;
            ResetToSource();
        }

        public void SetPipe(int column, int row, bool isOccupied, Demo2WaterPort ports)
        {
            if (isOccupied && (ports == Demo2WaterPort.None || !Demo2WaterPortUtility.IsValidMask(ports)))
            {
                throw new ArgumentOutOfRangeException(nameof(ports));
            }

            int index = GetIndex(column, row);
            Demo2WaterPort effectivePorts = isOccupied ? ports : Demo2WaterPort.None;
            if (_pipeOccupied[index] == isOccupied && _pipePorts[index] == effectivePorts)
            {
                return;
            }

            _pipeOccupied[index] = isOccupied;
            _pipePorts[index] = effectivePorts;
            ResetToSource();
        }

        public void SetBlocked(int column, int row, bool isBlocked)
        {
            int index = GetIndex(column, row);
            if (_blocked[index] == isBlocked)
            {
                return;
            }

            _blocked[index] = isBlocked;
            ResetToSource();
        }

        public bool IsWater(int column, int row)
        {
            return _water[GetIndex(column, row)];
        }

        public bool IsPipeOccupied(int column, int row)
        {
            return _pipeOccupied[GetIndex(column, row)];
        }

        /// <summary>
        /// 清除非源水，只保留未被阻挡的唯一水源格。
        /// </summary>
        public void ResetToSource()
        {
            Array.Clear(_water, 0, _water.Length);
            Array.Clear(_nextWater, 0, _nextWater.Length);
            if (_sourceIndex >= 0 && !_blocked[_sourceIndex])
            {
                _water[_sourceIndex] = true;
            }
        }

        /// <summary>
        /// 沿双向路线和互相匹配的管端口推进一层供水。
        /// </summary>
        public bool Step()
        {
            Array.Copy(_water, _nextWater, _water.Length);
            bool hasChanged = false;

            for (int row = 0; row < Height; row++)
            {
                for (int column = 0; column < Width; column++)
                {
                    int currentIndex = GetIndex(column, row);
                    if (!_water[currentIndex] || _blocked[currentIndex])
                    {
                        continue;
                    }

                    foreach (Demo2WaterPort direction in CardinalPorts)
                    {
                        if (!CanExitCurrentCell(currentIndex, direction))
                        {
                            continue;
                        }

                        UnityEngine.Vector2Int offset = Demo2WaterPortUtility.ToOffset(direction);
                        int nextColumn = column + offset.x;
                        int nextRow = row + offset.y;
                        if (!Contains(nextColumn, nextRow))
                        {
                            continue;
                        }

                        int nextIndex = GetIndex(nextColumn, nextRow);
                        Demo2WaterPort opposite = Demo2WaterPortUtility.Opposite(direction);
                        if (_blocked[nextIndex] || !_pipeOccupied[nextIndex] ||
                            (_routePorts[nextIndex] & opposite) == 0 ||
                            (_pipePorts[nextIndex] & opposite) == 0 || _nextWater[nextIndex])
                        {
                            continue;
                        }

                        _nextWater[nextIndex] = true;
                        hasChanged = true;
                    }
                }
            }

            Array.Copy(_nextWater, _water, _water.Length);
            return hasChanged;
        }

        private bool CanExitCurrentCell(int index, Demo2WaterPort direction)
        {
            if ((_routePorts[index] & direction) == 0)
            {
                return false;
            }

            return index == _sourceIndex ||
                (_pipeOccupied[index] && (_pipePorts[index] & direction) != 0);
        }

        private bool Contains(int column, int row)
        {
            return column >= 0 && column < Width && row >= 0 && row < Height;
        }

        private int GetIndex(int column, int row)
        {
            if (!Contains(column, row))
            {
                throw new ArgumentOutOfRangeException();
            }

            return row * Width + column;
        }
    }

}
