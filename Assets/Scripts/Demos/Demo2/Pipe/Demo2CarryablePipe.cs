using UnityEngine;

namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 标记一个允许被玩家拾取的管段。
    /// </summary>
    [RequireComponent(typeof(Demo2PipeSegment))]
    public sealed class Demo2CarryablePipe : MonoBehaviour
    {
        private Demo2PipeSegment _segment;

        public Demo2PipeSegment Segment => _segment;

        private void Awake()
        {
            _segment = GetComponent<Demo2PipeSegment>();
        }
    }
}
