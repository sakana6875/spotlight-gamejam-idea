using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 为不同视图中的水系统提供统一的世界坐标查询边界。
    /// </summary>
    public abstract class Demo2WaterPresenceSource : MonoBehaviour
    {
        /// <summary>
        /// 查询指定世界坐标当前是否存在可被玩法感知的水。
        /// </summary>
        public abstract bool IsWaterAtWorldPosition(Vector2 worldPosition);
    }
}
