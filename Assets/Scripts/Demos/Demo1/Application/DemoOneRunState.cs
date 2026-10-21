using Spotlight.Demos.Demo1.Domain.Rhythm;
using Spotlight.Demos.Demo1.Domain.Score;

namespace Spotlight.Demos.Demo1.Application
{
    /// <summary>
    /// 单局 Demo1 的运行时状态。持有判定计数、得分、note 指针和结束标记。
    /// 由 DemoOneRunController 修改；表现层与 UI 通过只读属性读取。
    /// </summary>
    public sealed class DemoOneRunState
    {
        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int MissCount { get; private set; }

        /// <summary>本局当前得分。</summary>
        public int Score { get; private set; }

        /// <summary>本局理论最高分，构造后不可变。</summary>
        public int MaxScore { get; }

        /// <summary>下一个待判定的 note 下标。等于 MaxScore/100 时表示全部已处理。</summary>
        public int CurrentNoteIndex { get; private set; }

        /// <summary>本局是否已结束。结束后不再接受输入和判定。</summary>
        public bool IsEnded { get; private set; }

        public DemoOneRunState(int maxScore)
        {
            MaxScore = maxScore;
        }

        /// <summary>累计一次判定结果：更新计数并加分。</summary>
        public void ApplyJudge(JudgeResult result)
        {
            switch (result)
            {
                case JudgeResult.Perfect: PerfectCount++; break;
                case JudgeResult.Good: GoodCount++; break;
                case JudgeResult.Miss: MissCount++; break;
            }
            Score += ScoreCalculator.CalcNoteScore(result);
        }

        /// <summary>推进 note 指针。由 controller 在每次判定后调用一次。</summary>
        public void AdvanceNote()
        {
            CurrentNoteIndex++;
        }

        /// <summary>标记本局结束。</summary>
        public void MarkEnded()
        {
            IsEnded = true;
        }

        /// <summary>重置为本局开始的初始状态。MaxScore 保留。</summary>
        public void Reset()
        {
            PerfectCount = 0;
            GoodCount = 0;
            MissCount = 0;
            Score = 0;
            CurrentNoteIndex = 0;
            IsEnded = false;
        }
    }
}