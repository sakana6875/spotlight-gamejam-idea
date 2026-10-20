namespace Spotlight.Domain.Story
{
    /// <summary>
    /// 一条可跳转的剧情节点。不持有 Unity 对象。
    /// </summary>
    public sealed class StoryNode
    {
        public StoryNode(
            string id,
            StoryNodeKind kind,
            string[] speakerIds,
            string text,
            string nextId,
            string sceneLabel,
            string failureNextId,
            string choiceGroup)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            SpeakerIds = speakerIds ?? new string[0];
            Text = text ?? string.Empty;
            NextId = nextId ?? string.Empty;
            SceneLabel = sceneLabel ?? string.Empty;
            FailureNextId = failureNextId ?? string.Empty;
            ChoiceGroup = choiceGroup ?? string.Empty;
        }

        public string Id { get; }
        public StoryNodeKind Kind { get; }
        public string[] SpeakerIds { get; }
        public string Text { get; }
        public string NextId { get; }
        public string SceneLabel { get; }
        public string FailureNextId { get; }
        public string ChoiceGroup { get; }
    }
}
