using System.Collections;
using Healing.Demo3.Application;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 隐藏区域场景根（Hidden1 / Hidden2 两个独立小场景共用）：
    ///  1. 进场自动在全局进度里记录"进入过本隐藏区"（Progress.MarkHiddenVisited，结局分支用），
    ///     并写一份 Visited_{id} 到石碑旗标——EndingRules 的 RequiredTablets 可直接拿它判定结局；
    ///  2. 女孩（WalkerViewAdapter）在 Walk Bounds Area 内自由 WASD 走动（Mode=Free 的小场景）；
    ///  3. 石碑：走进 Interact Radius 按 E → 点亮记账（跨幕携带）→ 渐黑 → 返回来源场景
    ///     （来源场景由 Act3Root 在传送前记账返回点，女孩原地复活）；
    ///  4. 备选出口：也可拖 Exit Trigger（走入即返回，不勾石碑）；两者留一个即可。
    /// </summary>
    public sealed class HiddenAreaRoot : ActSceneRootBase
    {
        [Header("身份")]
        [Tooltip("隐藏区 ID：与石碑 Id 对应（L1 / L2），全局进度与结局判定都用它")]
        [SerializeField] private string hiddenId = "L1";
        [Tooltip("出口指向的场景名（Build Settings 里的名字；隐藏区从第三幕进，默认回 Act2-1）")]
        [SerializeField] private string returnSceneId = "Act2-1";

        [Header("角色与相机")]
        [SerializeField] private WalkerViewAdapter girl;
        [SerializeField] private ActCameraRig cameraRig;
        [Tooltip("活动范围：拖一个带 Collider2D 的空物体（用其包围盒），必须罩住出生点")]
        [SerializeField] private Collider2D walkBoundsArea;
        [SerializeField] private float walkSpeed = 3.4f;

        [Header("交互与出口")]
        [SerializeField] private StoneTabletView tablet;            // 按 E：点亮 + 返回来源场景
        [Tooltip("备选出口（可留空）：空物体挂 BoxCollider2D（Is Trigger 勾上），走进即返回")]
        [SerializeField] private Collider2D exitTrigger;
        [SerializeField] private CanvasGroup fadeToBlack;           // 可留空（留空直接切）
        [SerializeField] private float fadeSeconds = 0.8f;
        [Tooltip("进场时从黑幕渐亮（上一场景是渐黑收尾的，建议勾上）")]
        [SerializeField] private bool fadeInOnStart = true;

        private IDemo3Services services;
        private ActWalkSession player;
        private bool leaving;

        public override void Configure(IDemo3Services s) => services = s;

        private void Start()
        {
            Debug.Log($"[Demo3] HiddenAreaRoot v10.19b 已加载（拖碰撞体+石碑按E返回版，隐藏区 {hiddenId}，出口→{returnSceneId}）", this);
            StartCoroutine(InitWhenReady());
        }

        private IEnumerator InitWhenReady()
        {
            float waited = 0f;
            while (services == null && waited < 3f) { waited += Time.unscaledDeltaTime; yield return null; }
            if (services == null)
            {
                Debug.LogError($"[Demo3] 隐藏区 {hiddenId} 需要 Demo3FlowCarrier 在场（从第一幕进入，或放一个临时载体）。");
                enabled = false;
                yield break;
            }
            if (girl == null || cameraRig == null || walkBoundsArea == null)
            {
                Debug.LogError($"[Demo3] 隐藏区 {hiddenId} 缺少引用：" +
                               (girl == null ? "Girl、" : "") + (cameraRig == null ? "CameraRig、" : "") +
                               (walkBoundsArea == null ? "WalkBoundsArea（拖带 Collider2D 的空物体）" : "") +
                               "。补齐后重进场景。", this);
                enabled = false;
                yield break;
            }
            Rect walkBounds = new Rect(walkBoundsArea.bounds.min, walkBoundsArea.bounds.size);

            // 全局参数①：进入过本隐藏区（结局分支用）
            services.Progress?.MarkHiddenVisited(hiddenId);
            // 全局参数②：同步写一份到石碑旗标（Visited_L1 / Visited_L2），EndingRules 可直接判定
            services.Flags?.MarkLit("Visited_" + hiddenId);

            if (!walkBounds.Contains(girl.transform.position))
                Debug.LogError($"[Demo3] 隐藏区 {hiddenId} 的 Walk Bounds Area（{walkBounds}）没罩住女孩出生点 " +
                               $"{(Vector2)girl.transform.position}——第一帧她会被钳到边缘。拉大该碰撞体即可。", this);
            if (exitTrigger == null && tablet == null)
                Debug.LogWarning($"[Demo3] 隐藏区 {hiddenId} 的 Exit Trigger 与 Tablet 都没拖：玩家将无法离开本场景。", this);

            player = new ActWalkSession(girl.transform.position, walkSpeed) { Bounds = walkBounds };
            girl.Bind(player.Position);
            services.Input.InteractPressed += OnInteract;

            // 进场渐亮（上一场景是渐黑收尾的）
            if (fadeInOnStart && fadeToBlack != null)
            {
                fadeToBlack.alpha = 1f;
                for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
                {
                    fadeToBlack.alpha = 1f - Mathf.Clamp01(t / fadeSeconds);
                    yield return null;
                }
                fadeToBlack.alpha = 0f;
            }
        }

        private void OnInteract()
        {
            if (tablet == null || player == null || leaving) return;
            if (!tablet.IsInRange(player.Position)) return;
            // 点亮记账（跨幕携带；已点亮则只放演出）→ 渐黑 → 返回来源场景
            if (services.Flags != null && !services.Flags.IsLit(tablet.TabletId))
            {
                services.Flags.MarkLit(tablet.TabletId);
                services.Bus.Publish(new TabletLitEvent(tablet.TabletId));
            }
            tablet.PlayLitEffect();
            Debug.Log($"[Demo3] 隐藏区 {hiddenId} 的石碑已交互，返回 {returnSceneId}。", this);
            StartCoroutine(LeaveRoutine());
        }

        private void Update()
        {
            if (player == null) return;
            float dt = Time.deltaTime;
            player.Tick(dt, services.Input.Move);
            girl.Apply(player.Position, player.Velocity);
            cameraRig.Apply(player.Position);
            if (!leaving && exitTrigger != null && exitTrigger.bounds.Contains(player.Position))
                StartCoroutine(LeaveRoutine());
        }

        private IEnumerator LeaveRoutine()
        {
            leaving = true;
            player.ControlLocked = true;
            for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
            {
                if (fadeToBlack != null) fadeToBlack.alpha = Mathf.Clamp01(t / fadeSeconds);
                yield return null;
            }
            if (fadeToBlack != null) fadeToBlack.alpha = 1f;
            services.Flow?.GoTo(returnSceneId);
        }

        private void OnDestroy()
        {
            if (services != null) services.Input.InteractPressed -= OnInteract;
        }
    }
}
