using System.Collections;
using System.Collections.Generic;
using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 第四幕「重演」场景根：从看客区域走向大树，迎接结局。
    /// 引导粒子按区域激发；C 隐藏路线开放（M5→C1→C2→飞升 M3）；
    /// 到达大树区域 → 会话判定结局 → EndingDirector 演出 → 上报 Demo 结果。
    /// </summary>
    public sealed class Act4Root : ActSceneRootBase
    {
        [Header("角色")]
        [SerializeField] private WalkerViewAdapter girl;

        [Header("场景")]
        [SerializeField] private ActCameraRig cameraRig;
        [SerializeField] private Rect walkBounds = new Rect(-6f, -200f, 12f, 210f);
        [SerializeField] private Rect treeZone;                    // 大树触发区
        [SerializeField] private Transform treePosition;           // 引导粒子的目标方向

        [Header("蘑菇与跳跃")]
        [SerializeField] private List<MushroomPlatformView> mushrooms = new List<MushroomPlatformView>();
        [SerializeField] private List<JumpZoneData> jumpZones = new List<JumpZoneData>(); // 含 C1->C2、C2->M3 飞升
        [SerializeField] private float jumpApexVisual = 1.2f;

        [Header("引导粒子与结局")]
        [SerializeField] private GuideParticleDirector guideParticles;
        [SerializeField] private EndingDirector endingDirector;

        private IDemo3Services services;
        private Act4Session session;

        public override void Configure(IDemo3Services s)
        {
            services = s;
            services.Bus.Subscribe<JumpLandedEvent>(OnGirlLanded);
            services.Bus.Subscribe<EndingDecidedEvent>(OnEndingDecided);
        }

        private void Start() => StartCoroutine(InitWhenReady());

        private IEnumerator InitWhenReady()
        {
            // 载体的加载协程会在场景激活后一两个帧才注入服务，这里等注入再初始化
            float waited = 0f;
            while (services == null && waited < 3f) { waited += Time.unscaledDeltaTime; yield return null; }
            if (services == null)
            {
                Debug.LogError("[Demo3] Act4 需要 Demo3FlowCarrier 在场（从第一幕进入，或在该场景放一个）。");
                enabled = false;
                yield break;
            }

            session = new Act4Session(girl.transform.position, services.Bus, services.Flags, services.Run);
            session.SetJumpZones(jumpZones);
            session.SetTreeZone(treeZone);
            session.Player.Bounds = walkBounds;

            girl.Bind(session.Player.Position);

            // 第四幕：之前点亮的蘑菇仍亮着；未点亮的保持微弱轮廓（女孩视角）
            foreach (var m in mushrooms)
                m.SetVisibility(m.IsLit ? MushroomPlatformView.Visibility.Lit
                                        : MushroomPlatformView.Visibility.Faint);

            services.Input.JumpPressed += OnJumpPressed;
        }

        private void OnJumpPressed() => session?.TryBeginJump();

        private void OnGirlLanded(JumpLandedEvent e)
        {
            if (!e.IsGirl) return;
            foreach (var m in mushrooms)
                if (((Vector2)m.transform.position - e.Pos).sqrMagnitude <= 4f)
                    m.OnLanded(isGirl: true); // 再踩仍会亮一下
        }

        private void OnEndingDecided(EndingDecidedEvent e)
        {
            endingDirector.Play(e.Ending, () => session.NotifyEndingFinished());
        }

        private void Update()
        {
            if (session == null) return;
            float dt = Time.deltaTime;
            session.Tick(dt, services.Input.Move, cameraRig.CameraBottomY);

            if (session.Jumping)
            {
                Vector2 p = JumpTrajectory.Eval(session.JumpFrom, session.JumpTo, session.JumpT, out float h);
                girl.Apply(p, Vector2.zero);
                girl.SetVisualLift(h * jumpApexVisual);
            }
            else
            {
                girl.SetVisualLift(0f);
                girl.Apply(session.Player.Position, session.Player.Velocity);
            }

            // 环境引导粒子：玩家所在区域内持续激发
            if (guideParticles != null && treePosition != null)
            {
                Vector2 dir = (treePosition.position - girl.transform.position).normalized;
                guideParticles.TickAmbient(dt, AmbientEmitPos(), dir);
            }

            cameraRig.Apply(session.Player.Position);
        }

        private Vector2 AmbientEmitPos()
        {
            // 画面外下缘随机取点，粒子向大树飘
            Vector2 gp = session.Player.Position;
            return gp + new Vector2(Random.Range(-6f, 6f), -6f);
        }

        private void OnDestroy()
        {
            if (services == null) return;
            services.Input.JumpPressed -= OnJumpPressed;
            services.Bus.Unsubscribe<JumpLandedEvent>(OnGirlLanded);
            services.Bus.Unsubscribe<EndingDecidedEvent>(OnEndingDecided);
        }
    }
}
