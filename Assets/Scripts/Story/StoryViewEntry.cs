using System;
using UnityEngine;

namespace Spotlight.Story
{
    /// <summary>
    /// 场景里的剧情绑定点。UI 在 Start 读取 Host，因为组合根注入晚于 Awake。
    /// </summary>
    public sealed class StoryViewEntry : MonoBehaviour
    {
        public StorySessionHost Host { get; private set; }

        public event Action<StorySessionHost> Bound;

        public void Bind(StorySessionHost host)
        {
            Host = host;
            Bound?.Invoke(host);
        }
    }
}
