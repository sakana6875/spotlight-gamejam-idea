using System;
using UnityEngine;
using Spotlight.Features.DemoOne.Domain.Chart;

namespace Spotlight.Features.DemoOne.Data
{
    /// <summary>
    /// 从 JSON 字符串解析谱面。纯逻辑，可在 EditMode 中直接测试。
    /// 校验失败时返回失败结果，不抛异常。
    /// </summary>
    public static class ChartJsonParser
    {
        /// <summary>
        /// 解析 JSON 为 ChartData。失败时返回 IsSuccess = false 的结果，
        /// 调用方应展示 ErrorMessage 并且不覆盖已有资产。
        /// </summary>
        public static ChartParseResult Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return ChartParseResult.Failure("JSON 内容为空。");
            }

            ChartJsonDto dto;
            try
            {
                dto = JsonUtility.FromJson<ChartJsonDto>(json);
            }
            catch (Exception ex)
            {
                return ChartParseResult.Failure($"JSON 解析失败：{ex.Message}");
            }

            if (dto == null)
            {
                return ChartParseResult.Failure("JSON 解析结果为空。");
            }
            if (string.IsNullOrWhiteSpace(dto.songId))
            {
                return ChartParseResult.Failure("songId 不能为空。");
            }
            if (dto.bpm <= 0f)
            {
                return ChartParseResult.Failure($"bpm 必须大于 0，实际为 {dto.bpm}。");
            }
            if (dto.notes == null || dto.notes.Length == 0)
            {
                return ChartParseResult.Failure("notes 不能为空。");
            }

            var notes = new NoteData[dto.notes.Length];
            for (int i = 0; i < dto.notes.Length; i++)
            {
                var n = dto.notes[i];
                if (n.time < 0f)
                {
                    return ChartParseResult.Failure($"notes[{i}].time 不能为负。");
                }
                if (i > 0 && n.time < dto.notes[i - 1].time)
                {
                    return ChartParseResult.Failure(
                        $"notes[{i}].time ({n.time}) 必须不小于 notes[{i - 1}].time ({dto.notes[i - 1].time})。");
                }
                if (n.index != i)
                {
                    return ChartParseResult.Failure(
                        $"notes[{i}].index 必须等于 {i}，实际为 {n.index}。");
                }
                notes[i] = new NoteData(n.time, n.index);
            }

            var chart = new ChartData(dto.songId, dto.bpm, dto.offset, notes);
            return ChartParseResult.Success(chart);
        }

        /// <summary>JSON 反序列化的中间 DTO。字段名必须与 JSON 键名一致。</summary>
        [Serializable]
        internal sealed class ChartJsonDto
        {
            public string songId;
            public float bpm;
            public float offset;
            public NoteJsonDto[] notes;
        }

        [Serializable]
        internal sealed class NoteJsonDto
        {
            public float time;
            public int index;
        }
    }

    /// <summary>JSON 解析结果。成功时 Data 非空，失败时 ErrorMessage 非空。</summary>
    public sealed class ChartParseResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
        public ChartData Data { get; }

        private ChartParseResult(bool success, string error, ChartData data)
        {
            IsSuccess = success;
            ErrorMessage = error;
            Data = data;
        }

        public static ChartParseResult Success(ChartData data)
            => new ChartParseResult(true, null, data);

        public static ChartParseResult Failure(string error)
            => new ChartParseResult(false, error, null);
    }
}