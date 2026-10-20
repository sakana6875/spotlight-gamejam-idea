using System;
using Spotlight.Domain.Story;
using UnityEngine;

namespace Spotlight.Story
{
    /// <summary>
    /// 导入器生成的剧情图。运行时只读这份资产，不解析 CSV。
    /// </summary>
    public sealed class StoryCatalogAsset : ScriptableObject
    {
        [SerializeField] private StoryNodeRecord[] _nodes = Array.Empty<StoryNodeRecord>();

        public int NodeCount => _nodes == null ? 0 : _nodes.Length;

        public bool TryCreateGraph(out StoryGraph graph, out string error)
        {
            if (_nodes == null || _nodes.Length == 0)
            {
                graph = null;
                error = "剧情资产没有任何节点。";
                return false;
            }

            StoryNode[] nodes = new StoryNode[_nodes.Length];
            for (int index = 0; index < _nodes.Length; index++)
            {
                StoryNodeRecord record = _nodes[index];
                nodes[index] = new StoryNode(
                    record.id,
                    record.kind,
                    record.speakerIds,
                    record.text,
                    record.nextId,
                    record.sceneLabel,
                    record.failureNextId,
                    record.choiceGroup);
            }

            return StoryGraph.TryCreate(nodes, out graph, out error);
        }
    }

    /// <summary>
    /// 剧情资产里的一条节点。字段名保持稳定，供导入器写入。
    /// </summary>
    [Serializable]
    public sealed class StoryNodeRecord
    {
        public string id;
        public StoryNodeKind kind;
        public string[] speakerIds;
        public string text;
        public string nextId;
        public string sceneLabel;
        public string failureNextId;
        public string choiceGroup;
    }
}
