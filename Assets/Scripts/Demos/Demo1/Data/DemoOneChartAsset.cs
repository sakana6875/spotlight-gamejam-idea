using System;
using UnityEngine;
using Spotlight.Demos.Demo1.Domain.Chart;

namespace Spotlight.Demos.Demo1.Data
{
    /// <summary>
    /// 供 Unity 序列化的单个节奏点。字段使用小驼峰以匹配 JSON。
    /// </summary>
    [Serializable]
    public struct NoteEntry
    {
        public float time;
        public int index;
    }

    /// <summary>
    /// Demo1 谱面资产。运行时只读取此资产，不直接读 JSON。
    /// 由 Editor 导入器从 JSON 生成；运行时通过 ToDomain 转换为 ChartData。
    /// </summary>
    [CreateAssetMenu(fileName = "DemoOneChartAsset", menuName = "Spotlight/Demo1/Chart Asset")]
    public sealed class DemoOneChartAsset : ScriptableObject
    {
        [SerializeField] private string _songId;
        [SerializeField] private float _bpm;
        [SerializeField] private float _offset;
        [SerializeField] private NoteEntry[] _notes = Array.Empty<NoteEntry>();

        public string SongId => _songId;
        public float Bpm => _bpm;
        public float Offset => _offset;
        public int NoteCount => _notes?.Length ?? 0;

        // ChartData 含 readonly struct，无法直接序列化，运行时按需构造并缓存。
        [NonSerialized] private ChartData _cached;

        /// <summary>将资产数据转换为运行时领域对象。结果会被缓存。</summary>
        public ChartData ToDomain()
        {
            if (_cached != null)
            {
                return _cached;
            }
            if (_notes == null || _notes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"DemoOneChartAsset '{name}' 尚未导入谱面数据。");
            }
            var notes = new NoteData[_notes.Length];
            for (int i = 0; i < _notes.Length; i++)
            {
                notes[i] = new NoteData(_notes[i].time, _notes[i].index);
            }
            _cached = new ChartData(_songId, _bpm, _offset, notes);
            return _cached;
        }

        /// <summary>由 Editor 导入器调用，写入解析后的谱面数据。</summary>
        public void SetData(string songId, float bpm, float offset, NoteEntry[] notes)
        {
            _songId = songId;
            _bpm = bpm;
            _offset = offset;
            _notes = notes ?? Array.Empty<NoteEntry>();
            _cached = null;
        }
    }
}