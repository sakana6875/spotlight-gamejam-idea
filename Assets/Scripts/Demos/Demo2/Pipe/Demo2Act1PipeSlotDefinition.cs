using System;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 定义第一幕俯视管网中的稳定目标Slot及其正确管段条件。
    /// </summary>
    [Serializable]
    public sealed class Demo2Act1PipeSlotDefinition
    {
        [SerializeField] private string _slotId;
        [SerializeField] private Vector3Int _gridOrigin;
        [SerializeField] private Demo2PipeShape _requiredShape;
        [Range(0, 3)]
        [SerializeField] private int _requiredQuarterTurns;

        /// <summary>
        /// 创建一个稳定目标Slot定义。
        /// </summary>
        public Demo2Act1PipeSlotDefinition(
            string slotId,
            Vector3Int gridOrigin,
            Demo2PipeShape requiredShape,
            int requiredQuarterTurns)
        {
            _slotId = slotId;
            _gridOrigin = gridOrigin;
            _requiredShape = requiredShape;
            _requiredQuarterTurns = requiredQuarterTurns;
        }

        public string SlotId => _slotId;

        public Vector3Int GridOrigin => _gridOrigin;

        public Demo2PipeShape RequiredShape => _requiredShape;

        public int RequiredQuarterTurns => _requiredQuarterTurns;

        /// <summary>
        /// 校验稳定ID、Shape和90度朝向范围。
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(_slotId))
            {
                error = "第一幕管道Slot ID不能为空。";
                return false;
            }

            if (_requiredShape == null)
            {
                error = "第一幕管道Slot缺少Shape。";
                return false;
            }

            if (!_requiredShape.TryValidate(out string shapeError))
            {
                error = "第一幕管道Slot Shape无效：" + shapeError;
                return false;
            }
            if (_requiredQuarterTurns < 0 || _requiredQuarterTurns > 3)
            {
                error = "第一幕管道Slot朝向必须位于0至3。";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// 判断已登记管段是否精确匹配Shape、原点和朝向。
        /// </summary>
        public bool Matches(Demo2PipeSegment segment)
        {
            return segment != null && segment.IsRegistered &&
                MatchesPlacement(segment.Shape, segment.GridOrigin, segment.QuarterTurns);
        }

        /// <summary>
        /// 判断一次管段登记事实是否精确匹配Shape、原点和朝向。
        /// </summary>
        public bool MatchesPlacement(Demo2PipeShape shape, Vector3Int gridOrigin, int quarterTurns)
        {
            return shape == _requiredShape && gridOrigin == _gridOrigin &&
                Demo2WaterPortUtility.NormalizeQuarterTurns(quarterTurns) == _requiredQuarterTurns;
        }
        /// <summary>
        /// 判断Slot全部占格当前是否均已有水。
        /// </summary>
        public bool IsFullyWet(Demo2PipeWaterController waterController)
        {
            if (waterController == null)
            {
                return false;
            }

            for (int index = 0; index < _requiredShape.CellCount; index++)
            {
                Demo2PipeCellDefinition definition =
                    _requiredShape.GetRotatedCell(index, _requiredQuarterTurns);
                Vector3Int cell = _gridOrigin + new Vector3Int(definition.Offset.x, definition.Offset.y, 0);
                if (!waterController.IsWaterAtCell(cell))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
