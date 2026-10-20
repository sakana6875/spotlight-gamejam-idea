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
    /// 场景内容：Intro 救援播片 → 黑夜上行（男孩牵女孩）→ 蓝框触发器接管自动走位到黄点 →
    ///           蘑菇跳台（女孩引导，玩家只能按空格逐台跳）→ 树下睡去 → 切第三幕。
    /// 全部玩法规则在 Act2Session/GirlGuideAI，本类只做表现同步与播片协程。
    /// 跳跃区不需要手填：按"黄点→M1→M2→…→最后一朵"的蘑菇列表顺序自动生成
    /// （起跳圈圆心=上一个落点，半径由 Jump Zone Radius 统一控制）。
    /// </summary>
    public sealed class Act2Root : ActSceneRootBase
    {
        [Header("角色")]
        [SerializeField] private WalkerViewAdapter boy;
        [SerializeField] private FollowerViewAdapter girl;   // 女孩无刚体：跟随男孩位置 + 播动画
        [SerializeField] private HandholdFollower handhold;

        [Header("场景")]
        [SerializeField] private ActCameraRig cameraRig;
        [SerializeField] private Act2IntroDirector introDirector;
        [Tooltip("活动范围（红框）：把红框那个 PolygonCollider2D 拖进来，玩家被钳在它的包围盒内；留空则不限制")]
        [SerializeField] private Collider2D playArea;

        [Header("蘑菇跳台段")]
        [SerializeField] private List<MushroomPlatformView> mushrooms = new List<MushroomPlatformView>();
        [SerializeField] private float jumpApexVisual = 1.2f;
        [Tooltip("蓝色框框：空物体挂 BoxCollider2D（Is Trigger 勾上）后拖进来，玩家走入即被系统接管")]
        [SerializeField] private Collider2D mushroomEntryTrigger;
        [Tooltip("黄色方块：接管后自动走位的集结点（放一个空物体进来）")]
        [SerializeField] private Transform mushroomGatherPoint;
        [Tooltip("起跳圈半径：站在上一个落点（黄点/上一朵蘑菇）多近内按空格有效")]
        [SerializeField] private float jumpZoneRadius = 1.3f;

        [Header("幕末（跳完最后一朵蘑菇 → 渐黑 → 切大树场景 Act3）")]
        [Tooltip("幕末渐黑：全屏黑色 Image 上的 CanvasGroup（起始 Alpha=0）。留空则无渐黑直接切幕")]
        [SerializeField] private CanvasGroup fadeToBlack;
        [SerializeField] private float outroFadeSeconds = 1.2f;   // 渐黑时长
        [SerializeField] private float outroHoldSeconds = 0.4f;   // 全黑保持后切幕

        [Header("场景变体（复用本场景时才用：按本场景被进入的次数自动切换物体 失活/出现）")]
        [Tooltip("仅第 1 次进入（营救段）激活的物体：男孩相关、开场导演等——第 2 次进入自动失活")]
        [SerializeField] private List<GameObject> rescueOnlyObjects = new List<GameObject>();
        [Tooltip("仅第 2 次及以后进入（追寻段）激活的物体：隐藏平台入口等——第 1 次进入自动失活")]
        [SerializeField] private List<GameObject> pursueOnlyObjects = new List<GameObject>();

        [Header("调试")]
        [Tooltip("开启后：牵手段按住方向键每秒打一条走位诊断链（输入/会话位置/钳制范围/刚体/transform），用于定位「男孩原地踏步」")]
        [SerializeField] private bool moveDebugLog = true;

        private IDemo3Services services;
        private Act2Session session;
        private Vector2 lastGuidePos;   // 蘑菇段引导 AI 上一帧位置（反推速度喂动画）
        private bool hasLastGuidePos;
        private Vector2 lastBoyJumpPos; // 男孩跳跃弧上一帧位置（反推速度喂方向动画）
        private bool hasLastBoyJumpPos;
        private Vector2 stuckWatchPos;  // 卡死哨兵：上一帧会话位置
        private float stuckTimer;
        private bool stuckReported;
        private float moveDebugTimer;   // 走位诊断链计时

        public override void Configure(IDemo3Services s)
        {
            services = s;
            services.Bus.Subscribe<Act2SegmentChangedEvent>(OnSegmentChanged);
            services.Bus.Subscribe<GirlLandedOnPlatformEvent>(OnGirlLanded);
            services.Bus.Subscribe<JumpLandedEvent>(OnPlayerLanded);
        }

        private void Start()
        {
            Debug.Log("[Demo3] Act2Root v10.19a 已加载（末菇落地→渐黑→切大树场景版）", this);
            StartCoroutine(InitWhenReady());
        }

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

            // 场景变体：按"本场景第几次进入"切换物体 失活/出现（进入次数由载体上的全局进度携带）
            int visit = services.Progress != null ? services.Progress.EntryCount(Demo3SceneIds.Act2Rescue) : 1;
            bool pursue = visit >= 2;
            foreach (var go in rescueOnlyObjects) if (go != null) go.SetActive(!pursue);
            foreach (var go in pursueOnlyObjects) if (go != null) go.SetActive(pursue);
            if (rescueOnlyObjects.Count + pursueOnlyObjects.Count > 0)
                Debug.Log($"[Demo3] Act2 场景第 {visit} 次进入（来源场景：{services.Progress?.LastSceneId ?? "未知"}），" +
                          $"当前变体：{(pursue ? "追寻段" : "营救段")}（营救专属 {rescueOnlyObjects.Count} 个已{(pursue ? "失活" : "激活")}，" +
                          $"追寻专属 {pursueOnlyObjects.Count} 个已{(pursue ? "激活" : "失活")}）。", this);

            var tops = new List<Vector2>();
            foreach (var m in mushrooms) tops.Add(m.LandingPos);
            session = new Act2Session(boy.transform.position, tops, services.Bus, services.Flow, services.Flags);
            session.SetJumpZones(BuildJumpZones());
            if (playArea != null) session.Player.Bounds = ToRect(playArea.bounds); // 红框钳制（碰撞体的包围盒）
            session.JumpApexVisual = jumpApexVisual;
            session.MushroomEntryZone = ToRect(mushroomEntryTrigger.bounds);       // 蓝框（触发器碰撞体的包围盒）
            session.MushroomGatherPoint = mushroomGatherPoint.position;

            boy.Bind(session.Player.Position);
            girl.Bind(girl.transform.position);   // 女孩呆在她摆放的位置上（开场跪姿点由场景摆放决定）

            foreach (var m in mushrooms) m.SetVisibility(MushroomPlatformView.Visibility.Hidden); // 男孩看不见未点亮的

            services.Input.JumpPressed += OnJumpPressed;
            introDirector.Play(() =>
            {
                // 播片把男孩牵到了新位置，交还操控前先把会话位置同步过去，避免瞬移回开场点
                session.Player.Teleport(boy.transform.position);
                session.NotifyIntroFinished();
            });
        }

        /// <summary>跳跃区链自动生成：黄点→M1→M2→…→最后一朵，起跳圈圆心=上一个落点</summary>
        private List<JumpZoneData> BuildJumpZones()
        {
            var list = new List<JumpZoneData>();
            Vector2 from = mushroomGatherPoint.position;
            for (int i = 0; i < mushrooms.Count; i++)
            {
                Vector2 to = mushrooms[i].LandingPos;
                list.Add(new JumpZoneData($"to-m{i + 1}", from, jumpZoneRadius, to));
                from = to;
            }
            return list;
        }

        private static Rect ToRect(Bounds b) => new Rect(b.min.x, b.min.y, b.size.x, b.size.y);

        /// <summary>启动自检：缺引用一次性报全，避免协程半途 NRE 静默卡死</summary>
        private bool SelfCheck()
        {
            var missing = new List<string>();
            if (boy == null) missing.Add(nameof(boy));
            if (girl == null) missing.Add(nameof(girl));
            if (handhold == null) missing.Add(nameof(handhold));
            if (cameraRig == null) missing.Add(nameof(cameraRig));
            if (introDirector == null) missing.Add(nameof(introDirector));
            if (mushroomEntryTrigger == null) missing.Add(nameof(mushroomEntryTrigger));
            if (mushroomGatherPoint == null) missing.Add(nameof(mushroomGatherPoint));
            if (missing.Count > 0)
            {
                var sb = new StringBuilder("[Demo3] Act2Root 缺少引用：");
                foreach (var m in missing) sb.Append(m).Append('、');
                sb.Append("。请在 Inspector 里补齐后重进场景。");
                Debug.LogError(sb.ToString(), this);
                return false;
            }
            if (playArea == null)
                Debug.LogWarning("[Demo3] Act2Root 的 Play Area 未设置：玩家左右不受红框限制。", this);
            if (mushrooms.Count == 0)
                Debug.LogWarning("[Demo3] Act2Root 的 Mushrooms 为空：蘑菇段将无台可跳。", this);
            if (fadeToBlack == null)
                Debug.LogWarning("[Demo3] Act2Root 的 Fade To Black 未拖：跳完最后一朵蘑菇将无渐黑，直接切大树场景。", this);
            return true;
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
                girl.SetFollow(boy.transform);         // 女孩变为纯跟随者：跟着男孩位置并播移动动画
            }
            else if (e.Segment == Act2Segment.Mushrooms)
            {
                girl.SetFollow(null);                       // 蘑菇段：女孩改由引导 AI 驱动
                StartCoroutine(MushroomIntroRoutine());      // 女孩松手 → 跳上第一个蘑菇顶（播片）
            }
            else if (e.Segment == Act2Segment.Done)
            {
                StartCoroutine(OutroRoutine());   // 跳完最后一朵蘑菇：渐黑 → 切大树场景 Act3
            }
        }

        /// <summary>幕末：画面渐黑 → 黑场片刻 → 切大树场景（大树演出与男孩退场在 Act3 大树场景进行）</summary>
        private IEnumerator OutroRoutine()
        {
            for (float t = 0f; t < outroFadeSeconds; t += Time.deltaTime)
            {
                if (fadeToBlack != null) fadeToBlack.alpha = Mathf.Clamp01(t / outroFadeSeconds);
                yield return null;
            }
            if (fadeToBlack != null) fadeToBlack.alpha = 1f;
            for (float t = 0f; t < outroHoldSeconds; t += Time.deltaTime) yield return null;
            session.NotifyOutroFinished();
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
            Vector2 to = mushrooms.Count > 0 ? mushrooms[0].LandingPos : from + Vector2.up * 3f;
            girl.SetJumpAnim(true); // 预留跳跃动画位；没填 Jump 状态名则按位移方向播
            Vector2 prev = from;
            for (float t = 0f; t < 1f;)
            {
                t += Time.deltaTime / 1.2f;
                Vector2 p = JumpTrajectory.Eval(from, to, t, out float h);
                girl.Apply(p, (p - prev) / Mathf.Max(Time.deltaTime, 1e-5f));
                prev = p;
                girl.SetVisualLift(h * jumpApexVisual);
                yield return null;
            }
            girl.SetJumpAnim(false);
            girl.SetVisualLift(0f);
            session.NotifyMushroomIntroFinished();
        }

        private void Update()
        {
            if (session == null) return;
            float dt = Time.deltaTime;
            Vector2 move = services.Input.Move;

            session.Tick(dt, move, cameraRig.CameraBottomY);

            // 卡死哨兵：有输入、没锁操作，但会话位置 1 秒没动——九成是 Play Area 碰撞体没罩住玩家
            // （会话先算速度再钳位置，所以被钳死时走路动画照播、坐标不变）
            if (session.Segment == Act2Segment.WalkUp && !session.Player.ControlLocked
                && move.sqrMagnitude > 0.04f)
            {
                if ((session.Player.Position - stuckWatchPos).sqrMagnitude < 1e-6f)
                {
                    stuckTimer += dt;
                    if (stuckTimer > 1f && !stuckReported)
                    {
                        stuckReported = true;
                        var b = session.Player.Bounds;
                        Debug.LogError($"[Demo3] 输入正常但男孩被钳死在原地：会话位置 {session.Player.Position}，" +
                                       $"钳制范围 X[{b.xMin:F2}, {b.xMax:F2}] Y[{b.yMin:F2}, {b.yMax:F2}]，" +
                                       $"动态底墙 {session.Player.DynamicFloorY:F2}，玩家在范围内={b.Contains(session.Player.Position)}。" +
                                       "检查 Act2Root 的 Play Area 拖的那个碰撞体：它的位置和大小必须罩住男孩的活动区域" +
                                       "（是不是拖错成了别的小碰撞体？尺寸是不是没拉过？）。",
                                       this);
                    }
                }
                else stuckTimer = 0f;
            }
            else stuckTimer = 0f;
            stuckWatchPos = session.Player.Position;

            // 走位诊断链：每秒一条，看数字就知道断在哪一环
            // 会话位置不动=钳制问题；会话动/刚体不动=MovePosition 链路断；刚体动/transform 不动=动画覆写
            if (moveDebugLog && session.Segment == Act2Segment.WalkUp && move.sqrMagnitude > 0.04f)
            {
                moveDebugTimer += dt;
                if (moveDebugTimer >= 1f)
                {
                    moveDebugTimer = 0f;
                    var b = session.Player.Bounds;
                    Debug.Log($"[Demo3][走位诊断] 输入={move}  会话位置={session.Player.Position}  " +
                              $"会话速度={session.Player.Velocity}  钳制范围X[{b.xMin:F1},{b.xMax:F1}] " +
                              $"Y[{b.yMin:F1},{b.yMax:F1}]  底墙={session.Player.DynamicFloorY:F1}  " +
                              $"男孩rb={boy.DebugRbPosition}  男孩transform={(Vector2)boy.transform.position}  " +
                              $"兜底={boy.DebugLateOverride}", this);
                }
            }
            else moveDebugTimer = 0f;

            // 男孩视图（含跳跃弧线；速度由位移反推，左右跳播左右动画——与女孩同规则）
            boy.SetJumpAnim(session.PlayerJumping); // 填了 States→Jump 状态名才生效，否则回退方向动画
            if (session.PlayerJumping)
            {
                Vector2 p = JumpTrajectory.Eval(session.PlayerJumpFrom, session.PlayerJumpTo,
                    session.PlayerJumpT, out float h);
                Vector2 bv = hasLastBoyJumpPos ? (p - lastBoyJumpPos) / Mathf.Max(dt, 1e-5f) : Vector2.zero;
                lastBoyJumpPos = p; hasLastBoyJumpPos = true;
                boy.Apply(p, bv);
                boy.SetVisualLift(h * jumpApexVisual);
            }
            else
            {
                hasLastBoyJumpPos = false;
                boy.SetVisualLift(0f);
                boy.Apply(session.Player.Position, session.Player.Velocity);
            }

            // 上行段 + 接管段：女孩由自身 FollowerViewAdapter 跟随男孩（此处无需驱动）

            // 蘑菇段：女孩由引导 AI 驱动（速度由位移反推，保证走动动画正常）
            if (session.Segment == Act2Segment.Mushrooms && session.Guide != null)
            {
                girl.SetJumpAnim(session.Guide.State == GuideState.Jumping); // 预留跳跃动画位
                Vector2 gp = session.Guide.Position;
                Vector2 gv = hasLastGuidePos ? (gp - lastGuidePos) / Mathf.Max(dt, 1e-5f) : Vector2.zero;
                lastGuidePos = gp; hasLastGuidePos = true;
                girl.Apply(gp, gv);
                girl.SetVisualLift(session.Guide.JumpHeight01 * jumpApexVisual);
            }
            else
            {
                hasLastGuidePos = false;
                if (session.Segment != Act2Segment.Mushrooms) girl.SetJumpAnim(false);
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
