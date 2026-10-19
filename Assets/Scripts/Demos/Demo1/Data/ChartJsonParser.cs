using System;
using UnityEngine;
using Spotlight.Demos.Demo1.Domain.Chart;

namespace Spotlight.Demos.Demo1.Data
{
    /// <summary>
    /// 把外部 JSON 转换为稳定的 Demo1 谱面数据。
    /// </summary>
    public static class ChartJsonParser
    {
        /// <summary>
        /// 解析并校验 JSON；失败时返回包含字段位置的错误，不产生谱面数据。
        /// </summary>
        public static ChartParseResult Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return ChartParseResult.Failure("JSON 内容不能为空。");
            }

            ChartJsonDto dto;
            try
            {
                dto = JsonUtility.FromJson<ChartJsonDto>(json);
            }
            catch (Exception exception)
            {
                return ChartParseResult.Failure("JSON 解析失败：" + exception.Message);
            }

            if (dto == null)
            {
                return ChartParseResult.Failure("JSON 根对象不能为空。");
            }

            if (string.IsNullOrWhiteSpace(dto.songId))
            {
                return ChartParseResult.Failure("songId 不能为空。");
            }

            if (dto.bpm <= 0f)
            {
                return ChartParseResult.Failure("bpm 必须大于 0，实际为 " + dto.bpm + "。");
            }

            if (dto.notes == null || dto.notes.Length == 0)
            {
                return ChartParseResult.Failure("notes 不能为空。");
            }

            NoteData[] notes = new NoteData[dto.notes.Length];
            for (int index = 0; index < dto.notes.Length; index++)
            {
                NoteJsonDto note = dto.notes[index];
                if (note.time < 0f)
                {
                    return ChartParseResult.Failure("notes[" + index + "].time 不能为负数。");
                }

                if (index > 0 && note.time < dto.notes[index - 1].time)
                {
                    return ChartParseResult.Failure(
                        "notes[" + index + "].time 不能早于前一个音符。");
                }

                if (note.index != index)
                {
                    return ChartParseResult.Failure(
                        "notes[" + index + "].index 必须为 " + index
                        + "，实际为 " + note.index + "。");
                }

                notes[index] = new NoteData(note.time, note.index);
            }

            return ChartParseResult.Success(new ChartData(dto.songId, dto.bpm, dto.offset, notes));
        }

        [Serializable]
        private sealed class ChartJsonDto
        {
            public string songId;
            public float bpm;
            public float offset;
            public NoteJsonDto[] notes;
        }

        [Serializable]
        private sealed class NoteJsonDto
        {
            public float time;
            public int index;
        }
    }

    /// <summary>
    /// 谱面 JSON 解析结果；成功和失败状态互斥。
    /// </summary>
    public sealed class ChartParseResult
    {
        private ChartParseResult(bool isSuccess, string errorMessage, ChartData data)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Data = data;
        }

        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
        public ChartData Data { get; }

        public static ChartParseResult Success(ChartData data)
        {
            return new ChartParseResult(true, null, data);
        }

        public static ChartParseResult Failure(string errorMessage)
        {
            return new ChartParseResult(false, errorMessage, null);
        }
    }
}
