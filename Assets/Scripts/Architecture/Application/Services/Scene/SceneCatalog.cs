using System;
using System.Collections.Generic;

namespace Spotlight.Application.Services.Scene
{
    /// <summary>
    /// 集中维护稳定场景标识与 Unity 场景名的映射，不调用 Unity API。
    /// </summary>
    public sealed class SceneCatalog
    {
        private readonly Dictionary<SceneId, SceneDefinition> _definitions;

        public SceneCatalog(IEnumerable<SceneDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _definitions = new Dictionary<SceneId, SceneDefinition>();
            foreach (SceneDefinition definition in definitions)
            {
                if (string.IsNullOrWhiteSpace(definition.UnitySceneName))
                {
                    throw new ArgumentException("场景定义必须包含 Unity 场景名。", nameof(definitions));
                }

                if (_definitions.ContainsKey(definition.SceneId))
                {
                    throw new ArgumentException("场景 ID 不能重复。", nameof(definitions));
                }

                _definitions.Add(definition.SceneId, definition);
            }
        }

        public static SceneCatalog CreateDefault()
        {
            return new SceneCatalog(new[]
            {
                new SceneDefinition(SceneId.SampleScene, "SampleScene", true),
                new SceneDefinition(SceneId.Bootstrap, "Bootstrap", false),
                new SceneDefinition(SceneId.Menu, "Menu", false),
                new SceneDefinition(SceneId.Hub, "Hub", false),
                new SceneDefinition(SceneId.Demo1, "Demo1", false),
                new SceneDefinition(SceneId.Demo2, "Demo2", false),
                new SceneDefinition(SceneId.Demo3, "Demo3", false),
                new SceneDefinition(SceneId.ChaosDemo, "ChaosDemo", false)
            });
        }

        public bool TryGet(SceneId sceneId, out SceneDefinition definition)
        {
            return _definitions.TryGetValue(sceneId, out definition);
        }

        public bool TryGetSceneId(DemoId demoId, out SceneId sceneId)
        {
            switch (demoId)
            {
                case DemoId.Demo1:
                    sceneId = SceneId.Demo1;
                    return true;
                case DemoId.Demo2:
                    sceneId = SceneId.Demo2;
                    return true;
                case DemoId.Demo3:
                    sceneId = SceneId.Demo3;
                    return true;
                case DemoId.ChaosDemo:
                    sceneId = SceneId.ChaosDemo;
                    return true;
                default:
                    sceneId = default;
                    return false;
            }
        }
    }

    /// <summary>
    /// 一个稳定场景 ID 的不可变映射定义。
    /// </summary>
    public sealed class SceneDefinition
    {
        public SceneDefinition(SceneId sceneId, string unitySceneName, bool isAvailable)
        {
            SceneId = sceneId;
            UnitySceneName = unitySceneName;
            IsAvailable = isAvailable;
        }

        public SceneId SceneId { get; }
        public string UnitySceneName { get; }
        public bool IsAvailable { get; }
    }
}
