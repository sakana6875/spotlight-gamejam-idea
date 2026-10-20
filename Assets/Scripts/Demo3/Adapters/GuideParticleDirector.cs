using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 第四幕引导粒子导演（表现层）：
    /// 看客区域——画面外粒子向大树方向（屏幕上方）飞去；
    /// 浅水沙地——玩家每落一个旧脚印激发一缕粒子上升；
    /// 蘑菇区域——蘑菇自身散发粒子并向大树方向飘。
    /// 实现：对象池小精灵，向目标点漂移 + 上飘，淡出。
    /// </summary>
    public sealed class GuideParticleDirector : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer particlePrefab;
        [SerializeField] private float driftSpeed = 1.6f;
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private float emitInterval = 0.35f;

        private sealed class P { public SpriteRenderer View; public Vector2 Vel; public float T; }
        private ObjectPool<SpriteRenderer> pool;
        private readonly List<P> actives = new List<P>(64);
        private float emitTimer;

        private void Awake()
        {
            pool = new ObjectPool<SpriteRenderer>(
                createFunc: () =>
                {
                    var v = Instantiate(particlePrefab, transform);
                    v.gameObject.SetActive(false);
                    return v;
                },
                actionOnGet: v => v.gameObject.SetActive(true),
                actionOnRelease: v => v.gameObject.SetActive(false),
                actionOnDestroy: v => { if (v != null) Destroy(v.gameObject); },
                collectionCheck: false, defaultCapacity: 32, maxSize: 128);
        }

        /// <summary>在指定位置激发一缕粒子，朝 targetDir（通常是大树方向）飘去</summary>
        public void Burst(Vector2 pos, Vector2 targetDir, int n = 6)
        {
            for (int i = 0; i < n; i++)
            {
                var v = pool.Get();
                v.transform.position = pos + Random.insideUnitCircle * 0.4f;
                Vector2 vel = (targetDir.normalized + Random.insideUnitCircle * 0.35f).normalized
                              * driftSpeed * Random.Range(0.7f, 1.3f);
                actives.Add(new P { View = v, Vel = vel, T = 0f });
            }
        }

        /// <summary>由场景根每帧驱动：持续从画面外缘生成上升粒子</summary>
        public void TickAmbient(float dt, Vector2 emitPos, Vector2 dir)
        {
            emitTimer -= dt;
            if (emitTimer <= 0f) { emitTimer = emitInterval; Burst(emitPos, dir, 2); }
            Tick(dt);
        }

        public void Tick(float dt)
        {
            for (int i = actives.Count - 1; i >= 0; i--)
            {
                var p = actives[i];
                p.T += dt;
                p.View.transform.position += (Vector3)(p.Vel * dt);
                var c = p.View.color; c.a = 1f - p.T / lifetime; p.View.color = c;
                if (p.T >= lifetime) { pool.Release(p.View); actives.RemoveAt(i); }
            }
        }

        private void OnDestroy() => pool?.Dispose();
    }
}
