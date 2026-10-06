using System.Collections.Generic;
using Healing.Demo3.Application;
using UnityEngine;
using UnityEngine.Pool;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 人群视图适配：把 CrowdSimulation 的纯数据同步为 NPC 表现体。
    /// NPC GameObject 的创建/回收使用 Unity 内置对象池 UnityEngine.Pool.ObjectPool：
    ///  - Get()     取出（自动激活）
    ///  - Release() 归还（自动禁用并回到池内）
    ///  - 池满时超出 maxSize 的实例自动销毁，防止人群失控膨胀
    /// 附带"黑暗吞没"：压迫值超过 darknessStart 后，NPC 整体逐渐透明，
    /// 配合 VisualBinder 的暗角与聚光灯，实现"只剩下小女孩与舞台灯光"。
    /// </summary>
    public sealed class CrowdViewAdapter : MonoBehaviour
    {
        [SerializeField] private NpcAgentView npcPrefab;
        [SerializeField] private int prewarm = 260;                  // 预热数量，避免演出中途实例化卡顿
        [SerializeField] private int maxPoolSize = 560; // 略大于 MaxCrowd=520              // 池上限（略大于 MaxCrowd）
        [SerializeField, Range(0f, 1f), Tooltip("压迫值超过此值后人群开始隐入黑暗")]
        private float darknessStart = 0.75f;

        private ObjectPool<NpcAgentView> pool;
        private readonly Dictionary<int, NpcAgentView> active = new Dictionary<int, NpcAgentView>(256);
        private readonly HashSet<int> seen = new HashSet<int>();
        private readonly List<int> toRelease = new List<int>(32);

        private void Awake()
        {
            pool = new ObjectPool<NpcAgentView>(
                createFunc: CreateOne,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnPoolDestroy,
                collectionCheck: true,          // 编辑器下检测重复归还等错误用法
                defaultCapacity: prewarm,
                maxSize: maxPoolSize);

            // 预热：提前实例化，首波人群入场不掉帧
            var warm = new List<NpcAgentView>(prewarm);
            for (int i = 0; i < prewarm; i++) warm.Add(pool.Get());
            foreach (var v in warm) pool.Release(v);
        }

        public void Sync(CrowdSimulation sim, float pressure)
        {
            seen.Clear();
            // smoothstep 化的黑暗吞没曲线
            float d = Mathf.InverseLerp(darknessStart, 1f, pressure);
            float alpha = 1f - d * d * (3f - 2f * d);

            var agents = sim.Agents;
            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i];
                seen.Add(a.Id);
                if (!active.TryGetValue(a.Id, out var view))
                {
                    view = pool.Get();
                    active.Add(a.Id, view);
                }
                view.SetPosition(a.Position);
                view.SetVelocity(a.Velocity);
                view.SetAlpha(alpha);
            }

            toRelease.Clear();
            foreach (var kv in active)
                if (!seen.Contains(kv.Key)) toRelease.Add(kv.Key);
            foreach (int id in toRelease)
            {
                pool.Release(active[id]);
                active.Remove(id);
            }
        }

        // ---- 对象池回调 ----

        private NpcAgentView CreateOne()
        {
            var v = Instantiate(npcPrefab, transform);
            v.gameObject.SetActive(false);
            return v;
        }

        private void OnGet(NpcAgentView v) => v.gameObject.SetActive(true);

        private void OnRelease(NpcAgentView v)
        {
            v.SetAlpha(1f); // 重置透明度，下次取出时不带上一轮的"黑暗"残留
            v.gameObject.SetActive(false);
        }

        private void OnPoolDestroy(NpcAgentView v)
        {
            if (v != null) Destroy(v.gameObject);
        }

        private void OnDestroy()
        {
            pool?.Dispose(); // 清空池并销毁池内未使用的实例
        }
    }
}
