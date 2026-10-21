using System.Collections.Generic;

namespace Spotlight.Domain.Story
{
    /// <summary>
    /// 按稳定 ID 索引的剧情图。重复或空白 ID 不能构成图。
    /// </summary>
    public sealed class StoryGraph
    {
        private readonly Dictionary<string, StoryNode> _nodes;
        private readonly Dictionary<string, List<StoryNode>> _choiceGroups;

        private StoryGraph(
            Dictionary<string, StoryNode> nodes,
            Dictionary<string, List<StoryNode>> choiceGroups)
        {
            _nodes = nodes;
            _choiceGroups = choiceGroups;
        }

        public int Count => _nodes.Count;

        public static bool TryCreate(IReadOnlyList<StoryNode> nodes, out StoryGraph graph, out string error)
        {
            graph = null;
            if (nodes == null || nodes.Count == 0)
            {
                error = "剧情图没有任何节点。";
                return false;
            }

            Dictionary<string, StoryNode> byId = new Dictionary<string, StoryNode>();
            Dictionary<string, List<StoryNode>> groups = new Dictionary<string, List<StoryNode>>();
            for (int index = 0; index < nodes.Count; index++)
            {
                StoryNode node = nodes[index];
                if (node == null || string.IsNullOrWhiteSpace(node.Id))
                {
                    error = "剧情图含有空白 ID。";
                    return false;
                }

                if (byId.ContainsKey(node.Id))
                {
                    error = "剧情图 ID 重复：" + node.Id;
                    return false;
                }

                byId.Add(node.Id, node);
                if (node.ChoiceGroup.Length == 0)
                {
                    continue;
                }

                if (!groups.TryGetValue(node.ChoiceGroup, out List<StoryNode> group))
                {
                    group = new List<StoryNode>();
                    groups.Add(node.ChoiceGroup, group);
                }

                group.Add(node);
            }

            graph = new StoryGraph(byId, groups);
            error = null;
            return true;
        }

        public bool TryGet(string id, out StoryNode node)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                node = null;
                return false;
            }

            return _nodes.TryGetValue(id, out node);
        }

        public IReadOnlyList<StoryNode> GetChoiceGroup(string choiceGroup)
        {
            if (string.IsNullOrWhiteSpace(choiceGroup)
                || !_choiceGroups.TryGetValue(choiceGroup, out List<StoryNode> group))
            {
                return new StoryNode[0];
            }

            return group;
        }
    }
}
