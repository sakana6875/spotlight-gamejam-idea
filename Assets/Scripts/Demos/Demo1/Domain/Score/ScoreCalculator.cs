using Spotlight.Demos.Demo1.Domain.Rhythm;

namespace Spotlight.Demos.Demo1.Domain.Score
{
    /// <summary>
    /// 纯计分规则：Perfect = 100，Good = 60，Miss = 20（保底分）。
    /// 无连击加成。总分 = 节奏点总数 × 100。
    /// </summary>
    public static class ScoreCalculator
    {
        public const int PerfectScore = 100;
        public const int GoodScore = 60;
        public const int MissScore = 20;

        /// <summary>单个节奏点的得分。</summary>
        public static int CalcNoteScore(JudgeResult result)
        {
            switch (result)
            {
                case JudgeResult.Perfect: return PerfectScore;
                case JudgeResult.Good: return GoodScore;
                case JudgeResult.Miss: return MissScore;
                default: return 0;
            }
        }

        /// <summary>给定节奏点数量，返回该谱面的理论最高分。</summary>
        public static int CalcMaxScore(int noteCount)
        {
            return noteCount * PerfectScore;
        }
    }
}