using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 定义一种水管在本地网格中的占格和双向连接端口。
    /// </summary>
    [CreateAssetMenu(fileName = "PipeShape", menuName = "Spotlight/Demo2/Pipe Shape")]
    public sealed class Demo2PipeShape : ScriptableObject
    {
        [SerializeField] private Demo2PipeCellDefinition[] _cells;

        public int CellCount => _cells?.Length ?? 0;

        /// <summary>
        /// 返回指定本地占格定义。
        /// </summary>
        public Demo2PipeCellDefinition GetCell(int index)
        {
            return _cells[index];
        }

        /// <summary>
        /// 返回按Prefab authored朝向旋转后的占格定义。
        /// </summary>
        public Demo2PipeCellDefinition GetRotatedCell(int index, int quarterTurns)
        {
            Demo2PipeCellDefinition cell = GetCell(index);
            return new Demo2PipeCellDefinition(
                Demo2WaterPortUtility.RotateClockwise(cell.Offset, quarterTurns),
                Demo2WaterPortUtility.RotateClockwise(cell.Ports, quarterTurns));
        }

        /// <summary>
        /// 校验Shape非空、占格唯一且每格至少具有一个有效端口。
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (_cells == null || _cells.Length == 0)
            {
                error = "水管Shape至少需要一个占格。";
                return false;
            }

            for (int index = 0; index < _cells.Length; index++)
            {
                Demo2PipeCellDefinition cell = _cells[index];
                if (cell.Ports == Demo2WaterPort.None || !Demo2WaterPortUtility.IsValidMask(cell.Ports))
                {
                    error = $"水管Shape第{index}格必须包含有效端口。";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (_cells[previous].Offset == cell.Offset)
                    {
                        error = $"水管Shape存在重复占格 {cell.Offset}。";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
