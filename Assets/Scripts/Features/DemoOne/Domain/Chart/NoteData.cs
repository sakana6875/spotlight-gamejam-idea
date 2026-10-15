using System;

namespace Spotlight.Features.DemoOne.Domain.Chart
{
    /// <summary>
    /// 谱面中的单个节奏点。时间单位为秒，表示从音乐开始到该节奏点的时间。
    /// 使用 readonly struct 保证不可变，避免运行期被意外修改。
    /// </summary>
    [Serializable]
    public readonly struct NoteData
    {
        /// <summary>节奏点相对音乐开始的时间（秒）。</summary>
        public readonly float Time;

        /// <summary>节奏点在谱面中的序号，从 0 开始。</summary>
        public readonly int Index;

        public NoteData(float time, int index)
        {
            Time = time;
            Index = index;
        }
    }
}