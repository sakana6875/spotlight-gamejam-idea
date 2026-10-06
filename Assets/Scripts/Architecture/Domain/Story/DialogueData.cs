using System;

namespace Spotlight.Domain.Story
{
    /// <summary>
    /// 剧情内容的稳定标识；不随场景对象或显示文案变化。
    /// </summary>
    public readonly struct ContentId : IEquatable<ContentId>
    {
        public ContentId(string value)
        {
            Value = value == null ? string.Empty : value.Trim();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ContentId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(ContentId left, ContentId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContentId left, ContentId right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// 永久剧情标记的稳定标识；不持有场景对象或服务实例。
    /// </summary>
    public readonly struct StoryFlag : IEquatable<StoryFlag>
    {
        public StoryFlag(string value)
        {
            Value = value == null ? string.Empty : value.Trim();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(StoryFlag other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is StoryFlag other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(StoryFlag left, StoryFlag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StoryFlag left, StoryFlag right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// 结局稳定标识；本阶段只保留坏结局契约，不触发结局流程。
    /// </summary>
    public enum EndingId
    {
        BadEnding
    }

    /// <summary>
    /// 对话请求的纯数据；移动锁定由外部适配器解释，不直接操作玩家。
    /// </summary>
    public sealed class DialogueRequest
    {
        public DialogueRequest(ContentId contentId, bool locksMovement)
        {
            ContentId = contentId;
            LocksMovement = locksMovement;
        }

        public ContentId ContentId { get; }
        public bool LocksMovement { get; }
    }
}
