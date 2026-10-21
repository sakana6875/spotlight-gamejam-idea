using System.Collections;
using System.Collections.Generic;
using Healing.Demo3.Application;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 跨场景服务载体（临时组合根，演示用）。
    /// 职责：携带纯数据运行旗标（石碑点亮记录）、包装 SceneManager 做场景切换、
    ///       持有输入适配器，场景加载完成后找到该幕的场景根并注入服务。
    /// 它是全局架构里 SessionRoot + SceneFlowManager 的临时替身：
    /// 正式接入后删除本类，由 SessionRoot 注入同等接口。
    /// 注意：查重用静态实例引用仅用于服务定位，玩法状态全部在纯数据字段里。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class Demo3FlowCarrier : MonoBehaviour, IDemoSceneFlow, IDemo3RunFlags, IDemo3Progress
    {
        private static Demo3FlowCarrier instance;   // 仅查重/定位，不存玩法状态

        [SerializeField] private Demo3InputAdapter inputAdapter; // 挂在同一载体物体上

        private readonly HashSet<string> litTablets = new HashSet<string>();
        // ---- 全局进度（纯数据）：场景进入计数 + 隐藏区域访问记录，结局分支/场景变体用 ----
        private readonly Dictionary<string, int> sceneEntries = new Dictionary<string, int>();
        private readonly HashSet<string> hiddenVisited = new HashSet<string>();
        // ---- 隐藏区返回点（纯数据）：离开去隐藏区时记下站位与行走范围，返回时原地复活 ----
        private readonly Dictionary<string, Vector2> returnPoints = new Dictionary<string, Vector2>();
        private readonly Dictionary<string, Rect> returnAreas = new Dictionary<string, Rect>();

        private Demo3LocalServices services;

        public static bool Exists => instance != null;

        /// <summary>组合根服务定位：传送门等纯适配器拿场景切换端口的唯一入口（对应正式架构的 SceneFlowManager 定位）。</summary>
        public static IDemoSceneFlow LocateFlow() => instance;

        /// <summary>当前跨幕载体。组合根只在 Act1 加载后用它换结果上报。</summary>
        public static Demo3FlowCarrier Current => instance;

        /// <summary>
        /// 换成剧情侧的结果上报。必须在进入后几幕之前调用，后面的场景根拿的是同一份服务。
        /// </summary>
        public void UseRunReporter(IDemoRun run)
        {
            services?.UseRunReporter(run);
        }

        /// <summary>
        /// 离开 Demo3 时拆掉载体。留着会挡住下一次进入，也会把输入带到 Hub。
        /// </summary>
        public static void Shutdown()
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            if (inputAdapter == null)
                Debug.LogError("[Demo3] Demo3FlowCarrier 的 Input Adapter 字段为空" +
                               "——把挂了 Demo3InputAdapter（且拖好 Move/Jump/Interact 动作）的物体拖进来，" +
                               "否则进入任何一幕都无法操作。", this);
            // 初始场景（直接 Play 进来的那一幕）也计入进入次数
            CurrentSceneId = SceneManager.GetActiveScene().name;
            sceneEntries[CurrentSceneId] = 1;
            services = new Demo3LocalServices(inputAdapter, null);
            services.Flow = this;
            services.Flags = this;
            services.Progress = this;
            InjectServicesInto(services);
        }

        // ---- IDemoSceneFlow（适配层，唯一允许碰 SceneManager 的地方）----

        public void GoTo(string sceneId)
        {
            StartCoroutine(LoadRoutine(sceneId));
        }

        private IEnumerator LoadRoutine(string sceneId)
        {
            // 换场先记账：来源场景 + 目标场景进入次数（场景根随后按计数切换变体/区分幕次）
            LastSceneId = CurrentSceneId;
            CurrentSceneId = sceneId;
            sceneEntries[sceneId] = EntryCount(sceneId) + 1;
            Debug.Log($"[Demo3] 全局进度：进入 {sceneId}（第 {sceneEntries[sceneId]} 次），来源 {LastSceneId ?? "无"}");

            var op = SceneManager.LoadSceneAsync(sceneId, LoadSceneMode.Single);
            if (op == null)
            {
                // 场景名拼写错误或未加入 Build Settings 时 LoadSceneAsync 返回 null，
                // 不能继续轮询（否则空引用），给出可操作的错误信息后中止切换。
                Debug.LogError($"[Demo3] 场景 '{sceneId}' 加载失败：请检查场景文件名是否逐字一致，" +
                               "且已在 File → Build Settings 中加入（当前应为 Scenes/Demo3/Act1、Act2、Act2-1、Act2-2、Act3、Act4、Hidden1、Hidden2）。");
                yield break;
            }
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
            // 场景根没跑起来时整幕会静默卡死（女孩不动、不钳制、相机不跟），在这里一次性点破：
            // 找不到激活的场景根（物体未激活/没挂根），或找到了但组件被勾掉（禁用）。
            if (root == null)
            {
                if (FindFirstObjectByType<Demo3SceneRoot>() != null) return; // 第一幕是自含根（本地服务），不算缺失
                Debug.LogError($"[Demo3] 场景 {SceneManager.GetActiveScene().name} 里没有激活的场景根" +
                               "（Act1Root/Act2Root/Act3Root/Act3TreeRoot/HiddenAreaRoot 之一）——本幕不会运行。" +
                               "请确认场景里挂了场景根的物体处于激活状态（Inspector 左上角勾上）。");
                return;
            }
            if (!root.enabled)
                Debug.LogError($"[Demo3] 场景根 {root.name}（{root.GetType().Name}）的组件被禁用了——本幕不会运行。" +
                               "请在 Inspector 里勾上该组件左边的勾。", root);
            root.Configure(s);
        }

        // ---- IDemo3RunFlags（纯数据）----

        public bool IsLit(string tabletId) => litTablets.Contains(tabletId);
        public void MarkLit(string tabletId) => litTablets.Add(tabletId);

        // ---- IDemo3Progress（纯数据：结局分支与场景变体的全局参数）----

        public string CurrentSceneId { get; private set; }
        public string LastSceneId { get; private set; }
        public int EntryCount(string sceneId) => sceneEntries.TryGetValue(sceneId, out var n) ? n : 0;
        public bool HasVisitedHidden(string hiddenId) => hiddenVisited.Contains(hiddenId);
        public void MarkHiddenVisited(string hiddenId)
        {
            if (hiddenVisited.Add(hiddenId))
                Debug.Log($"[Demo3] 全局进度：已记录进入过隐藏区域 {hiddenId}（结局分支用）");
        }

        public void SetReturnPoint(string sceneId, Vector2 pos, Rect walkArea)
        {
            returnPoints[sceneId] = pos;
            returnAreas[sceneId] = walkArea;
            Debug.Log($"[Demo3] 全局进度：已记录 {sceneId} 的返回点 {pos}（从隐藏区回来时原地复活）");
        }

        public bool TryConsumeReturnPoint(string sceneId, out Vector2 pos, out Rect walkArea)
        {
            if (returnPoints.TryGetValue(sceneId, out pos))
            {
                returnPoints.Remove(sceneId);
                returnAreas.TryGetValue(sceneId, out walkArea);
                returnAreas.Remove(sceneId);
                return true;
            }
            walkArea = default;
            return false;
        }

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
