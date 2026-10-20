using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 大树场景根（Act3，枢纽场景）——第二幕跳完最后一朵蘑菇后进入。
    /// 第一次进入：男孩还在（HandholdFollower 牵手跟随女孩），玩家把两人牵进大树触发器 →
    ///             渐黑 → 全黑中销毁男孩、只剩女孩 → 渐亮回来，留在枢纽场景自由行动；
    /// 之后每次进入：男孩自动移除（只有女孩），按 Progress.EntryCount("Act3") 打日志区分幕次（结局分支用）。
    /// 自动走位区（Auto Walk Zones，可放任意多个）：玩家走进划定的区域 → 锁输入自动走到目标点
    /// （如大树出发点）→ 到达后可切场景（如 Act2-1 开始第三幕蘑菇跳跃）或只走位。
    /// 防误入：Require Tree Done 勾上的区域，在男孩退场演出播完前不激活（不触发、提示物隐藏），
    /// 演出播完才激活并显现提示物——避免男孩还在场时玩家误入提前切场。
    /// </summary>
    public sealed class Act3TreeRoot : ActSceneRootBase
    {
        [Serializable]
        public sealed class AutoWalkZone
        {
            [Tooltip("划定区域：空物体挂 BoxCollider2D（Is Trigger 勾上）")]
            public Collider2D Zone;
            [Tooltip("自动走位目标点（如大树出发点）：放一个空物体")]
            public Transform Target;
            [Tooltip("到达后切换的场景名（Build Settings 里的名字，如 Act2-1；留空则只走位不切场景）")]
            public string GoToSceneId = "";
            [Tooltip("只触发一次（不勾则每次走进区域都会触发）")]
            public bool Once = true;
            [Tooltip("需要先完成大树演出（男孩退场）才激活——防止男孩还在场时误入提前切场；" +
                     "第 2 次及以后进大树场景（男孩本就不在）自动视为已满足")]
            public bool RequireTreeDone = true;
            [Tooltip("可选提示物（光圈/箭头/发光标记等）：区域激活时才显现，未激活或已消费（Once 触发过）自动隐藏")]
            public GameObject ActiveVisual;
            [NonSerialized] public bool Fired;
            [NonSerialized] public bool BlockedLogged;
            [NonSerialized] public bool WasInside; // 上一帧玩家是否在判定范围内（进区即时诊断用）
            [NonSerialized] public bool NearLogged; // 已报过一次「走近但没进判定」
        }

        [Header("角色")]
        [SerializeField] private WalkerViewAdapter girl;
        [Tooltip("男孩：仅第一次进大树场景在场（牵手跟随女孩）；之后进入自动移除")]
        [SerializeField] private WalkerViewAdapter boy;
        [SerializeField] private HandholdFollower handhold;

        [Header("场景")]
        [SerializeField] private ActCameraRig cameraRig;
        [Tooltip("活动范围：拖一个带 Collider2D 的空物体（用其包围盒），必须罩住出生点、大树与所有走位区")]
        [SerializeField] private Collider2D walkBoundsArea;
        [SerializeField] private float walkSpeed = 3f;

        [Header("大树收尾（仅第一次进入）")]
        [Tooltip("大树触发器：空物体挂 BoxCollider2D（Is Trigger 勾上）放在大树位置——两人走进 → 渐黑 → 男孩销毁只剩女孩")]
        [SerializeField] private Collider2D treeTrigger;
        [SerializeField] private CanvasGroup fadeToBlack;
        [SerializeField] private float treeFadeSeconds = 1.5f;      // 渐黑/渐亮时长
        [SerializeField] private float treeBlackHoldSeconds = 0.6f; // 全黑保持（男孩在此期间销毁）
        [Tooltip("进场时从黑场渐亮（上一幕是渐黑收尾的，勾上衔接自然）")]
        [SerializeField] private bool fadeInOnStart = true;

        [Header("自动走位区（走进区域 → 自动走到目标点 → 到达后可选切场景）")]
        [SerializeField] private List<AutoWalkZone> autoWalkZones = new List<AutoWalkZone>();

        [Header("场景变体（按第几次进大树场景自动切换）")]
        [Tooltip("仅第 1 次进入激活的物体（男孩相关等）——第 2 次起自动失活")]
        [SerializeField] private List<GameObject> firstVisitOnlyObjects = new List<GameObject>();
        [Tooltip("仅第 2 次及以后进入激活的物体（结局相关等）——第 1 次自动失活")]
        [SerializeField] private List<GameObject> laterVisitOnlyObjects = new List<GameObject>();

        [Header("结局（仅第 2 次及以后进入激活；第 1 次进入失活防误触发）")]
        [Tooltip("结局触发区：空物体挂 BoxCollider2D（Is Trigger 勾上）放在大树处。女孩重回大树走进此区 → 判定并播放结局")]
        [SerializeField] private Collider2D endingTrigger;
        [Tooltip("结局提示物（可选，光圈/箭头等）：结局区激活时显现，未激活（第 1 次进入）或已触发自动隐藏")]
        [SerializeField] private GameObject endingVisual;
        [Tooltip("结局判定所需旗标 ID 清单（如 L1、L2、Visited_L1、Visited_L2）。全亮=结局二「是你吗？」，否则=结局一「还是你」；" +
                 "留空则恒为结局二（想要收集分支就填上）")]
        [SerializeField] private List<string> requiredTablets = new List<string>();
        [Tooltip("结局导演（可选）：拖 EndingDirector 播结局演出（女孩倚树/大姐姐相拥/语音）；不拖则渐黑定格即结局")]
        [SerializeField] private EndingDirector endingDirector;

        private IDemo3Services services;
        private ActWalkSession player;
        private bool boyPresent;
        private bool firstVisit = true; // 本次是否第 1 次进入（结局区按它失活/激活）
        private bool treeDone;       // 大树收尾演出已播
        private bool autoWalking;
        private Transform autoTarget;
        private string autoGoScene = "";
        private bool endingFired;         // 结局已触发（防重入）
        private bool endingBlockedLogged; // 结局区被拦已报过一次

        public override void Configure(IDemo3Services s) => services = s;

        private void Start()
        {
            Debug.Log("[Demo3] Act3TreeRoot v10.19j 已加载（大树枢纽：走位区坐标对质诊断版）", this);
            StartCoroutine(InitWhenReady());
        }

        private IEnumerator InitWhenReady()
        {
            float waited = 0f;
            while (services == null && waited < 3f) { waited += Time.unscaledDeltaTime; yield return null; }
            if (services == null)
            {
                Debug.LogError("[Demo3] 大树场景需要 Demo3FlowCarrier 在场（从第一幕进入，或放一个临时载体）。");
                enabled = false;
                yield break;
            }
            if (girl == null || cameraRig == null || walkBoundsArea == null)
            {
                Debug.LogError("[Demo3] Act3TreeRoot 缺少引用：" +
                               (girl == null ? "Girl、" : "") + (cameraRig == null ? "CameraRig、" : "") +
                               (walkBoundsArea == null ? "WalkBoundsArea（拖带 Collider2D 的空物体）" : "") +
                               "。补齐后重进场景。", this);
                enabled = false;
                yield break;
            }
            Rect walkBounds = new Rect(walkBoundsArea.bounds.min, walkBoundsArea.bounds.size);

            int visit = services.Progress != null ? services.Progress.EntryCount(Demo3SceneIds.Act3Pursue) : 1;
            bool first = visit <= 1; firstVisit = first;
            Debug.Log($"[Demo3] 大树场景第 {visit} 次进入（来源场景：{services.Progress?.LastSceneId ?? "未知"}），" +
                      (first ? "男孩在场——把两人牵进大树触发器播退场演出；结局区此时失活防误触发。"
                             : "只剩女孩（男孩已退场）；结局区现已激活——走进即演出结局。"), this);

            // 变体清单：按进入次数切换物体 失活/出现
            foreach (var go in firstVisitOnlyObjects) if (go != null) go.SetActive(first);
            foreach (var go in laterVisitOnlyObjects) if (go != null) go.SetActive(!first);

            boyPresent = first && boy != null;
            if (first && boy == null)
                Debug.LogWarning("[Demo3] 第一次进大树场景但 Boy 字段没拖——没有男孩可退场，大树演出不会播。", this);
            if (!first && boy != null) Destroy(boy.gameObject); // 后续进入：男孩已不在
            if (first && treeTrigger == null)
                Debug.LogWarning("[Demo3] Tree Trigger 未拖：两人到大树不会有「全黑 → 男孩退场」演出。", this);
            if (boyPresent && handhold == null)
                Debug.LogError("[Demo3] Handhold 字段没拖！男孩不会跟随女孩（两人各走各的、相隔很远）。" +
                               "把场景里的 HandholdFollower 物体（如 Girl 的子物体 Handhold）拖进来。", this);
            for (int i = 0; i < autoWalkZones.Count; i++)
            {
                var z = autoWalkZones[i];
                if (z != null && (z.Zone == null || z.Target == null))
                    Debug.LogWarning($"[Demo3] Auto Walk Zones 第 {i + 1} 条没配全（Zone/Target 缺一不可）" +
                                     "——这条会被静默跳过，走进区域毫无反应。补齐或删掉它。", this);
            }
            if (!first && endingTrigger == null)
                Debug.LogWarning("[Demo3] 第 2 次及以后进大树场景但 Ending Trigger 没拖——重回大树不会触发结局。在大树处放一个空物体碰撞体拖进来。", this);
            if (endingTrigger != null && requiredTablets.Count == 0)
                Debug.LogWarning("[Demo3] Required Tablets 为空：结局判定恒为结局二「是你吗？」。填入隐藏石碑 ID（如 L1、L2 / Visited_L1、Visited_L2）才有双结局分支。", this);
            if (!walkBounds.Contains(girl.transform.position))
                Debug.LogError($"[Demo3] 大树场景的 Walk Bounds Area（{walkBounds}）没罩住女孩出生点 " +
                               $"{(Vector2)girl.transform.position}——第一帧她会被钳到边缘。拉大该碰撞体即可。", this);

            // 触发区必须落在 Walk Bounds 内：在框外时玩家被钳制永远走不进去，触发区形同虚设（典型「走进区毫无反应」）
            CheckAreaInside(walkBounds, treeTrigger, "Tree Trigger");
            CheckAreaInside(walkBounds, endingTrigger, "Ending Trigger");
            for (int i = 0; i < autoWalkZones.Count; i++)
            {
                var z = autoWalkZones[i];
                if (z != null && z.Zone != null)
                {
                    CheckAreaInside(walkBounds, z.Zone, $"Auto Walk Zones 第 {i + 1} 条");
                    if (z.Target != null)
                        Debug.Log($"[Demo3] 走位区第 {i + 1} 条「{z.Target.name}」判定包围盒：中心 {(Vector2)z.Zone.bounds.center}，" +
                                  $"尺寸 {(Vector2)z.Zone.bounds.size}；目标点 {(Vector2)z.Target.position}。" +
                                  $"（女孩出生点 {(Vector2)girl.transform.position}，Walk Bounds {walkBounds}）", this);
                }
            }

            player = new ActWalkSession(girl.transform.position, walkSpeed) { Bounds = walkBounds };
            girl.Bind(player.Position);
            if (boyPresent)
            {
                boy.Bind(boy.transform.position);
                if (handhold != null)
                {
                    handhold.SetFollower(boy.transform);
                    handhold.Snap(player.Position, boy.transform.position);
                    handhold.Active = true;
                }
            }

            // 从黑场渐亮（衔接上一幕的渐黑收尾）
            if (fadeInOnStart && fadeToBlack != null)
            {
                fadeToBlack.alpha = 1f;
                for (float t = 0f; t < treeFadeSeconds; t += Time.deltaTime)
                {
                    fadeToBlack.alpha = 1f - Mathf.Clamp01(t / treeFadeSeconds);
                    yield return null;
                }
                fadeToBlack.alpha = 0f;
            }
        }

        private void Update()
        {
            if (player == null) return;
            float dt = Time.deltaTime;

            RefreshZoneVisuals(); // 提示物显隐：区域激活且未消费才显现（男孩退场前需要演出的区保持隐藏）

            // 结局区：第 1 次进入失活（防误触发结局）；第 2 次及以后进入激活，女孩走进即判定并演出结局
            if (endingTrigger != null && !endingFired && !autoWalking && !player.ControlLocked
                && endingTrigger.bounds.Contains(player.Position))
            {
                if (firstVisit)
                {
                    if (!endingBlockedLogged)
                    { endingBlockedLogged = true; Debug.Log("[Demo3] 结局触发区暂未激活：这是第 1 次进大树场景（剧情还没到结局——已拦住防误入。完成第三幕与第四幕后重回大树再走进这里）。"); }
                }
                else StartCoroutine(EndingRoutine());
            }

            // 自动走位区：走进划定区域 → 锁输入，自动走到目标点
            // 防误入：RequireTreeDone 的区域在男孩退场前（boyPresent=true 即演出未播）不激活
            if (!autoWalking && !player.ControlLocked)
            {
                foreach (var z in autoWalkZones)
                {
                    if (z == null || z.Zone == null || z.Target == null) continue;
                    // 进区即时诊断：走进判定范围的当帧报一条，并说明接下来会触发/被拦/已消费——
                    // 没这条日志=根本没走进判定（查 Walk Bounds 是否罩住该区、是否拖进了列表）
                    bool insideNow = z.Zone.bounds.Contains(player.Position);
                    if (insideNow && !z.WasInside)
                        Debug.Log($"[Demo3] 检测到你走进走位区「{z.Target.name}」的判定范围" +
                                  (z.Once && z.Fired ? "（本条 Once 已消费过，不再触发）" :
                                   z.RequireTreeDone && boyPresent ? "（男孩未退场，本条被 Require Tree Done 拦住）" :
                                   "，开始自动走位。"), this);
                    else if (!insideNow && !z.NearLogged)
                    {
                        // 走近（2.5 米内）但没进判定：把双方坐标打出来对质——
                        // 视觉站在绿框里却没日志 = 绿框物体的碰撞体没罩住绿框（尺寸/偏移不对），按坐标调整
                        Vector2 pp = player.Position;
                        Bounds zb = z.Zone.bounds;
                        float dx = Mathf.Max(zb.min.x - pp.x, 0f, pp.x - zb.max.x);
                        float dy = Mathf.Max(zb.min.y - pp.y, 0f, pp.y - zb.max.y);
                        if (dx * dx + dy * dy < 2.5f * 2.5f)
                        {
                            z.NearLogged = true;
                            Debug.Log($"[Demo3] 你走到了走位区「{z.Target.name}」附近但没进判定：" +
                                      $"你的逻辑位置 {pp}，判定包围盒 中心 {(Vector2)zb.center} 尺寸 {(Vector2)zb.size}。" +
                                      "视觉上若已站进绿框＝碰撞体和绿框不重合（Offset/Size 调一下）。", this);
                        }
                    }
                    if (insideNow) z.NearLogged = false;
                    z.WasInside = insideNow;
                    if (z.Once && z.Fired) continue;
                    if (z.RequireTreeDone && boyPresent)
                    {
                        // 玩家正站在未激活的区里：报一次为什么没反应，避免误以为坏了
                        if (!z.BlockedLogged && z.Zone.bounds.Contains(player.Position))
                        {
                            z.BlockedLogged = true;
                            Debug.Log($"[Demo3] 走位区「{(z.Target != null ? z.Target.name : "?")}」暂未激活：" +
                                      "男孩还没退场（先把两人牵进大树触发器播完退场演出），已拦住防误入。", this);
                        }
                        continue;
                    }
                    if (!z.Zone.bounds.Contains(player.Position)) continue;
                    z.Fired = true;
                    autoWalking = true;
                    autoTarget = z.Target;
                    autoGoScene = z.GoToSceneId;
                    player.ControlLocked = true;
                    Debug.Log($"[Demo3] 走进自动走位区，自动走向 {z.Target.position}" +
                              (string.IsNullOrEmpty(z.GoToSceneId) ? "。" : $"，到达后切场景 {z.GoToSceneId}。"), this);
                    break;
                }
            }

            if (autoWalking)
            {
                if (player.AutoStepTo(autoTarget.position, dt))
                {
                    autoWalking = false;
                    if (!string.IsNullOrEmpty(autoGoScene)) StartCoroutine(GoRoutine(autoGoScene));
                    else player.ControlLocked = false;
                }
            }
            else player.Tick(dt, services.Input.Move);

            girl.Apply(player.Position, player.Velocity);
            if (boyPresent && boy != null && handhold != null)
            {
                handhold.SetLeaderPos(player.Position);
                boy.Apply(handhold.FollowerPos, player.Velocity);
            }
            cameraRig.Apply(player.Position);

            // 大树收尾：两人走进触发器 → 渐黑 → 全黑中销毁男孩 → 渐亮回来，只剩女孩（不切场景）
            if (boyPresent && !treeDone && treeTrigger != null && treeTrigger.bounds.Contains(player.Position))
                StartCoroutine(TreeRoutine());
        }

        /// <summary>大树收尾演出：渐黑 → 男孩销毁（只剩女孩）→ 黑场片刻 → 渐亮回来，留在枢纽场景</summary>
        private IEnumerator TreeRoutine()
        {
            treeDone = true;
            player.ControlLocked = true;
            for (float t = 0f; t < treeFadeSeconds; t += Time.deltaTime)
            {
                if (fadeToBlack != null) fadeToBlack.alpha = Mathf.Clamp01(t / treeFadeSeconds);
                yield return null;
            }
            if (fadeToBlack != null) fadeToBlack.alpha = 1f;
            if (handhold != null) handhold.Active = false;
            if (boy != null) Destroy(boy.gameObject);
            boyPresent = false;
            Debug.Log("[Demo3] 大树演出播完：男孩已退场，只剩女孩。需要演出才激活的走位区现已开放（提示物显现）。", this);
            for (float t = 0f; t < treeBlackHoldSeconds; t += Time.deltaTime) yield return null;
            for (float t = 0f; t < treeFadeSeconds; t += Time.deltaTime)
            {
                if (fadeToBlack != null) fadeToBlack.alpha = 1f - Mathf.Clamp01(t / treeFadeSeconds);
                yield return null;
            }
            if (fadeToBlack != null) fadeToBlack.alpha = 0f;
            player.ControlLocked = false;
        }

        /// <summary>
        /// 走位区提示物显隐同步：激活（无需演出 或 男孩已退场）且未消费（Once 未触发过）才显现。
        /// 每帧调用但只在状态变化时才 SetActive，开销可忽略。
        /// </summary>
        private void RefreshZoneVisuals()
        {
            foreach (var z in autoWalkZones)
            {
                if (z == null || z.ActiveVisual == null) continue;
                bool show = !(z.RequireTreeDone && boyPresent) && !(z.Once && z.Fired);
                if (z.ActiveVisual.activeSelf != show) z.ActiveVisual.SetActive(show);
            }
            if (endingVisual != null)
            {
                bool showEnding = endingTrigger != null && !firstVisit && !endingFired;
                if (endingVisual.activeSelf != showEnding) endingVisual.SetActive(showEnding);
            }
        }

        /// <summary>
        /// 结局演出：仅第 2 次及以后进入大树场景才会走到这里（第 1 次在 Update 就被拦住）。
        /// 按隐藏石碑点亮情况判定结局（EndingRules），有 EndingDirector 就交给它演出（它自己掌管渐黑，根不预渐黑以免闪屏）；
        /// 没有导演则根渐黑停住（Demo 到此为止，不再交还控制）。
        /// </summary>
        private IEnumerator EndingRoutine()
        {
            endingFired = true;
            player.ControlLocked = true;
            if (handhold != null) handhold.Active = false;

            Demo3Ending ending = EndingRules.Decide(requiredTablets,
                id => services.Flags != null && services.Flags.IsLit(id));
            var miss = new StringBuilder();
            foreach (var id in requiredTablets)
                if (services.Flags == null || !services.Flags.IsLit(id)) miss.Append(miss.Length == 0 ? id : "、" + id);
            Debug.Log($"[Demo3] 结局触发：判定为 {Demo3EndingName(ending)}。" +
                      (ending == Demo3Ending.IsItYou ? "全部隐藏石碑都已点亮。" : $"未点亮的石碑：{(miss.Length > 0 ? miss.ToString() : "（未配置 Required Tablets，按结局一处理）")}"), this);

            if (endingDirector != null)
            {
                endingDirector.Play(ending, () => Debug.Log("[Demo3] 结局演出播完（Demo 结束）。", this));
                yield break;
            }

            // 无导演兜底：渐黑停住
            for (float t = 0f; t < treeFadeSeconds; t += Time.deltaTime)
            {
                if (fadeToBlack != null) fadeToBlack.alpha = Mathf.Clamp01(t / treeFadeSeconds);
                yield return null;
            }
            if (fadeToBlack != null) fadeToBlack.alpha = 1f;
            Debug.LogWarning("[Demo3] 结局演出未拖 EndingDirector——只做了渐黑停住。拖入 EndingDirector 才有完整演出。", this);
        }

        private static string Demo3EndingName(Demo3Ending ending)
            => ending == Demo3Ending.IsItYou ? "结局二「是你吗？」" : "结局一「还是你」";

        /// <summary>自检：触发区必须落在 Walk Bounds 内，否则玩家被钳制永远走不进去（完全在外=红字，部分在外=黄字）</summary>
        private void CheckAreaInside(Rect walkBounds, Collider2D area, string what)
        {
            if (area == null) return;
            Bounds ab = area.bounds;
            bool minIn = walkBounds.Contains(ab.min);
            bool maxIn = walkBounds.Contains(ab.max);
            if (!minIn && !maxIn)
                Debug.LogError($"[Demo3] {what}（{area.name}，包围盒 {ab}）完全在 Walk Bounds Area（{walkBounds}）之外" +
                               "——女孩被钳制永远走不进它，这个触发区形同虚设。把 Walk Bounds Area 拉大罩住它（或把它挪回活动范围内）。", this);
            else if (!minIn || !maxIn)
                Debug.LogWarning($"[Demo3] {what}（{area.name}）只有一部分在 Walk Bounds Area 内——玩家只能走进重叠部分，" +
                                 "很难触发。建议把 Walk Bounds Area 拉大罩住整个区域。", this);
        }

        /// <summary>自动走位到达后的切场：渐黑 → GoTo</summary>
        private IEnumerator GoRoutine(string sceneId)
        {
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                if (fadeToBlack != null) fadeToBlack.alpha = Mathf.Clamp01(t / 0.8f);
                yield return null;
            }
            if (fadeToBlack != null) fadeToBlack.alpha = 1f;
            services.Flow?.GoTo(sceneId);
        }
    }
}
