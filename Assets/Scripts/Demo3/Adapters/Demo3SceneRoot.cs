using Healing.Demo3.Application;
using Healing.Demo3.Data;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// Demo3 场景入口（场景级接线点）。
    /// 独立演示：Start 中自动用场景内的 Demo3InputAdapter / Demo3AmbienceAudioAdapter
    ///           组装本地服务，直接打开 Demo3 场景按 Play 即可运行。
    /// 正式流程：全局架构就绪后，由 SessionRoot 在场景加载后调用 Configure(services)
    ///           注入真实服务，本地组件自动被旁路。
    /// 本类只做"接线与每帧驱动"，不包含玩法规则。
    /// </summary>
    public sealed class Demo3SceneRoot : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private Demo3Tuning tuning;

        [Header("场景内引用（同场景序列化，不跨场景）")]
        [SerializeField] private GirlViewAdapter girl;
        [SerializeField] private CrowdViewAdapter crowd;
        [SerializeField] private Demo3VisualBinder visuals;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float viewExpandMargin = 3f; // 模拟范围比视野外扩，保证边沿游荡生效

        [Header("输入与音频（场景内适配器）")]
        [SerializeField] private Demo3InputAdapter inputAdapter;
        [SerializeField] private Demo3AmbienceAudioAdapter ambienceAudio; // 可选，未接则静音

        [Header("演示衔接（可选）：跪倒演出结束后切到第二幕")]
        [SerializeField] private Demo3FlowCarrier flowCarrier; // 场景内引用；留空则只上报结果
        [SerializeField] private string nextSceneId = Demo3SceneIds.Act2Rescue;

        private IDemo3Services services;
        private Demo3Session session;
        private bool handedOff;

        /// <summary>由 SessionRoot（唯一组合根）在场景加载后调用；未调用则走本地模式</summary>
        public void Configure(IDemo3Services s) => services = s;

        private void Start()
        {
            if (services == null)
            {
                if (inputAdapter == null)
                {
                    Debug.LogError("[Demo3] 未指定 Demo3InputAdapter，无法读取输入。");
                    enabled = false;
                    return;
                }
                services = new Demo3LocalServices(inputAdapter, ambienceAudio);
                Debug.Log("[Demo3] 本地模式运行（输入/音频来自场景组件，结果打印到 Console）。");
            }

            session = new Demo3Session(tuning.ToData(), services.Audio, services.Run, services.Bus);
            girl.Bind(session);
        }

        private void Update()
        {
            if (session == null) return;

            GetViewBounds(out Vector2 min, out Vector2 max);
            session.Tick(Time.deltaTime, services.Input.Move, min, max);

            girl.Apply();
            crowd.Sync(session.Sim, session.Pressure);
            visuals.Apply(session);

            // 第一幕已完结 → 通过场景切换端口进入第二幕（Gameplay 不碰 SceneManager）
            if (!handedOff && flowCarrier != null && session.Stage == Demo3Stage.Finished)
            {
                handedOff = true;
                flowCarrier.GoTo(nextSceneId);
            }
        }

        private void GetViewBounds(out Vector2 min, out Vector2 max)
        {
            float halfH = targetCamera.orthographicSize + viewExpandMargin;
            float halfW = halfH * targetCamera.aspect;
            Vector2 c = targetCamera.transform.position;
            min = c + new Vector2(-halfW, -halfH);
            max = c + new Vector2(halfW, halfH);
        }

        /// <summary>暂停界面"放弃本幕"按钮调用（UI → 接口调用，不改内部字段）</summary>
        public void RequestAbandon() => session?.Abandon();

        private void OnDestroy() => session?.Dispose();
    }
}
