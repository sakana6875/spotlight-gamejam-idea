using System;
using System.Collections.Generic;
using UnityEngine;

namespace Healing.Demo3.Application
{
    /// <summary>
    /// 区域名称触发区（纯数据，可序列化，供场景根在 Inspector 配置）。
    /// 需求：进入该区域时触发（正向/反向进入都算），看客区域不配置即可。
    /// </summary>
    [Serializable]
    public class AreaNameZone
    {
        public string Name;
        public Rect Area;

        public AreaNameZone(string name, Rect area) { Name = name; Area = area; }
    }

    /// <summary>
    /// 区域名播报（应用层纯逻辑）：
    /// 玩家坐标进入触发区时发一次 Entered；留在区内不重复触发；离开后再进入会再次触发。
    /// 表现（纵向显示、2 秒后消失）由适配层 AreaNameBannerView 负责。
    /// </summary>
    public sealed class AreaNameAnnouncer
    {
        private readonly IReadOnlyList<AreaNameZone> zones;
        private readonly bool[] inside;

        public event Action<string> Entered;

        public AreaNameAnnouncer(IReadOnlyList<AreaNameZone> zones)
        {
            this.zones = zones ?? (IReadOnlyList<AreaNameZone>)Array.Empty<AreaNameZone>();
            inside = new bool[this.zones.Count];
        }

        public void Tick(Vector2 playerPos)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                bool now = zones[i].Area.Contains(playerPos);
                if (now && !inside[i]) Entered?.Invoke(zones[i].Name);
                inside[i] = now;
            }
        }
    }
}
