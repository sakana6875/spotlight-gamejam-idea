using System.Collections;
using System.Collections.Generic;
using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 第二幕「拯救」场景根：接线与每帧驱动。
    /// 场景内容：Intro 救援播片 → 黑夜上行（男孩牵女孩）→ 蘑菇跳台（女孩引导）→ 树下睡去 → 切第三幕。
    /// 全部玩法规则在 Act2Session/GirlGuideAI，本类只做表现同步与播片协程。
    /// </summary>
    public sealed class Act2Root : ActSceneRootBase
    {
        [Header("角色")]
        [SerializeField] private WalkerViewAdapter boy;
        [SerializeField] private WalkerViewAdapter girl;
        [SerializeField] private HandholdFollower handhold;

        [Header("场景")]
        [SerializeField] private ActCameraRig cameraRig;
        [SerializeField] private TrailViewAdapter trailView;
        [SerializeField] private Act2IntroDirector introDirector;
        [SerializeField] private Rect walkBounds = new Rect(-6f, -5f, 12f, 200f);

        [Header("蘑菇跳台段")]
        [SerializeField] private List<MushroomPlatformView> mushrooms = new List<MushroomPlatformView>();
        [SerializeField] private List<JumpZoneData> jumpZones = new List<JumpZoneData>();
        [SerializeField] private float jumpApexVisual = 1.2f;

        private IDemo3Services services;
        private Act2Session session;
        private TrailRecorder followerTrails; // 牵手跟随者的脚印（沙地段女孩脚印）
        private ActWalkSession trailHookedFor; // 当前已订阅视图的轨迹记录器归属

        public override void Configure(IDemo3Services s)
        {
            services = s;
            services.Bus.Subscribe<Act2SegmentChangedEvent>(OnSegmentChanged);
            services.Bus.Subscribe<GirlLandedOnPlatformEvent>(OnGirlLanded);
            services.Bus.Subscribe<JumpLandedEvent>(OnPlayerLanded);
        }

        private void Start()
        {
            if (services == null) { Debug.LogError("[Demo3] Act2 需要 Demo3FlowCarrier 在场（从第一幕进入，或在该场景放一个）。"); enabled = false; return; }

            var tops = new List<Vector2>();
            foreach (var m in mushrooms) tops.Add(m.transform.position);
            session = new Act2Session(boy.transform.position, tops, services.Bus, services.Flow, services.Flags);
            session.SetJumpZones(jumpZones);
            session.Player.Bounds = walkBounds;
            session.JumpApexVisual = jumpApexVisual;

            boy.Bind(session.Player.Position);
            girl.Bind(session.Player.Position + new Vector2(0.45f, -0.1f));

            foreach (var m in mushrooms) m.SetVisibility(MushroomPlatformView.Visibility.Hidden); // 男孩看不见未点亮的

            services.Input.JumpPressed += OnJumpPressed;
            introDirector.Play(() => session.NotifyIntroFinished());
        }

        private void OnJumpPressed()
        {
            if (session != null && session.TryBeginJump()) { /* 起跳事实已在会话内发布 */ }
        }

        private void OnSegmentChanged(Act2SegmentChangedEvent e)
        {
            if (e.Segment == Act2Segment.Mushrooms)
                StartCoroutine(MushroomIntroRoutine());      // 女孩松手 → 跳上第一个蘑菇顶（播片）
            else if (e.Segment == Act2Segment.ToTree)
            {
                handhold.SetFollower(boy.transform);          // 第三段女孩领队、男孩跟随
                handhold.Snap(session.Player.Position, boy.transform.position);
                handhold.Active = true;
            }
        }

        private void OnGirlLanded(GirlLandedOnPlatformEvent e)
        {
            if (e.PlatformIndex < mushrooms.Count)
                mushrooms[e.PlatformIndex].OnLanded(isGirl: true); // 凹陷回弹 + 点亮 + 粒子
        }

        private void OnPlayerLanded(JumpLandedEvent e)
        {
            if (e.IsGirl) return;
            var m = NearestMushroom(e.Pos);
            if (m != null) m.OnLanded(isGirl: false);              // 男孩只触发凹陷回弹
        }

        /// <summary>会话在不同段落会更换 Player 与轨迹记录器，这里统一把新记录器接到视图上</summary>
        private void HookPlayerTrails()
        {
            if (session == null || session.Player.Trails == null) return;
            if (trailHookedFor == session.Player) return;
            trailHookedFor = session.Player;
            session.Player.Trails.MarkPlaced += trailView.OnMarkPlaced;
        }

        private MushroomPlatformView NearestMushroom(Vector2 pos)
        {
            MushroomPlatformView best = null; float bd = float.MaxValue;
            foreach (var m in mushrooms)
            {
                float d = ((Vector2)m.transform.position - pos).sqrMagnitude;
                if (d < bd) { bd = d; best = m; }
            }
            return bd <= 4f ? best : null;
        }

        private IEnumerator MushroomIntroRoutine()
        {
            handhold.Active = false; // 女孩主动松手
            Vector2 from = girl.transform.position;
            Vector2 to = mushrooms.Count > 0 ? mushrooms[0].transform.position : from + Vector2.up * 3f;
            for (float t = 0f; t < 1f;)
            {
                t += Time.deltaTime / 1.2f;
                Vector2 p = JumpTrajectory.Eval(from, to, t, out float h);
                girl.Apply(p, Vector2.zero);
                girl.SetVisualLift(h * jumpApexVisual);
                yield return null;
            }
            girl.SetVisualLift(0f);
            session.NotifyMushroomIntroFinished();
        }

        private void Update()
        {
            if (session == null) return;
            float dt = Time.deltaTime;
            Vector2 move = services.Input.Move;

            session.Tick(dt, move, cameraRig.CameraBottomY);

            // 男孩视图（含跳跃弧线）
            if (session.PlayerJumping)
            {
                Vector2 p = JumpTrajectory.Eval(session.PlayerJumpFrom, session.PlayerJumpTo,
                    session.PlayerJumpT, out float h);
                boy.Apply(p, Vector2.zero);
                boy.SetVisualLift(h * jumpApexVisual);
            }
            else
            {
                boy.SetVisualLift(0f);
                if (!session.GirlIsLeader) boy.Apply(session.Player.Position, session.Player.Velocity);
            }

            HookPlayerTrails();

            // 上行段：男孩领队，女孩跟随并留脚印
            if (session.Segment == Act2Segment.WalkUp)
            {
                handhold.SetLeaderPos(session.Player.Position);
                girl.Apply(handhold.FollowerPos, session.Player.Velocity);
                if (session.Player.Trails != null)
                {
                    if (followerTrails == null)
                    {
                        followerTrails = new TrailRecorder(0.8f, TrailKind.GirlFootprint);
                        followerTrails.MarkPlaced += trailView.OnMarkPlaced;
                    }
                    followerTrails.Tick(dt, handhold.FollowerPos, session.Player.Velocity);
                }
            }

            // 蘑菇段：女孩由引导 AI 驱动
            if (session.Segment == Act2Segment.Mushrooms && session.Guide != null)
            {
                girl.Apply(session.Guide.Position, Vector2.zero);
                girl.SetVisualLift(session.Guide.JumpHeight01 * jumpApexVisual);
            }

            // 第三段：操控女孩，男孩跟随
            if (session.GirlIsLeader)
            {
                girl.Apply(session.Player.Position, session.Player.Velocity);
                handhold.SetLeaderPos(session.Player.Position);
                boy.Apply(handhold.FollowerPos, session.Player.Velocity);
            }

            cameraRig.Apply(session.Player.Position);
        }

        private void OnDestroy()
        {
            if (services == null) return;
            services.Input.JumpPressed -= OnJumpPressed;
            services.Bus.Unsubscribe<Act2SegmentChangedEvent>(OnSegmentChanged);
            services.Bus.Unsubscribe<GirlLandedOnPlatformEvent>(OnGirlLanded);
            services.Bus.Unsubscribe<JumpLandedEvent>(OnPlayerLanded);
        }
    }
}
