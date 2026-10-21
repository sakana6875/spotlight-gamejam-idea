using System;

namespace Spotlight.Domain.Story
{
    /// <summary>
    /// 旁白逐字显示。未打完时确认只补全当前句，打完后的下一次确认才允许推进。
    /// </summary>
    public sealed class StoryTypewriter
    {
        private float _fraction;

        /// <summary>
        /// 当前句全文。
        /// </summary>
        public string FullText { get; private set; } = string.Empty;

        /// <summary>
        /// 已经可以显示的字符数。
        /// </summary>
        public int VisibleCount { get; private set; }

        /// <summary>
        /// 当前句是否已经全部显示。
        /// </summary>
        public bool IsComplete => VisibleCount >= FullText.Length;

        /// <summary>
        /// 按当前可见长度截出的文本。
        /// </summary>
        public string VisibleText => FullText.Substring(0, VisibleCount);

        /// <summary>
        /// 开始一句新旁白。空文本视为已经打完，避免卡住没有正文的句子。
        /// </summary>
        public void Restart(string text)
        {
            FullText = text ?? string.Empty;
            VisibleCount = 0;
            _fraction = 0f;
        }

        /// <summary>
        /// 按未缩放时间推进可见字符。非正速度或非正时间不推进，避免一帧跳完或倒退。
        /// </summary>
        public void Reveal(float deltaSeconds, float charactersPerSecond)
        {
            if (IsComplete || deltaSeconds <= 0f || charactersPerSecond <= 0f)
            {
                return;
            }

            _fraction += deltaSeconds * charactersPerSecond;
            int wholeCharacters = (int)_fraction;
            if (wholeCharacters <= 0)
            {
                return;
            }

            _fraction -= wholeCharacters;
            VisibleCount = Math.Min(FullText.Length, VisibleCount + wholeCharacters);
            if (IsComplete)
            {
                _fraction = 0f;
            }
        }

        /// <summary>
        /// 未打完时只补全并返回 false。已经打完时返回 true，调用方才能进入下一句。
        /// </summary>
        public bool Confirm()
        {
            if (!IsComplete)
            {
                VisibleCount = FullText.Length;
                _fraction = 0f;
                return false;
            }

            return true;
        }
    }
}
