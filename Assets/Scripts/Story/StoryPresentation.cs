using System;
using Spotlight.Domain.Story;
using UnityEngine;

namespace Spotlight.Story
{
    /// <summary>
    /// 给 UI 读取的当前画面。不包含按钮或字体。
    /// </summary>
    public sealed class StoryPresentation
    {
        public StoryPresentation(
            StoryWaitKind kind,
            string nodeId,
            string[] speakerIds,
            string[] speakerNames,
            Sprite[] portraits,
            string text,
            StoryChoice[] choices,
            string eventId,
            string sceneLabel,
            string blockReason,
            int demo1FinalHeight)
        {
            Kind = kind;
            NodeId = nodeId ?? string.Empty;
            SpeakerIds = speakerIds ?? Array.Empty<string>();
            SpeakerNames = speakerNames ?? Array.Empty<string>();
            Portraits = portraits ?? Array.Empty<Sprite>();
            Text = text ?? string.Empty;
            Choices = choices ?? Array.Empty<StoryChoice>();
            EventId = eventId ?? string.Empty;
            SceneLabel = sceneLabel ?? string.Empty;
            BlockReason = blockReason ?? string.Empty;
            Demo1FinalHeight = demo1FinalHeight;
        }

        public StoryWaitKind Kind { get; }
        public string NodeId { get; }
        public string[] SpeakerIds { get; }
        public string[] SpeakerNames { get; }
        public Sprite[] Portraits { get; }
        public string Text { get; }
        public StoryChoice[] Choices { get; }
        public string EventId { get; }
        public string SceneLabel { get; }
        public string BlockReason { get; }
        public int Demo1FinalHeight { get; }
    }

    /// <summary>
    /// 一个可点选的剧情选项。
    /// </summary>
    public readonly struct StoryChoice
    {
        public StoryChoice(string nodeId, string text)
        {
            NodeId = nodeId ?? string.Empty;
            Text = text ?? string.Empty;
        }

        public string NodeId { get; }
        public string Text { get; }
    }
}
