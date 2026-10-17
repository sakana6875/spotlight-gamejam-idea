using System;

namespace Spotlight.Demos.Demo1.Domain.Rhythm
{
    /// <summary>
    /// 纯判定引擎：给定玩家输入的时间偏差，输出判定结果。
    /// 不负责推进 note 指针、不负责自动超时，这两件事由调用方（DemoOneRunController）负责。
    /// </summary>
    public sealed class RhythmJudgeEngine
    {
        private readonly JudgeWindow _window;

        public RhythmJudgeEngine(JudgeWindow window)
        {
            _window = window;
        }

        /// <summary>
        /// 根据输入时间与 note 目标时间的偏差给出判定结果。
        /// </summary>
        /// <param name="deltaSeconds">玩家输入时间 - note 目标时间（秒）。</param>
        public JudgeResult Judge(float deltaSeconds)
        {
            float abs = Math.Abs(deltaSeconds);
            if (abs <= _window.PerfectSeconds)
            {
                return JudgeResult.Perfect;
            }
            if (abs <= _window.GoodSeconds)
            {
                return JudgeResult.Good;
            }
            return JudgeResult.Miss;
        }

        /// <summary>
        /// 判断某个 note 是否已超过 Good 窗口，用于触发自动 Miss。
        /// </summary>
        /// <param name="songTimeSeconds">当前歌曲时间（秒）。</param>
        /// <param name="noteTimeSeconds">note 的目标时间（秒）。</param>
        public bool IsExpired(float songTimeSeconds, float noteTimeSeconds)
        {
            return songTimeSeconds - noteTimeSeconds > _window.GoodSeconds;
        }

        /// <summary>
        /// 判断输入是否早于 Good 窗口左边界，即"按太早"。
        /// 玩法层用它来忽略太早的输入，避免玩家误触浪费后续 note。
        /// </summary>
        /// <param name="deltaSeconds">玩家输入时间 - note 目标时间（秒）。</param>
        public bool IsTooEarly(float deltaSeconds)
        {
            return deltaSeconds < -_window.GoodSeconds;
        }
    }
}