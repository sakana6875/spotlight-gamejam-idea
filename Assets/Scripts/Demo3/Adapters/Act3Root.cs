using System;
using System.Collections;
using System.Collections.Generic;
using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 女孩独行通用场景根（第三幕 Act2-1 追寻蘑菇 / 看客场景 Act4 / 第四幕 Act2-2 重演蘑菇共用）。
    /// 所有区域一律拖"带 Collider2D 的空物体"，落点一律拖 Transform，无需手填坐标。
    /// 固定动线：蘑菇垫落地即锁定移动（只能按跳跃键沿跳区链前进）；
    /// 落到标记"可行走平台"的落点（如 L1）恢复 WASD 且活动范围钳到该平台。
    /// 石碑：靠近按 E ——填了 Portal Scene Id 的石碑传送隐藏区（记账返回点，回来原地复活），否则点亮。
    /// 看客场景：走入 Crowd Vanish Trigger → 渐黑 → 人群消失 → 渐亮；再走入 Finish Trigger → 渐黑切场。
    /// 蘑菇场景收尾二选一：Finish Trigger（走入切场）或末菇跳下（Final Jump Zone + Final Landing Point，落地即切场）。
    /// </summary>
    public sealed class Act3Root : ActSceneRootBase
    {
        /// <summary>手配跳区（支线/平台）：全部拖物体，不填坐标</summary>
        [Serializable]
        public sealed class JumpZoneWire
        {
            [Tooltip("起名（可选，不填用起跳区物体名）")]
            public string Id = "";
            [Tooltip("起跳区：拖一个带 Collider2D 的空物体（用其包围盒判定站位）")]
            public Collider2D Zone;
            [Tooltip("落点：拖一个空物体（如蘑菇的 Point 子物体、L1 上的落点）")]
            public Transform Landing;
            [Tooltip("落点是可行走平台（如 L1）：落地恢复 WASD；蘑菇垫不勾（只能继续跳）")]
            public bool LandingIsWalkable;
            [Tooltip("可行走落地时的活动范围：拖平台自身的 Collider2D（如 L1 白块）；留空则用全场 Walk Bounds")]
            public Collider2D LandingWalkArea;
            public bool Enabled = true;
        }

        [Header("角色")]
        [SerializeField] private WalkerViewAdapter girl;

        [Header("场景")]
        [SerializeField] private ActCameraRig cameraRig;
        [Tooltip("全场活动范围：拖一个带 Collider2D 的空物体（拉满女孩可走区域）")]
        [SerializeField] private Collider2D walkBoundsArea;
        [Tooltip("看客迷失区（可选）：拖 Collider2D。填了才有「进人群计时晕倒」；看客场景 Act4 走触发器演出时不填")]
        [SerializeField] private Collider2D crowdZoneArea;

        [Header("蘑菇与跳跃")]
        [SerializeField] private List<MushroomPlatformView> mushrooms = new List<MushroomPlatformView>();
        [SerializeField] private List<MushroomPlatformView> hiddenPlatforms = new List<MushroomPlatformView>(); // B/C 平台：微弱轮廓
        [Tooltip("主线自动成链：列表顺序 = 策划动线（出生点→第1朵→…→最后一朵）。站上本菇 Point 按跳 → 落到下一菇 Point")]
        [SerializeField] private bool autoChainFromMushrooms = true;
        [Tooltip("自动链的起跳圈半径（站在落点多少米内可按跳）")]
        [SerializeField] private float chainZoneRadius = 1.3f;
        [Tooltip("支线跳区（B1/B2/L1 等）：每条拖起跳区碰撞体+落点空物体")]
        [SerializeField] private List<JumpZoneWire> extraJumpZones = new List<JumpZoneWire>();
        [SerializeField] private float jumpApexVisual = 1.2f;

        [Header("隐藏区域（石碑传送）")]
        [SerializeField] private List<StoneTabletView> tablets = new List<StoneTabletView>();

        [Header("看客场景演出（Act4；蘑菇场景不用填）")]
        [SerializeField] private AmbientCrowdAdapter ambientCrowd;
        [Tooltip("人群消失触发：走入 → 渐黑 → 看客人群消失 → 渐亮（人群凭空散场的演出）")]
        [SerializeField] private Collider2D crowdVanishTrigger;
        [SerializeField] private float crowdVanishFadeSeconds = 1.0f;
        [Tooltip("可选提示物（光圈/箭头等）：人群消失演出还没播时显现，播完自动隐藏")]
        [SerializeField] private GameObject crowdVanishVisual;
        [Tooltip("晕倒演出播完 → 切向的场景 ID；仅在配了 Crowd Zone Area 的迷失玩法下用得到")]
        [SerializeField] private string faintGoToSceneId = "";

        [Header("收尾")]
        [SerializeField] private CanvasGroup fadeToBlack;        // 切场/演出黑屏
        [Tooltip("走入即切场的终点区（可选，Collider2D 勾 Is Trigger）。Act4 看客场景用它切 Act2-2")]
        [SerializeField] private Collider2D finishTrigger;
        [Tooltip("可选提示物（光圈/箭头等）：终点区激活时才显现，未激活（剧情前置未满足）自动隐藏")]
        [SerializeField] private GameObject finishVisual;
        [Tooltip("防误入：配了 Crowd Vanish Trigger 且本项勾上时，人群消失演出没播完，Finish Trigger 不触发" +
                 "（看客场景必须先播散场演出才能切场，防止玩家绕过剧情直接切走）")]
        [SerializeField] private bool finishRequireCrowdVanished = true;
        [Tooltip("末菇跳下收尾：站上最后一朵蘑菇的判定区（拖碰撞体）。女孩在此按跳 → 落到 Final Landing Point")]
        [SerializeField] private Collider2D finalJumpZone;
        [Tooltip("末菇跳下的落点：拖一个空物体（命名 Point 便于辨认）。落地 → 渐黑 → 切 Finish Scene Id")]
        [SerializeField] private Transform finalLandingPoint;
        [Tooltip("收尾切向的场景 ID：Act2-1 填 Act4（看客区），Act2-2 填 Act3（回大树），Act4 填 Act2-2")]
        [SerializeField] private string finishSceneId = "";
        [SerializeField] private float finishFadeSeconds = 1.2f;
        [Tooltip("石碑传送的渐黑时长（秒）")]
        [SerializeField] private float portalFadeSeconds = 0.8f;

        [Header("进场")]
        [Tooltip("进场时从黑幕渐亮（上一场景以黑屏收尾，建议保持勾选；Fade To Black 未拖则自动跳过）")]
        [SerializeField] private bool fadeInOnStart = true;
        [SerializeField] private float fadeInSeconds = 1.0f;

        [Header("调参")]
        [SerializeField] private float runSpeed = 3.4f;          // 女孩慢跑速度（第二幕是 2.2）
        [SerializeField] private float jumpPlanarSpeed = 6.5f;   // 跳跃平面速度
        [Tooltip("走进看客迷失区后开始计时，累计这么多秒晕倒；计时只进不退（走出人群不会取消）")]
        [SerializeField] private float faintDelay = 8f;

        private IDemo3Services services;
        private Act3Session session;
        private Vector2 lastJumpPos;    // 跳跃弧上一帧位置（反推速度喂方向动画）
        private bool hasLastJumpPos;
        private bool finishing;         // 切场进行中（防重入）
        private bool crowdVanished;     // 人群消失演出已播
        private bool finishBlockedLogged; // 终点被剧情前置拦住已报过一次

        private static Rect ToRect(Bounds b) => new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
        private string SceneName => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        public override void Configure(IDemo3Services s)
        {
            services = s;
            services.Bus.Subscribe<JumpLandedEvent>(OnGirlLanded);
            services.Bus.Subscribe<TabletLitEvent>(OnTabletLit);
            services.Bus.Subscribe<GirlFaintedEvent>(OnGirlFainted);
        }

        private void Start()
        {
            Debug.Log($"[Demo3] Act3Root v10.20 已加载（自动链严格续链＋按跳无响应日志版，场景 {SceneName}）", this);
            StartCoroutine(InitWhenReady());
        }

        private IEnumerator InitWhenReady()
        {
            // 载体的加载协程会在场景激活后一两个帧才注入服务，这里等注入再初始化
            float waited = 0f;
            while (services == null && waited < 3f) { waited += Time.unscaledDeltaTime; yield return null; }
            if (services == null)
            {
                Debug.LogError("[Demo3] 本场景需要 Demo3FlowCarrier 在场（从第一幕进入，或放一个临时载体）。");
                enabled = false;
                yield break;
            }
            if (!SelfCheck()) { enabled = false; yield break; }

            session = new Act3Session(girl.transform.position, services.Bus, services.Flow, services.Flags);
            session.SetWalkBounds(ToRect(walkBoundsArea.bounds));
            if (crowdZoneArea != null) session.SetCrowdZone(ToRect(crowdZoneArea.bounds));
            session.Player.Speed = runSpeed;
            session.JumpPlanarSpeed = jumpPlanarSpeed;
            session.FaintDelay = faintDelay;
            if (!string.IsNullOrEmpty(faintGoToSceneId)) session.FaintGoToSceneId = faintGoToSceneId;
            session.SetJumpZones(BuildAllZones());

            // 从隐藏区返回：原地复活（站位+平台行走范围由载体记账带来）
            if (services.Progress != null &&
                services.Progress.TryConsumeReturnPoint(SceneName, out Vector2 rp, out Rect area))
            {
                session.Player.Teleport(rp);
                session.ForceFreeMove(area);
                Debug.Log($"[Demo3] 从隐藏区返回，女孩原地复活在 {rp}。", this);
            }

            girl.Bind(session.Player.Position);

            // 女孩视角：主线已点亮的保持亮，未点亮隐藏平台给微弱轮廓
            foreach (var m in mushrooms) m.SetVisibility(MushroomPlatformView.Visibility.Lit);
            foreach (var m in hiddenPlatforms) m.SetVisibility(MushroomPlatformView.Visibility.Faint);

            services.Input.JumpPressed += OnJumpPressed;
            services.Input.InteractPressed += OnInteractPressed;

            // 进场渐亮：上一场景以黑屏收尾
            if (fadeInOnStart && fadeToBlack != null)
            {
                fadeToBlack.alpha = 1f;
                for (float t = 0f; t < fadeInSeconds; t += Time.deltaTime)
                {
                    fadeToBlack.alpha = 1f - t / fadeInSeconds;
                    yield return null;
                }
                fadeToBlack.alpha = 0f;
            }
        }

        /// <summary>跳区总装：支线手配（优先）→ 主线自动链（列表顺序=动线）→ 末菇跳下收尾</summary>
        private List<JumpZoneData> BuildAllZones()
        {
            var list = new List<JumpZoneData>();

            // 1) 支线手配跳区（拖碰撞体+落点；优先匹配，可压过自动链做分支）
            foreach (var w in extraJumpZones)
            {
                if (w == null) continue;
                if (w.Zone == null || w.Landing == null)
                {
                    Debug.LogWarning($"[Demo3] Act3Root 支线跳区「{(w == null ? "" : w.Id)}」缺 Zone 或 Landing，已跳过。", this);
                    continue;
                }
                list.Add(new JumpZoneData
                {
                    Id = string.IsNullOrEmpty(w.Id) ? w.Zone.name : w.Id,
                    Area = ToRect(w.Zone.bounds),
                    UseRectArea = true,
                    Landing = w.Landing.position,
                    Enabled = w.Enabled,
                    LandingIsWalkable = w.LandingIsWalkable,
                    LandingWalkArea = w.LandingWalkArea != null ? ToRect(w.LandingWalkArea.bounds) : default,
                });
            }

            // 2) 主线自动链：出生点→第 1 朵→…→最后一朵（站上本菇 Point 按跳 → 落到下一菇 Point）
            if (autoChainFromMushrooms && mushrooms.Count > 0)
            {
                Vector2 from = girl.transform.position; // 第一跳：出生点 → 第 1 朵
                for (int i = 0; i < mushrooms.Count; i++)
                {
                    Vector2 to = mushrooms[i].LandingPos;
                    list.Add(new JumpZoneData($"chain-{i + 1}", from, chainZoneRadius, to));
                    from = to;
                }
            }

            // 3) 末菇跳下收尾（站上最后一朵 → 落到指定 Point → 渐黑切场）
            if (finalJumpZone != null && finalLandingPoint != null)
                list.Add(new JumpZoneData
                {
                    Id = "final-drop",
                    Area = ToRect(finalJumpZone.bounds),
                    UseRectArea = true,
                    Landing = finalLandingPoint.position,
                    Enabled = true,
                });

            return list;
        }

        /// <summary>启动自检：缺引用一次性报全；出生点不在 Walk Bounds 里是头号静默死法（开场即瞬移）</summary>
        private bool SelfCheck()
        {
            var missing = new List<string>();
            if (girl == null) missing.Add(nameof(girl));
            if (cameraRig == null) missing.Add(nameof(cameraRig));
            if (walkBoundsArea == null) missing.Add(nameof(walkBoundsArea));
            if (missing.Count > 0)
            {
                Debug.LogError($"[Demo3] Act3Root 缺少引用：{string.Join("、", missing)}。" +
                               "请在 Inspector 里补齐后重进场景（Walk Bounds Area 拖一个带 Collider2D 的空物体）。", this);
                return false;
            }
            if (!walkBoundsArea.bounds.Contains(girl.transform.position))
                Debug.LogError($"[Demo3] Act3Root 的 Walk Bounds Area（{walkBoundsArea.bounds}）没罩住女孩出生点 " +
                               $"{(Vector2)girl.transform.position}——第一帧她就会被钳到边缘（看起来像是瞬移）。" +
                               "把该碰撞体的位置和大小调到罩住出生点与全程路径。", this);
            if (!autoChainFromMushrooms && extraJumpZones.Count == 0 && mushrooms.Count > 0)
                Debug.LogWarning("[Demo3] Act3Root 关了自动链又没配支线跳区：蘑菇将跳不上去（纯走路）。", this);
            if (crowdZoneArea != null && ambientCrowd == null)
                Debug.LogWarning("[Demo3] Act3Root 配了 Crowd Zone Area 但 Ambient Crowd 未拖：看客区将没有人群。", this);
            if (fadeToBlack == null)
                Debug.LogWarning("[Demo3] Act3Root 的 Fade To Black 未拖：切场/演出将没有渐黑，直接硬切。", this);
            bool hasFinish = finishTrigger != null || (finalJumpZone != null && finalLandingPoint != null);
            if (!hasFinish && crowdZoneArea == null)
                Debug.LogWarning("[Demo3] Act3Root 既没配收尾（Finish Trigger 或末菇跳下）也没配 Crowd Zone Area：本场景没有任何出口。", this);
            if (hasFinish && string.IsNullOrEmpty(finishSceneId))
                Debug.LogWarning("[Demo3] Act3Root 配了收尾但 Finish Scene Id 为空：触发后不会切场。", this);
            if (finishTrigger != null && !walkBoundsArea.bounds.Contains(finishTrigger.bounds.center))
                Debug.LogError($"[Demo3] Act3Root 的 Finish Trigger 中心 {(Vector2)finishTrigger.bounds.center} 不在 Walk Bounds Area 内——" +
                               "女孩会被钳制走不到终点。请扩大 Walk Bounds Area 或移动终点区域。", this);
            if ((finalJumpZone == null) != (finalLandingPoint == null))
                Debug.LogWarning("[Demo3] Act3Root 的末菇跳下收尾只配了一半：Final Jump Zone 与 Final Landing Point 要一起拖。", this);
            return true;
        }

        private void OnJumpPressed()
        {
            if (session == null) return;
            if (session.TryBeginJump()) return;
            if (finishing || !session.CanBeginJump) return; // 切场中/空中/锁定中的按跳属正常忽略
            Debug.Log($"[Demo3] 按了跳跃，但当前位置 {session.Player.Position} 不在任何可用跳区内：" +
                      "自动链只认「从上一落点续跳」（支线/可行走平台上主线链圈不响应）——" +
                      "支线请在 Extra Jump Zones 配连线，并让它的 Zone 碰撞体罩住站立点；" +
                      "主线请确认女孩确实站在上一朵蘑菇的 Point 上。", this);
        }

        private void OnInteractPressed()
        {
            if (session == null || finishing) return;
            foreach (var t in tablets)
            {
                if (t == null || !t.IsInRange(session.Player.Position)) continue;
                session.NotifyTabletInteracted(t.TabletId); // 点亮记账（已点亮会跳过）
                if (!string.IsNullOrEmpty(t.PortalSceneId))
                {
                    // 隐藏区入口：记下返回点（站位 + 所在可行走平台的范围）→ 渐黑传送
                    Rect area = default;
                    foreach (var w in extraJumpZones)
                    {
                        if (w == null || !w.LandingIsWalkable || w.LandingWalkArea == null) continue;
                        if (w.LandingWalkArea.bounds.Contains(session.Player.Position))
                        { area = ToRect(w.LandingWalkArea.bounds); break; }
                    }
                    services.Progress?.SetReturnPoint(SceneName, session.Player.Position, area);
                    StartCoroutine(PortalRoutine(t.PortalSceneId));
                }
                break;
            }
        }

        private void OnGirlLanded(JumpLandedEvent e)
        {
            if (!e.IsGirl) return;
            foreach (var m in mushrooms)       TryLand(m, e.Pos);
            foreach (var m in hiddenPlatforms) TryLand(m, e.Pos); // 女孩跳上隐藏平台同样点亮

            // 末菇跳下收尾：落到指定 Point → 渐黑 → 切场（如 Act2-1 的 M1 跳下 → 看客区 Act4）
            if (!finishing && finalLandingPoint != null &&
                ((Vector2)finalLandingPoint.position - e.Pos).sqrMagnitude <= 0.25f)
            {
                if (string.IsNullOrEmpty(finishSceneId))
                    Debug.LogWarning("[Demo3] 女孩已落到末菇跳下的指定点，但 Finish Scene Id 为空，不切场。", this);
                else StartCoroutine(FinishRoutine());
            }
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

        private void OnGirlFainted(GirlFaintedEvent _)
        {
            if (finishing) return;
            finishing = true;
            StartCoroutine(FaintRoutine());
        }

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

        /// <summary>石碑传送：渐黑 → 切隐藏区场景（返回点已在 OnInteractPressed 记账）</summary>
        private IEnumerator PortalRoutine(string sceneId)
        {
            finishing = true;
            session.Player.ControlLocked = true;
            for (float t = 0f; t < portalFadeSeconds; t += Time.deltaTime)
            {
                if (fadeToBlack != null) fadeToBlack.alpha = Mathf.Clamp01(t / portalFadeSeconds);
                yield return null;
            }
            if (fadeToBlack != null) fadeToBlack.alpha = 1f;
            services.Flow?.GoTo(sceneId);
        }

        /// <summary>走入终点区域 / 落到末菇指定点 → 渐黑 → 切 Finish Scene Id</summary>
        private IEnumerator FinishRoutine()
        {
            finishing = true;
            session.Player.ControlLocked = true;
            for (float t = 0f; t < finishFadeSeconds; t += Time.deltaTime)
            {
                if (fadeToBlack != null) fadeToBlack.alpha = Mathf.Clamp01(t / finishFadeSeconds);
                yield return null;
            }
            if (fadeToBlack != null) fadeToBlack.alpha = 1f;
            services.Flow?.GoTo(finishSceneId);
        }

        /// <summary>看客区演出：走入触发区 → 渐黑 → 人群消失 → 渐亮（人群凭空散场）</summary>
        private IEnumerator CrowdVanishRoutine()
        {
            crowdVanished = true;
            session.Player.ControlLocked = true;
            for (float t = 0f; t < crowdVanishFadeSeconds; t += Time.deltaTime)
            {
                if (fadeToBlack != null) fadeToBlack.alpha = Mathf.Clamp01(t / crowdVanishFadeSeconds);
                yield return null;
            }
            if (fadeToBlack != null) fadeToBlack.alpha = 1f;
            ambientCrowd?.VanishAll();
            Debug.Log("[Demo3] 看客人群已消失。", this);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) yield return null; // 全黑保持片刻
            for (float t = 0f; t < crowdVanishFadeSeconds; t += Time.deltaTime)
            {
                if (fadeToBlack != null) fadeToBlack.alpha = 1f - Mathf.Clamp01(t / crowdVanishFadeSeconds);
                yield return null;
            }
            if (fadeToBlack != null) fadeToBlack.alpha = 0f;
            session.Player.ControlLocked = false;
        }

        private void Update()
        {
            if (session == null) return;
            float dt = Time.deltaTime;
            session.Tick(dt, services.Input.Move, cameraRig.CameraBottomY);
            RefreshVisuals(); // 提示物显隐：散场/终点区按激活状态显现或隐藏（防误入的可视化）

            // 跳跃弧：速度由位移反推（与第二幕同规则），左右跳自动播左右动画；
            // States→Jump 填了状态名则全程播跳跃动画（预留位）
            girl.SetJumpAnim(session.Jumping);
            if (session.Jumping)
            {
                Vector2 p = JumpTrajectory.Eval(session.JumpFrom, session.JumpTo, session.JumpT, out float h);
                Vector2 gv = hasLastJumpPos ? (p - lastJumpPos) / Mathf.Max(dt, 1e-5f) : Vector2.zero;
                lastJumpPos = p; hasLastJumpPos = true;
                girl.Apply(p, gv);
                girl.SetVisualLift(h * jumpApexVisual);
            }
            else
            {
                hasLastJumpPos = false;
                girl.SetVisualLift(0f);
                girl.Apply(session.Player.Position, session.Player.Velocity);
            }
            cameraRig.Apply(session.Player.Position);
            ambientCrowd?.Tick(dt);

            if (finishing || session.Jumping) return; // 切场中/空中不检测触发区

            // 人群消失触发（看客场景）
            if (!crowdVanished && crowdVanishTrigger != null &&
                crowdVanishTrigger.bounds.Contains(session.Player.Position))
            {
                StartCoroutine(CrowdVanishRoutine());
                return;
            }

            // 走入式终点（落地状态走入才触发，跳跃掠过不算）
            // 防误入：配了人群消失演出且要求先播完时，演出没播前终点不激活
            if (finishTrigger != null && !string.IsNullOrEmpty(finishSceneId) &&
                finishTrigger.bounds.Contains(session.Player.Position))
            {
                if (crowdVanishTrigger != null && finishRequireCrowdVanished && !crowdVanished)
                {
                    if (!finishBlockedLogged)
                    {
                        finishBlockedLogged = true;
                        Debug.Log("[Demo3] Finish Trigger 暂未激活：人群消失演出还没播（先走进 Crowd Vanish Trigger），已拦住防误入。", this);
                    }
                    return;
                }
                StartCoroutine(FinishRoutine());
            }
        }

        /// <summary>提示物显隐同步：散场提示物在演出播完前显现；终点提示物在终点激活后显现。每帧调用，仅变化时 SetActive</summary>
        private void RefreshVisuals()
        {
            if (crowdVanishVisual != null)
            {
                bool showVanish = crowdVanishTrigger != null && !crowdVanished;
                if (crowdVanishVisual.activeSelf != showVanish) crowdVanishVisual.SetActive(showVanish);
            }
            if (finishVisual != null)
            {
                bool finishActive = finishTrigger != null &&
                                    !(crowdVanishTrigger != null && finishRequireCrowdVanished && !crowdVanished);
                if (finishVisual.activeSelf != finishActive) finishVisual.SetActive(finishActive);
            }
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
