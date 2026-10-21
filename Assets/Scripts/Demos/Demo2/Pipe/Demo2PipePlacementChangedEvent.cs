using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 描述一个管段已经登记到管网或已经从管网移除的事实。
    /// </summary>
    public readonly struct Demo2PipePlacementChangedEvent
    {
        public Demo2PipePlacementChangedEvent(
            Demo2PipeSegment segment,
            Demo2PipeShape shape,
            Vector3Int gridOrigin,
            int quarterTurns,
            bool isPlaced)
        {
            Segment = segment;
            Shape = shape;
            GridOrigin = gridOrigin;
            QuarterTurns = quarterTurns;
            IsPlaced = isPlaced;
        }

        public Demo2PipeSegment Segment { get; }

        public Demo2PipeShape Shape { get; }

        public Vector3Int GridOrigin { get; }

        public int QuarterTurns { get; }

        public bool IsPlaced { get; }
    }
}
