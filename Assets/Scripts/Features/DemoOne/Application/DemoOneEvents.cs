using Spotlight.Features.DemoOne.Domain.Rhythm;

namespace Spotlight.Features.DemoOne.Application
{
    /// <summary>
    /// 单个节奏点判定完成事件。由 DemoOneRunController 发布，
    /// 表现层（泉水动画、判定线闪光、UI 判定弹字）订阅后各自处理。
    /// </summary>
    public readonly struct NoteJudgedEvent
    {
        /// <summary>节奏点在谱面中的序号。</summary>
        public readonly int NoteIndex;

        /// <summary>判定结果。</summary>
        public readonly JudgeResult Result;

        /// <summary>判定发生时的歌曲时间（秒），用于表现层校对时序。</summary>
        public readonly float SongTimeSeconds;

        public NoteJudgedEvent(int noteIndex, JudgeResult result, float songTimeSeconds)
        {
            NoteIndex = noteIndex;
            Result = result;
            SongTimeSeconds = songTimeSeconds;
        }
    }

    /// <summary>
    /// 关卡结束事件。由 DemoOneRunController 在音乐结尾缓冲后发布，
    /// UI 订阅后显示结算面板。此事件发布后 State.IsEnded 为 true，
    /// 本局不再接受新的输入和判定。
    /// </summary>
    public readonly struct DemoOneCompletedEvent
    {
        /// <summary>本次达到的最终高度（m）。</summary>
        public readonly int FinalHeight;

        /// <summary>完美通关高度（m），用于结算面板对比展示。</summary>
        public readonly int PerfectHeight;

        /// <summary>本局得分。</summary>
        public readonly int Score;

        /// <summary>本局理论最高分。</summary>
        public readonly int MaxScore;

        public readonly int PerfectCount;
        public readonly int GoodCount;
        public readonly int MissCount;

        public DemoOneCompletedEvent(
            int finalHeight,
            int perfectHeight,
            int score,
            int maxScore,
            int perfectCount,
            int goodCount,
            int missCount)
        {
            FinalHeight = finalHeight;
            PerfectHeight = perfectHeight;
            Score = score;
            MaxScore = maxScore;
            PerfectCount = perfectCount;
            GoodCount = goodCount;
            MissCount = missCount;
        }
    }
}