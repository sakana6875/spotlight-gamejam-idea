using System;

namespace Spotlight.Demos.Demo1.Domain.Chart
{
    /// <summary>
    /// 解析后的谱面数据。由数据层从 JSON 导入后构造，运行时只读使用。
    /// </summary>
    public sealed class ChartData
    {
        /// <summary>音乐标识，用于查找对应 AudioCue。</summary>
        public string SongId { get; }

        /// <summary>BPM，仅作参考，判定不依赖 BPM。</summary>
        public float Bpm { get; }

        /// <summary>音频延迟补偿（秒）。正值表示谱面时间整体后移。</summary>
        public float Offset { get; }

        /// <summary>按时间升序排列的节奏点数组。</summary>
        public NoteData[] Notes { get; }

        public ChartData(string songId, float bpm, float offset, NoteData[] notes)
        {
            if (notes == null)
            {
                throw new ArgumentNullException(nameof(notes));
            }
            SongId = songId;
            Bpm = bpm;
            Offset = offset;
            Notes = notes;
        }

        /// <summary>谱面最后一个节奏点的时间（秒）。空谱面返回 0。</summary>
        public float LastNoteTime => Notes.Length > 0 ? Notes[Notes.Length - 1].Time : 0f;
    }
}