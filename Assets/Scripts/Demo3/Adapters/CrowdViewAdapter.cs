using System.Collections.Generic;
using Healing.Demo3.Application;
using UnityEngine;
using UnityEngine.Pool;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 人群视图适配：把 CrowdSimulation 的纯数据同步为 NPC 表现体。
    /// 男/女看客是两个独立预制体（各自挂自己的 Animator 与四方向走动动画），
    /// 按模拟层的 Variant 从各自的对象池取用：
    ///  - Get()     取出（自动激活）
    ///  - Release() 归还（自动禁用并回到池内）
    ///  - 池满时超出 maxSize 的实例自动销毁，防止人群失控膨胀
    /// 附带"黑暗吞没"：压迫值超过 darknessStart 后，NPC 整体逐渐透明，
    /// 配合 VisualBinder 的暗角与聚光灯，实现"只剩下小女孩与舞台灯光"。
    /// </summary>
    public sealed class CrowdViewAdapter : MonoBehaviour
    {
        [Header("看客预制体（男/女各一个，各自挂好自己的 Animator）")]
        [SerializeField] private NpcAgentView malePrefab;
        [SerializeField] private NpcAgentView femalePrefab;
        [SerializeField] private int prewarmPerPrefab = 130;         // 每个预制体预热数量，避免演出中途实例化卡顿
        [SerializeField] private int maxPoolSize = 560;              // 池上限（略大于 MaxCrowd）
        [SerializeField, Range(0f, 1f), Tooltip("压迫值超过此值后人群开始隐入黑暗")]
        private float darknessStart = 0.8f;                          // 默认 0.8：前中期保证看客清晰可见

        private ObjectPool<NpcAgentView> malePool;
        private ObjectPool<NpcAgentView> femalePool;
        private readonly Dictionary<int, NpcAgentView> active = new Dictionary<int, NpcAgentView>(256);
        private readonly Dictionary<int, byte> activeVariant = new Dictionary<int, byte>(256); // 归还时找对应的池
        private readonly HashSet<int> seen = new HashSet<int>();
        private readonly List<int> toRelease = new List<int>(32);

        private void Awake()
        {
            // 自检看客预制体的 Animator 参数（只对预制体检查一次，不对池内实例逐个检查）
            ValidatePrefab(malePrefab);
            ValidatePrefab(femalePrefab);

            malePool = MakePool(malePrefab);
            femalePool = MakePool(femalePrefab);

            // 预热：提前实例化，首波人群入场不掉帧
            Prewarm(malePool);
            Prewarm(femalePool);
        }

        private static void ValidatePrefab(NpcAgentView prefab)
        {
            if (prefab == null) return;
            if (prefab.UsesDirectDrive) { prefab.PreflightStates(); return; }
            var anim = prefab.GetComponentInChildren<Animator>();
            Demo3AnimParams.Validate(anim, prefab, needPosture: false, needHandhold: false);
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
                    view = (a.Variant == 0 ? malePool : femalePool).Get(); // 按性别从对应预制体的池取
                    active.Add(a.Id, view);
                    activeVariant.Add(a.Id, a.Variant);
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
                byte variant = activeVariant[id];
                (variant == 0 ? malePool : femalePool).Release(active[id]);
                active.Remove(id);
                activeVariant.Remove(id);
            }
        }

        // ---- 对象池回调 ----

        private ObjectPool<NpcAgentView> MakePool(NpcAgentView prefab)
        {
            if (prefab == null)
                Debug.LogError("[Demo3] CrowdViewAdapter 缺少看客预制体（male/female 两个都要拖）。");
            return new ObjectPool<NpcAgentView>(
                createFunc: () => CreateOne(prefab),
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnPoolDestroy,
                collectionCheck: true,          // 编辑器下检测重复归还等错误用法
                defaultCapacity: prewarmPerPrefab,
                maxSize: maxPoolSize);
        }

        private void Prewarm(ObjectPool<NpcAgentView> pool)
        {
            if (pool == null) return;
            var warm = new List<NpcAgentView>(prewarmPerPrefab);
            for (int i = 0; i < prewarmPerPrefab; i++) warm.Add(pool.Get());
            foreach (var v in warm) pool.Release(v);
        }

        private NpcAgentView CreateOne(NpcAgentView prefab)
        {
            var v = Instantiate(prefab, transform);
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
            malePool?.Dispose();    // 清空池并销毁池内未使用的实例
            femalePool?.Dispose();
        }
    }
}
