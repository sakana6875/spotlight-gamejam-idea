using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 第三幕的无视看客（纯表现）：原地站立或微微挪动，不回避、不追逐，
    /// 女孩从他们之间穿过。男/女看客为两个独立预制体（各自挂自己的 Animator）；
    /// 少量 NPC 使用"像男孩"的背影变体（外形有区别）。
    /// 没有任何玩法规则，因此不需要 Domain/Session。
    /// </summary>
    public sealed class AmbientCrowdAdapter : MonoBehaviour
    {
        [SerializeField] private NpcAgentView malePrefab;    // 男看客预制体
        [SerializeField] private NpcAgentView femalePrefab;  // 女看客预制体
        [SerializeField] private int count = 160;
        [Tooltip("人群分布区域：拖一个带 Collider2D 的空物体（用其包围盒随机取点）")]
        [SerializeField] private Collider2D area;
        [SerializeField] private float driftSpeed = 0.15f;   // 极慢地挪动
        [SerializeField] private Sprite[] lookalikeSprites;  // "像男孩"的背影变体（有区别）
        [SerializeField, Range(0f, 0.2f)] private float lookalikeRatio = 0.05f;

        private ObjectPool<NpcAgentView> malePool;
        private ObjectPool<NpcAgentView> femalePool;
        private readonly List<Vector2> driftTargets = new List<Vector2>();
        private readonly List<NpcAgentView> views = new List<NpcAgentView>();
        private bool spawned;
        private bool vanished;   // 人群已消失（看客区演出：消失后不再生成）

        private void Awake()
        {
            malePool = MakePool(malePrefab);
            femalePool = MakePool(femalePrefab);
        }

        private ObjectPool<NpcAgentView> MakePool(NpcAgentView prefab)
        {
            if (prefab == null)
                Debug.LogError("[Demo3] AmbientCrowdAdapter 缺少看客预制体（male/female 两个都要拖）。");
            return new ObjectPool<NpcAgentView>(
                createFunc: () =>
                {
                    var v = Instantiate(prefab, transform);
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
            if (spawned || vanished) return;
            if (area == null)
            {
                Debug.LogError("[Demo3] AmbientCrowdAdapter 的 Area 未拖碰撞体：人群没有分布区域，不生成。", this);
                spawned = true; // 只报一次
                return;
            }
            spawned = true;
            for (int i = 0; i < count; i++)
            {
                var v = (Random.value < 0.5f ? malePool : femalePool).Get(); // 男女各半
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
            if (vanished) return;
            EnsureSpawned();
            for (int i = 0; i < views.Count; i++)
            {
                Vector2 pos = views[i].transform.position;
                if ((driftTargets[i] - pos).sqrMagnitude < 0.02f)
                {
                    driftTargets[i] = RandomPointIn();
                    views[i].SetVelocity(Vector2.zero); // 到位 → Idle
                }
                else
                {
                    Vector2 next = Vector2.MoveTowards(pos, driftTargets[i], driftSpeed * dt);
                    views[i].SetVelocity((next - pos) / Mathf.Max(1e-5f, dt)); // 喂速度 → 走动向
                    views[i].SetPosition(next);
                }
            }
        }

        private Vector2 RandomPointIn()
        {
            Bounds b = area.bounds;
            return new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));
        }

        /// <summary>看客区演出：人群全部消失（场景根在渐黑全屏时调用；消失后不再生成）</summary>
        public void VanishAll()
        {
            vanished = true;
            foreach (var v in views)
                if (v != null) v.gameObject.SetActive(false);
            views.Clear();
            driftTargets.Clear();
        }

        private void OnDestroy()
        {
            malePool?.Dispose();
            femalePool?.Dispose();
        }
    }
}
