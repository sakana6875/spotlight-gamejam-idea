using System;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 将一组TopView管网格映射到一个SideView水视觉。
    /// </summary>
    [Serializable]
    public sealed class Demo2SideWaterVisualBinding
    {
        [SerializeField] private Vector3Int[] _requiredCells;
        [SerializeField] private GameObject _wetVisual;

        /// <summary>
        /// 绑定包含的必需有水格数量。
        /// </summary>
        public int RequiredCellCount => _requiredCells?.Length ?? 0;

        /// <summary>
        /// SideView中由本绑定控制的水视觉。
        /// </summary>
        public GameObject WetVisual => _wetVisual;

        /// <summary>
        /// 返回指定索引的TopView必需有水格。
        /// </summary>
        public Vector3Int GetRequiredCell(int index)
        {
            if (_requiredCells == null || index < 0 || index >= _requiredCells.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _requiredCells[index];
        }

        /// <summary>
        /// 校验必需格、重复坐标和视觉引用。
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (_requiredCells == null || _requiredCells.Length == 0)
            {
                error = "SideView水视觉绑定至少需要一个Required Cell。";
                return false;
            }

            for (int index = 0; index < _requiredCells.Length; index++)
            {
                for (int previous = 0; previous < index; previous++)
                {
                    if (_requiredCells[previous] == _requiredCells[index])
                    {
                        error = $"SideView水视觉绑定包含重复格：{_requiredCells[index]}。";
                        return false;
                    }
                }
            }

            if (_wetVisual == null)
            {
                error = "SideView水视觉绑定缺少Wet Visual。";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// 判断全部必需格当前是否均已有水。
        /// </summary>
        public bool IsWet(Demo2PipeWaterController waterController)
        {
            if (waterController == null || !waterController.IsConfigured ||
                _requiredCells == null || _requiredCells.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < _requiredCells.Length; index++)
            {
                if (!waterController.IsWaterAtCell(_requiredCells[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
