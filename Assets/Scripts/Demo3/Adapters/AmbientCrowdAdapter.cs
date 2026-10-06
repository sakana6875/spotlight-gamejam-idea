using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 第三幕的无视看客（纯表现）：原地站立或微微挪动，不回避、不追逐，
    /// 女孩从他们之间穿过。少量 NPC 使用"像男孩"的背影变体（外形有区别）。
    /// 没有任何玩法规则，因此不需要 Domain/Session。
    /// </summary>
    public sealed class AmbientCrowdAdapter : MonoBehaviour
    {
        [SerializeField] private NpcAgentView npcPrefab;
        [SerializeField] private int count = 160;
        [SerializeField] private Rect area;
        [SerializeField] private float driftSpeed = 0.15f;   // 极慢地挪动
        [SerializeField] private Sprite[] lookalikeSprites;  // "像男孩"的背影变体（有区别）
        [SerializeField, Range(0f, 0.2f)] private float lookalikeRatio = 0.05f;

        private ObjectPool<NpcAgentView> pool;
        private readonly List<Vector2> driftTargets = new List<Vector2>();
        private readonly List<NpcAgentView> views = new List<NpcAgentView>();
        private bool spawned;

        private void Awake()
        {
            pool = new ObjectPool<NpcAgentView>(
                createFunc: () =>
                {
                    var v = Instantiate(npcPrefab, transform);
                    v.gameObject.SetActive(false);
                    return v;
                },
                actionOnGet: v => v.gameObject.SetActive(true),
                actionOnRelease: v => v.gameObject.SetActive(false),
                actionOnDestroy: v => { if (v != null) Destroy(v.gameObject); },
                collectionCheck: false, defaultCapacity: count, maxSize: count + 16);
        }

        public void EnsureSpawned()
        {
            if (spawned) return;
            spawned = true;
            for (int i = 0; i < count; i++)
            {
                var v = pool.Get();
                v.SetPosition(RandomPointIn());
                views.Add(v);
                driftTargets.Add(RandomPointIn());
                if (lookalikeSprites != null && lookalikeSprites.Length > 0
                    && Random.value < lookalikeRatio)
                {
                    var sr = v.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) sr.sprite = lookalikeSprites[Random.Range(0, lookalikeSprites.Length)];
                }
            }
        }

        /// <summary>由场景根每帧驱动（保持适配层模式一致，不用自己的 Update）</summary>
        public void Tick(float dt)
        {
            EnsureSpawned();
            for (int i = 0; i < views.Count; i++)
            {
                Vector2 pos = views[i].transform.position;
                if ((driftTargets[i] - pos).sqrMagnitude < 0.02f)
                    driftTargets[i] = RandomPointIn();
                else
                    views[i].SetPosition(Vector2.MoveTowards(pos, driftTargets[i], driftSpeed * dt));
            }
        }

        private Vector2 RandomPointIn() =>
            new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));

        private void OnDestroy() => pool?.Dispose();
    }
}
