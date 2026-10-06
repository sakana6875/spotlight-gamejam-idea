using System.Collections;
using System.Collections.Generic;
using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 跨场景服务载体（临时组合根，演示用）。
    /// 职责：携带纯数据运行旗标（石碑点亮/旧脚印）、包装 SceneManager 做场景切换、
    ///       持有输入适配器，场景加载完成后找到该幕的场景根并注入服务。
    /// 它是全局架构里 SessionRoot + SceneFlowManager 的临时替身：
    /// 正式接入后删除本类，由 SessionRoot 注入同等接口。
    /// 注意：查重用静态实例引用仅用于服务定位，玩法状态全部在纯数据字段里。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class Demo3FlowCarrier : MonoBehaviour, IDemoSceneFlow, IDemo3RunFlags
    {
        private static Demo3FlowCarrier instance;   // 仅查重/定位，不存玩法状态

        [SerializeField] private Demo3InputAdapter inputAdapter; // 挂在同一载体物体上

        private readonly HashSet<string> litTablets = new HashSet<string>();
        private readonly Dictionary<string, List<TrailMark>> oldMarks = new Dictionary<string, List<TrailMark>>();

        private Demo3LocalServices services;

        public static bool Exists => instance != null;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            services = new Demo3LocalServices(inputAdapter, null);
            services.Flow = this;
            services.Flags = this;
            InjectServicesInto(services);
        }

        // ---- IDemoSceneFlow（适配层，唯一允许碰 SceneManager 的地方）----

        public void GoTo(string sceneId)
        {
            StartCoroutine(LoadRoutine(sceneId));
        }

        private IEnumerator LoadRoutine(string sceneId)
        {
            var op = SceneManager.LoadSceneAsync(sceneId, LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            yield return null; // 等场景根 Start
            InjectServicesInto(services);
        }

        /// <summary>
        /// 组合根行为：找到新场景的场景根并把服务交给它。
        /// （架构中文档禁止的是"模块间互相 FindObjectOfType"；组合根定位场景入口不受此限，
        ///   正式架构中这对应 SceneFlowManager 的"场景加载后传递恢复上下文"。）
        /// </summary>
        private void InjectServicesInto(Demo3LocalServices s)
        {
            var root = FindFirstObjectByType<ActSceneRootBase>();
            if (root != null) root.Configure(s);
        }

        // ---- IDemo3RunFlags（纯数据）----

        public bool IsLit(string tabletId) => litTablets.Contains(tabletId);
        public void MarkLit(string tabletId) => litTablets.Add(tabletId);
        public IReadOnlyList<TrailMark> GetOldMarks(string areaId)
            => oldMarks.TryGetValue(areaId, out var m) ? m : (IReadOnlyList<TrailMark>)System.Array.Empty<TrailMark>();
        public void SaveOldMarks(string areaId, List<TrailMark> marks) => oldMarks[areaId] = marks;

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }

    /// <summary>各幕场景根的基类：统一"注入优先、本地兜底"的接线方式</summary>
    public abstract class ActSceneRootBase : MonoBehaviour
    {
        public abstract void Configure(IDemo3Services services);
    }
}
