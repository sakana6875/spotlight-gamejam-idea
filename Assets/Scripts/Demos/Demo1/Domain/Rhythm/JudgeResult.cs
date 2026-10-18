namespace Spotlight.Demos.Demo1.Domain.Rhythm
{
    /// <summary>单个节奏点的判定结果。</summary>
    public enum JudgeResult
    {
        /// <summary>完美，判定窗口内偏差最小。</summary>
        Perfect = 0,

        /// <summary>良好，判定窗口内偏差较大。</summary>
        Good = 1,

        /// <summary>未命中，包括玩家未输入导致超时，以及输入偏差超出 Good 窗口。</summary>
        Miss = 2
    }
}