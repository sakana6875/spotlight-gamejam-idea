using System;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 俯视兽王的追击寻路。障碍按轴对齐矩形膨胀兽王半径，中心点不会走进树里。
    /// 数组在构造时分配，重复寻路不再分配。
    /// </summary>
    public sealed class Demo2BossPathfinder
    {
        public const float CellSize = 0.5f;
        public const int GridSize = 64;

        private const int CellCount = GridSize * GridSize;
        private const float DiagonalCost = 1.41421356f;
        private const float ArriveDistance = 0.05f;

        private readonly float[] _gScore = new float[CellCount];
        private readonly float[] _fScore = new float[CellCount];
        private readonly int[] _cameFrom = new int[CellCount];
        private readonly int[] _heap = new int[CellCount * 4];
        private readonly bool[] _closed = new bool[CellCount];
        private readonly float[] _wayX = new float[CellCount];
        private readonly float[] _wayY = new float[CellCount];

        private int _heapCount;
        private int _wayCount;
        private int _wayIndex;
        private int _plannedGoalCell = -1;
        private float _originX;
        private float _originY;
        private bool _hasPath;

        /// <summary>
        /// 沿绕开障碍的路径走出不超过 <paramref name="maxDistance"/> 的一步。
        /// 已经陷在障碍里时，这一步朝最近的空格退出，允许暂时仍重叠。
        /// </summary>
        public bool TryStep(
            float fromX,
            float fromY,
            float toX,
            float toY,
            float maxDistance,
            int obstacleCount,
            float[] minX,
            float[] minY,
            float[] maxX,
            float[] maxY,
            float clearance,
            out float stepX,
            out float stepY)
        {
            stepX = 0f;
            stepY = 0f;
            if (maxDistance <= 0f || float.IsNaN(maxDistance) || float.IsInfinity(maxDistance) ||
                !IsFinite(fromX) || !IsFinite(fromY) || !IsFinite(toX) || !IsFinite(toY) ||
                clearance < 0f || !IsFinite(clearance))
            {
                return false;
            }

            bool inside = Overlaps(fromX, fromY, obstacleCount, minX, minY, maxX, maxY, clearance);
            if (!inside && !SegmentHits(fromX, fromY, toX, toY, obstacleCount, minX, minY, maxX, maxY, clearance))
            {
                _hasPath = false;
                _plannedGoalCell = -1;
                return TakeToward(fromX, fromY, toX, toY, maxDistance, out stepX, out stepY);
            }

            if (!EnsurePath(fromX, fromY, toX, toY, obstacleCount, minX, minY, maxX, maxY, clearance, inside))
            {
                return false;
            }

            if (inside)
            {
                return TakeToward(fromX, fromY, _wayX[_wayIndex], _wayY[_wayIndex], maxDistance, out stepX, out stepY);
            }

            PullString(fromX, fromY, obstacleCount, minX, minY, maxX, maxY, clearance);
            float targetX = _wayX[_wayIndex];
            float targetY = _wayY[_wayIndex];
            if (!TakeToward(fromX, fromY, targetX, targetY, maxDistance, out stepX, out stepY))
            {
                return false;
            }

            if (Overlaps(fromX + stepX, fromY + stepY, obstacleCount, minX, minY, maxX, maxY, clearance) ||
                SegmentHits(fromX, fromY, fromX + stepX, fromY + stepY, obstacleCount, minX, minY, maxX, maxY, clearance))
            {
                stepX = 0f;
                stepY = 0f;
                _hasPath = false;
                return false;
            }

            float remainX = targetX - (fromX + stepX);
            float remainY = targetY - (fromY + stepY);
            if (remainX * remainX + remainY * remainY <= ArriveDistance * ArriveDistance &&
                _wayIndex < _wayCount - 1)
            {
                _wayIndex++;
            }

            return true;
        }

        private bool EnsurePath(
            float fromX,
            float fromY,
            float toX,
            float toY,
            int obstacleCount,
            float[] minX,
            float[] minY,
            float[] maxX,
            float[] maxY,
            float clearance,
            bool inside)
        {
            FitOrigin(fromX, fromY, toX, toY);
            int goalCell = NearestFreeCell(toX, toY, obstacleCount, minX, minY, maxX, maxY, clearance);
            if (goalCell < 0)
            {
                _hasPath = false;
                return false;
            }

            bool goalMoved = goalCell != _plannedGoalCell;
            bool waypointBlocked = _hasPath &&
                _wayIndex < _wayCount &&
                Overlaps(_wayX[_wayIndex], _wayY[_wayIndex], obstacleCount, minX, minY, maxX, maxY, clearance);
            if (_hasPath && !goalMoved && !waypointBlocked && !inside)
            {
                return true;
            }

            int startCell = inside
                ? NearestFreeCell(fromX, fromY, obstacleCount, minX, minY, maxX, maxY, clearance)
                : CellIndex(fromX, fromY);
            if (startCell < 0 || !Search(startCell, goalCell, obstacleCount, minX, minY, maxX, maxY, clearance))
            {
                _hasPath = false;
                _plannedGoalCell = -1;
                return false;
            }

            WritePath(startCell, goalCell, toX, toY, obstacleCount, minX, minY, maxX, maxY, clearance);
            if (inside)
            {
                _wayIndex = 0;
            }

            _plannedGoalCell = goalCell;
            _hasPath = _wayCount > 0;
            return _hasPath;
        }

        private bool Search(
            int startCell,
            int goalCell,
            int obstacleCount,
            float[] minX,
            float[] minY,
            float[] maxX,
            float[] maxY,
            float clearance)
        {
            for (int index = 0; index < CellCount; index++)
            {
                _gScore[index] = float.PositiveInfinity;
                _closed[index] = false;
            }

            _heapCount = 0;
            _gScore[startCell] = 0f;
            _cameFrom[startCell] = -1;
            HeapPush(startCell, Heuristic(startCell, goalCell));

            while (_heapCount > 0)
            {
                int current = HeapPop();
                if (_closed[current])
                {
                    continue;
                }

                _closed[current] = true;
                if (current == goalCell)
                {
                    return true;
                }

                int cellX = current % GridSize;
                int cellY = current / GridSize;
                for (int neighbor = 0; neighbor < 8; neighbor++)
                {
                    int offsetX = NeighborX(neighbor);
                    int offsetY = NeighborY(neighbor);
                    int nextX = cellX + offsetX;
                    int nextY = cellY + offsetY;
                    if (nextX < 0 || nextY < 0 || nextX >= GridSize || nextY >= GridSize)
                    {
                        continue;
                    }

                    if (offsetX != 0 && offsetY != 0 &&
                        (CellBlocked(cellX + offsetX, cellY, obstacleCount, minX, minY, maxX, maxY, clearance) ||
                         CellBlocked(cellX, cellY + offsetY, obstacleCount, minX, minY, maxX, maxY, clearance)))
                    {
                        continue;
                    }

                    if (CellBlocked(nextX, nextY, obstacleCount, minX, minY, maxX, maxY, clearance))
                    {
                        continue;
                    }

                    int next = nextY * GridSize + nextX;
                    float stepCost = offsetX != 0 && offsetY != 0 ? DiagonalCost : 1f;
                    float tentative = _gScore[current] + stepCost;
                    if (_closed[next] || tentative >= _gScore[next])
                    {
                        continue;
                    }

                    _cameFrom[next] = current;
                    _gScore[next] = tentative;
                    HeapPush(next, tentative + Heuristic(next, goalCell));
                }
            }

            return false;
        }

        private void WritePath(
            int startCell,
            int goalCell,
            float toX,
            float toY,
            int obstacleCount,
            float[] minX,
            float[] minY,
            float[] maxX,
            float[] maxY,
            float clearance)
        {
            int count = 0;
            int cell = goalCell;
            while (cell >= 0 && count < CellCount)
            {
                _wayX[count] = _originX + (cell % GridSize + 0.5f) * CellSize;
                _wayY[count] = _originY + (cell / GridSize + 0.5f) * CellSize;
                count++;
                if (cell == startCell)
                {
                    break;
                }

                cell = _cameFrom[cell];
            }

            for (int index = 0; index < count / 2; index++)
            {
                float swapX = _wayX[index];
                float swapY = _wayY[index];
                _wayX[index] = _wayX[count - 1 - index];
                _wayY[index] = _wayY[count - 1 - index];
                _wayX[count - 1 - index] = swapX;
                _wayY[count - 1 - index] = swapY;
            }

            if (count > 0 &&
                !Overlaps(toX, toY, obstacleCount, minX, minY, maxX, maxY, clearance))
            {
                _wayX[count - 1] = toX;
                _wayY[count - 1] = toY;
            }

            _wayCount = count;
            _wayIndex = count > 1 ? 1 : 0;
        }

        private void PullString(
            float fromX,
            float fromY,
            int obstacleCount,
            float[] minX,
            float[] minY,
            float[] maxX,
            float[] maxY,
            float clearance)
        {
            for (int index = _wayCount - 1; index > _wayIndex; index--)
            {
                if (!SegmentHits(fromX, fromY, _wayX[index], _wayY[index], obstacleCount, minX, minY, maxX, maxY, clearance))
                {
                    _wayIndex = index;
                    return;
                }
            }
        }

        private int NearestFreeCell(
            float x,
            float y,
            int obstacleCount,
            float[] minX,
            float[] minY,
            float[] maxX,
            float[] maxY,
            float clearance)
        {
            int cellX = ClampCell((int)Math.Floor((x - _originX) / CellSize));
            int cellY = ClampCell((int)Math.Floor((y - _originY) / CellSize));
            if (!CellBlocked(cellX, cellY, obstacleCount, minX, minY, maxX, maxY, clearance))
            {
                return cellY * GridSize + cellX;
            }

            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int radius = 1; radius < GridSize; radius++)
            {
                for (int offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    for (int offsetX = -radius; offsetX <= radius; offsetX++)
                    {
                        if (Math.Abs(offsetX) != radius && Math.Abs(offsetY) != radius)
                        {
                            continue;
                        }

                        int nextX = cellX + offsetX;
                        int nextY = cellY + offsetY;
                        if (nextX < 0 || nextY < 0 || nextX >= GridSize || nextY >= GridSize ||
                            CellBlocked(nextX, nextY, obstacleCount, minX, minY, maxX, maxY, clearance))
                        {
                            continue;
                        }

                        float worldX = _originX + (nextX + 0.5f) * CellSize;
                        float worldY = _originY + (nextY + 0.5f) * CellSize;
                        float distance = (worldX - x) * (worldX - x) + (worldY - y) * (worldY - y);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            best = nextY * GridSize + nextX;
                        }
                    }
                }

                if (best >= 0)
                {
                    return best;
                }
            }

            return -1;
        }

        private bool CellBlocked(
            int cellX,
            int cellY,
            int obstacleCount,
            float[] minX,
            float[] minY,
            float[] maxX,
            float[] maxY,
            float clearance)
        {
            float worldX = _originX + (cellX + 0.5f) * CellSize;
            float worldY = _originY + (cellY + 0.5f) * CellSize;
            return Overlaps(worldX, worldY, obstacleCount, minX, minY, maxX, maxY, clearance);
        }

        private static bool Overlaps(
            float x,
            float y,
            int obstacleCount,
            float[] minX,
            float[] minY,
            float[] maxX,
            float[] maxY,
            float clearance)
        {
            for (int index = 0; index < obstacleCount; index++)
            {
                if (x + clearance > minX[index] && x - clearance < maxX[index] &&
                    y + clearance > minY[index] && y - clearance < maxY[index])
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SegmentHits(
            float x0,
            float y0,
            float x1,
            float y1,
            int obstacleCount,
            float[] minX,
            float[] minY,
            float[] maxX,
            float[] maxY,
            float clearance)
        {
            for (int index = 0; index < obstacleCount; index++)
            {
                if (SegmentHitsExpanded(x0, y0, x1, y1, minX[index], minY[index], maxX[index], maxY[index], clearance))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SegmentHitsExpanded(
            float x0,
            float y0,
            float x1,
            float y1,
            float minX,
            float minY,
            float maxX,
            float maxY,
            float clearance)
        {
            float t0 = 0f;
            float t1 = 1f;
            if (!Clip(x0, x1 - x0, minX - clearance, maxX + clearance, ref t0, ref t1))
            {
                return false;
            }

            if (!Clip(y0, y1 - y0, minY - clearance, maxY + clearance, ref t0, ref t1))
            {
                return false;
            }

            return t1 >= 0f && t0 <= 1f;
        }

        private static bool Clip(float origin, float delta, float min, float max, ref float t0, ref float t1)
        {
            if (Math.Abs(delta) < 0.0000001f)
            {
                return origin >= min && origin <= max;
            }

            float inverse = 1f / delta;
            float enter = (min - origin) * inverse;
            float exit = (max - origin) * inverse;
            if (enter > exit)
            {
                float swap = enter;
                enter = exit;
                exit = swap;
            }

            if (enter > t0)
            {
                t0 = enter;
            }

            if (exit < t1)
            {
                t1 = exit;
            }

            return t0 <= t1;
        }

        private void FitOrigin(float fromX, float fromY, float toX, float toY)
        {
            float span = GridSize * CellSize;
            _originX = FitAxis(Math.Min(fromX, toX), Math.Max(fromX, toX), span);
            _originY = FitAxis(Math.Min(fromY, toY), Math.Max(fromY, toY), span);
        }

        private static float FitAxis(float min, float max, float span)
        {
            float origin = (min + max) * 0.5f - span * 0.5f;
            if (min - CellSize < origin)
            {
                origin = min - CellSize;
            }

            if (max + CellSize > origin + span)
            {
                origin = max + CellSize - span;
            }

            return origin;
        }

        private int CellIndex(float x, float y)
        {
            int cellX = ClampCell((int)Math.Floor((x - _originX) / CellSize));
            int cellY = ClampCell((int)Math.Floor((y - _originY) / CellSize));
            return cellY * GridSize + cellX;
        }

        private static int ClampCell(int cell)
        {
            if (cell < 0)
            {
                return 0;
            }

            return cell >= GridSize ? GridSize - 1 : cell;
        }

        private float Heuristic(int fromCell, int goalCell)
        {
            int deltaX = Math.Abs(fromCell % GridSize - goalCell % GridSize);
            int deltaY = Math.Abs(fromCell / GridSize - goalCell / GridSize);
            int straight = Math.Max(deltaX, deltaY);
            int diagonal = Math.Min(deltaX, deltaY);
            return straight + (DiagonalCost - 1f) * diagonal;
        }

        private void HeapPush(int cell, float score)
        {
            if (_heapCount >= _heap.Length)
            {
                return;
            }

            _fScore[cell] = score;
            int index = _heapCount++;
            _heap[index] = cell;
            while (index > 0)
            {
                int parent = (index - 1) >> 1;
                if (_fScore[_heap[parent]] <= _fScore[_heap[index]])
                {
                    return;
                }

                int swap = _heap[parent];
                _heap[parent] = _heap[index];
                _heap[index] = swap;
                index = parent;
            }
        }

        private int HeapPop()
        {
            int top = _heap[0];
            _heapCount--;
            _heap[0] = _heap[_heapCount];
            int index = 0;
            while (true)
            {
                int left = index * 2 + 1;
                if (left >= _heapCount)
                {
                    return top;
                }

                int right = left + 1;
                int smaller = right < _heapCount && _fScore[_heap[right]] < _fScore[_heap[left]] ? right : left;
                if (_fScore[_heap[index]] <= _fScore[_heap[smaller]])
                {
                    return top;
                }

                int swap = _heap[index];
                _heap[index] = _heap[smaller];
                _heap[smaller] = swap;
                index = smaller;
            }
        }

        private static bool TakeToward(
            float fromX,
            float fromY,
            float toX,
            float toY,
            float maxDistance,
            out float stepX,
            out float stepY)
        {
            float deltaX = toX - fromX;
            float deltaY = toY - fromY;
            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (distance <= 0.0001f)
            {
                stepX = 0f;
                stepY = 0f;
                return true;
            }

            float scale = distance <= maxDistance ? 1f : maxDistance / distance;
            stepX = deltaX * scale;
            stepY = deltaY * scale;
            return true;
        }

        private static int NeighborX(int neighbor)
        {
            switch (neighbor)
            {
                case 0: return 1;
                case 1: return -1;
                case 2: return 0;
                case 3: return 0;
                case 4: return 1;
                case 5: return 1;
                case 6: return -1;
                default: return -1;
            }
        }

        private static int NeighborY(int neighbor)
        {
            switch (neighbor)
            {
                case 0: return 0;
                case 1: return 0;
                case 2: return 1;
                case 3: return -1;
                case 4: return 1;
                case 5: return -1;
                case 6: return 1;
                default: return -1;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
