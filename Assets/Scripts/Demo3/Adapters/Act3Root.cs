using System.Collections;
using System.Collections.Generic;
using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 第三幕「追寻」场景根：反向走回看客区。
    /// 女孩视角：未点亮蘑菇显微弱轮廓；B 隐藏路线与 L1 石碑交互开放；
    /// 旧脚印淡显、新脚印清晰；看客无视（AmbientCrowdAdapter）；迷失晕倒 → 第四幕。
    /// </summary>
    public sealed class Act3Root : ActSceneRootBase
    {
        [Header("角色")]
        [SerializeField] private WalkerViewAdapter girl;

        [Header("场景")]
        [SerializeField] private ActCameraRig cameraRig;
        [SerializeField] private TrailViewAdapter trailView;
        [SerializeField] private Rect walkBounds = new Rect(-6f, -200f, 12f, 210f);
        [SerializeField] private Rect crowdZone;                 // 看客区域（进入开始迷失计时）
        [SerializeField] private string trailAreaId = "sand";

        [Header("蘑菇与跳跃")]
        [SerializeField] private List<MushroomPlatformView> mushrooms = new List<MushroomPlatformView>();
        [SerializeField] private List<MushroomPlatformView> hiddenPlatforms = new List<MushroomPlatformView>(); // B/C 平台：微弱轮廓
        [SerializeField] private List<JumpZoneData> jumpZones = new List<JumpZoneData>();   // 含 B2<->L1 双向
        [SerializeField] private float jumpApexVisual = 1.2f;

        [Header("隐藏区域")]
        [SerializeField] private List<StoneTabletView> tablets = new List<StoneTabletView>();

        [Header("看客（无视模式）")]
        [SerializeField] private AmbientCrowdAdapter ambientCrowd;
        [SerializeField] private CanvasGroup fadeToBlack;        // 晕倒黑屏

        private IDemo3Services services;
        private Act3Session session;

        public override void Configure(IDemo3Services s)
        {
            services = s;
            services.Bus.Subscribe<JumpLandedEvent>(OnGirlLanded);
            services.Bus.Subscribe<TabletLitEvent>(OnTabletLit);
            services.Bus.Subscribe<GirlFaintedEvent>(OnGirlFainted);
        }

        private void Start()
        {
            if (services == null) { Debug.LogError("[Demo3] Act3 需要 Demo3FlowCarrier 在场。"); enabled = false; return; }

            session = new Act3Session(girl.transform.position, services.Bus, services.Flow, services.Flags);
            session.SetJumpZones(jumpZones);
            session.SetCrowdZone(crowdZone);
            session.Player.Bounds = walkBounds;

            girl.Bind(session.Player.Position);
            if (session.Player.Trails != null)
                session.Player.Trails.MarkPlaced += trailView.OnMarkPlaced;

            // 女孩视角：主线已点亮的保持亮，未点亮隐藏平台给微弱轮廓
            foreach (var m in mushrooms) m.SetVisibility(MushroomPlatformView.Visibility.Lit);
            foreach (var m in hiddenPlatforms) m.SetVisibility(MushroomPlatformView.Visibility.Faint);

            // 旧脚印复现（变淡）
            if (services.Flags != null)
                trailView.RenderOldMarks(services.Flags.GetOldMarks(trailAreaId));

            services.Input.JumpPressed += OnJumpPressed;
            services.Input.InteractPressed += OnInteractPressed;
        }

        private void OnJumpPressed() => session?.TryBeginJump();

        private void OnInteractPressed()
        {
            if (session == null) return;
            foreach (var t in tablets)
                if (t.IsInRange(session.Player.Position))
                    session.NotifyTabletInteracted(t.TabletId);
        }

        private void OnGirlLanded(JumpLandedEvent e)
        {
            if (!e.IsGirl) return;
            foreach (var m in mushrooms)    TryLand(m, e.Pos);
            foreach (var m in hiddenPlatforms) TryLand(m, e.Pos); // 女孩跳上隐藏平台同样点亮
        }

        private static void TryLand(MushroomPlatformView m, Vector2 pos)
        {
            if (((Vector2)m.transform.position - pos).sqrMagnitude <= 4f) m.OnLanded(isGirl: true);
        }

        private void OnTabletLit(TabletLitEvent e)
        {
            foreach (var t in tablets)
                if (t.TabletId == e.TabletId) t.PlayLitEffect();
        }

        private void OnGirlFainted(GirlFaintedEvent _) => StartCoroutine(FaintRoutine());

        private IEnumerator FaintRoutine()
        {
            for (float t = 0f; t < 2.5f; t += Time.deltaTime)
            {
                if (fadeToBlack != null) fadeToBlack.alpha = t / 2.5f;
                yield return null;
            }
            if (fadeToBlack != null) fadeToBlack.alpha = 1f;
            session.NotifyFaintFinished();
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
            cameraRig.Apply(session.Player.Position);
            ambientCrowd?.Tick(dt);
        }

        private void OnDestroy()
        {
            if (services == null) return;
            services.Input.JumpPressed -= OnJumpPressed;
            services.Input.InteractPressed -= OnInteractPressed;
            services.Bus.Unsubscribe<JumpLandedEvent>(OnGirlLanded);
            services.Bus.Unsubscribe<TabletLitEvent>(OnTabletLit);
            services.Bus.Unsubscribe<GirlFaintedEvent>(OnGirlFainted);
        }
    }
}
