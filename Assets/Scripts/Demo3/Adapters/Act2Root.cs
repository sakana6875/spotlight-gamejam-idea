using System.Collections;
using System.Collections.Generic;
using System.Text;
using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 第二幕「拯救」场景根：接线与每帧驱动。
    /// 场景内容：Intro 救援播片 → 黑夜上行（男孩牵女孩）→ 蓝框接管自动走位到黄点 →
    ///           蘑菇跳台（女孩引导，玩家只能按空格逐台跳）→ 树下睡去 → 切第三幕。
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
        [SerializeField] private Act2IntroDirector introDirector;
        [Tooltip("红色框框：玩家活动范围，出不去")]
        [SerializeField] private Rect walkBounds = new Rect(-6f, -5f, 12f, 200f);

        [Header("蘑菇跳台段")]
        [SerializeField] private List<MushroomPlatformView> mushrooms = new List<MushroomPlatformView>();
        [SerializeField] private List<JumpZoneData> jumpZones = new List<JumpZoneData>();
        [SerializeField] private float jumpApexVisual = 1.2f;
        [Tooltip("蓝色框框：玩家走入即被系统接管（ Rect 按场景坐标摆 ）")]
        [SerializeField] private Rect mushroomEntryZone = new Rect(-6f, 58f, 12f, 6f);
        [Tooltip("黄色方块：接管后自动走位的集结点（放一个空物体进来）")]
        [SerializeField] private Transform mushroomGatherPoint;

        [Header("段落阈值")]
        [Tooltip("第三段终点：大树位置的绝对 Y 坐标（须高于蘑菇段出口）")]
        [SerializeField] private float treeY = 150f;

        private IDemo3Services services;
        private Act2Session session;
        private Vector2 lastGuidePos;   // 蘑菇段引导 AI 上一帧位置（反推速度喂动画）
        private bool hasLastGuidePos;

        public override void Configure(IDemo3Services s)
        {
            services = s;
            services.Bus.Subscribe<Act2SegmentChangedEvent>(OnSegmentChanged);
            services.Bus.Subscribe<GirlLandedOnPlatformEvent>(OnGirlLanded);
            services.Bus.Subscribe<JumpLandedEvent>(OnPlayerLanded);
        }

        private void Start() => StartCoroutine(InitWhenReady());

        private IEnumerator InitWhenReady()
        {
            // 载体的加载协程会在场景激活后一两个帧才注入服务（它有意多等了一帧），这里等注入再初始化
            float waited = 0f;
            while (services == null && waited < 3f) { waited += Time.unscaledDeltaTime; yield return null; }
            if (services == null)
            {
                Debug.LogError("[Demo3] Act2 需要 Demo3FlowCarrier 在场（从第一幕进入，或在该场景放一个）。");
                enabled = false;
                yield break;
            }
            if (!SelfCheck()) { enabled = false; yield break; }

            var tops = new List<Vector2>();
            foreach (var m in mushrooms) tops.Add(m.transform.position);
            session = new Act2Session(boy.transform.position, tops, services.Bus, services.Flow, services.Flags);
            session.SetJumpZones(jumpZones);
            session.Player.Bounds = walkBounds;
            session.JumpApexVisual = jumpApexVisual;
            session.MushroomEntryZone = mushroomEntryZone;
            if (mushroomGatherPoint != null) session.MushroomGatherPoint = mushroomGatherPoint.position;
            session.TreeY = treeY;

            boy.Bind(session.Player.Position);
            girl.Bind(session.Player.Position + new Vector2(0.45f, -0.1f));

            foreach (var m in mushrooms) m.SetVisibility(MushroomPlatformView.Visibility.Hidden); // 男孩看不见未点亮的

            services.Input.JumpPressed += OnJumpPressed;
            introDirector.Play(() =>
            {
                // 播片把男孩牵到了新位置，交还操控前先把会话位置同步过去，避免瞬移回开场点
                session.Player.Teleport(boy.transform.position);
                session.NotifyIntroFinished();
            });
        }

        /// <summary>启动自检：缺引用一次性报全，避免协程半途 NRE 静默卡死</summary>
        private bool SelfCheck()
        {
            var missing = new List<string>();
            if (boy == null) missing.Add(nameof(boy));
            if (girl == null) missing.Add(nameof(girl));
            if (handhold == null) missing.Add(nameof(handhold));
            if (cameraRig == null) missing.Add(nameof(cameraRig));
            if (introDirector == null) missing.Add(nameof(introDirector));
            if (missing.Count == 0) return true;

            var sb = new StringBuilder("[Demo3] Act2Root 缺少引用：");
            foreach (var m in missing) sb.Append(m).Append('、');
            sb.Append("。请在 Inspector 里补齐后重进场景。");
            Debug.LogError(sb.ToString(), this);
            return false;
        }

        private void OnJumpPressed()
        {
            if (session != null && session.TryBeginJump()) { /* 起跳事实已在会话内发布 */ }
        }

        private void OnSegmentChanged(Act2SegmentChangedEvent e)
        {
            if (e.Segment == Act2Segment.WalkUp)
            {
                boy.SetHandholding(true);              // 男孩牵起女孩，进入"牵手跑动"动画状态
                girl.SetHandholding(true);
            }
            else if (e.Segment == Act2Segment.Mushrooms)
                StartCoroutine(MushroomIntroRoutine());      // 女孩松手 → 跳上第一个蘑菇顶（播片）
            else if (e.Segment == Act2Segment.ToTree)
            {
                handhold.SetFollower(boy.transform);          // 第三段女孩领队、男孩跟随
                handhold.Snap(session.Player.Position, boy.transform.position);
                handhold.Active = true;
                boy.SetHandholding(true);                     // 重新牵手
                girl.SetHandholding(true);
            }
        }

        private void OnGirlLanded(GirlLandedOnPlatformEvent e)
        {
            if (e.PlatformIndex < mushrooms.Count)
                mushrooms[e.PlatformIndex].OnLanded(isGirl: true); // 凹陷回弹 + 点亮
        }

        private void OnPlayerLanded(JumpLandedEvent e)
        {
            if (e.IsGirl) return;
            var m = NearestMushroom(e.Pos);
            if (m != null) m.OnLanded(isGirl: false);              // 男孩只触发凹陷回弹
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
            boy.SetHandholding(false);
            girl.SetHandholding(false);
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

            // 上行段 + 接管段：男孩领队，女孩牵手跟随（接管段男孩由会话自动走位）
            if (session.Segment == Act2Segment.WalkUp || session.Segment == Act2Segment.Approach)
            {
                handhold.SetLeaderPos(session.Player.Position);
                girl.Apply(handhold.FollowerPos, session.Player.Velocity);
            }

            // 蘑菇段：女孩由引导 AI 驱动（速度由位移反推，保证走动动画正常）
            if (session.Segment == Act2Segment.Mushrooms && session.Guide != null)
            {
                Vector2 gp = session.Guide.Position;
                Vector2 gv = hasLastGuidePos ? (gp - lastGuidePos) / Mathf.Max(dt, 1e-5f) : Vector2.zero;
                lastGuidePos = gp; hasLastGuidePos = true;
                girl.Apply(gp, gv);
                girl.SetVisualLift(session.Guide.JumpHeight01 * jumpApexVisual);
            }
            else hasLastGuidePos = false;

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
