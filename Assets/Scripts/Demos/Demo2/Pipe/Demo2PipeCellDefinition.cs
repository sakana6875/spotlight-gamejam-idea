using System;
using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 描述水管Shape中的一个本地1米占格及其双向端口。
    /// </summary>
    [Serializable]
    public struct Demo2PipeCellDefinition
    {
        [SerializeField] private Vector2Int _offset;
        [SerializeField] private Demo2WaterPort _ports;

        /// <summary>
        /// 创建一个水管本地占格定义。
        /// </summary>
        public Demo2PipeCellDefinition(Vector2Int offset, Demo2WaterPort ports)
        {
            _offset = offset;
            _ports = ports;
        }

        public Vector2Int Offset => _offset;

        public Demo2WaterPort Ports => _ports;
    }
}
