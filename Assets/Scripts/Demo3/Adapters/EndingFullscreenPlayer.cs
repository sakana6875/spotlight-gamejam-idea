using System;
using System.Collections;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 全屏纯动画结局播放器（适配层）：盖屏 Canvas + Animator。
    /// 两个结局各是一个动画状态（默认 Ending1=「还是你」 / Ending2=「是你吗？」，名字可在 Inspector 改），
    /// 控制器不用拉转换线——按状态名直切。流程：盖屏淡入 → 播对应结局动画 → 播完定格最后一帧（Demo 结束，
    /// 不再交还控制）。平时（Awake 起）自动隐藏，由场景根在结局判定后调用 Play。
    /// 与 EndingDirector（演员版演出）二选一：Act3TreeRoot 拖了本组件就优先用本组件。
    /// </summary>
    public sealed class EndingFullscreenPlayer : MonoBehaviour
    {
        [Header("全屏画布（Screen Space - Overlay，Sort Order 拉最高，盖过渐黑画布）")]
        [SerializeField] private CanvasGroup group;   // 全屏 Canvas 上的 CanvasGroup
        [SerializeField] private Animator animator;   // 铺满 Canvas 的 Image 上的 Animator

        [Header("结局动画状态名（与 Animator 根层级状态逐字一致）")]
        [SerializeField] private string ending1State = "Ending1"; // 结局一「还是你」
        [SerializeField] private string ending2State = "Ending2"; // 结局二「是你吗？」

        [Header("节奏")]
        [SerializeField] private float fadeInSeconds = 1.2f;   // 盖屏淡入时长
        [Tooltip("兜底上限：结局剪辑必须勾掉 Loop Time（循环播放检测不到结束），异常时最多等这么久")]
        [SerializeField] private float maxPlaySeconds = 180f;

        private void Awake()
        {
            // 平时隐藏：结局判定前玩家看不到这块画布
            if (group != null)
            {
                group.alpha = 0f;
                group.gameObject.SetActive(false);
            }
        }

        /// <summary>播结局全屏动画（由场景根在结局判定后调用；播完定格，不交还控制）。</summary>
        public void Play(Demo3Ending ending, Action onComplete) => StartCoroutine(Routine(ending, onComplete));

        private IEnumerator Routine(Demo3Ending ending, Action onComplete)
        {
            if (group == null || animator == null)
            {
                Debug.LogError("[Demo3] EndingFullscreenPlayer 缺少引用（Group / Animator）——播不了全屏结局。" +
                               "把 Canvas 的 CanvasGroup 和铺满画面的 Image 上的 Animator 拖进字段。", this);
                onComplete?.Invoke();
                yield break;
            }

            string stateName = ending == Demo3Ending.IsItYou ? ending2State : ending1State;
            int hash = Animator.StringToHash(stateName);

            group.gameObject.SetActive(true);
            // 先切到结局状态再淡入：淡入过程露出的就是结局动画本体（不会先闪一帧默认状态）
            if (!animator.HasState(0, hash))
                Debug.LogError($"[Demo3] 结局动画控制器里找不到状态 \"{stateName}\"——在控制器根层级建同名状态" +
                               $"（结局一 {ending1State} / 结局二 {ending2State}，名字可在本组件改），把全屏动画剪辑拖进去。", this);
            else animator.Play(hash, 0, 0f);

            for (float t = 0f; t < fadeInSeconds; t += Time.deltaTime)
            {
                group.alpha = Mathf.Clamp01(t / fadeInSeconds);
                yield return null;
            }
            group.alpha = 1f;

            // 等动画播完（轮询 normalizedTime；剪辑勾掉 Loop Time 后 normalizedTime 播到 1 即结束）
            yield return null;
            yield return null; // 让状态切换先生效一帧
            float waited = 0f;
            while (waited < maxPlaySeconds)
            {
                var info = animator.GetCurrentAnimatorStateInfo(0);
                if (info.shortNameHash == hash && info.normalizedTime >= 1f) break;
                waited += Time.deltaTime;
                yield return null;
            }
            if (waited >= maxPlaySeconds)
                Debug.LogWarning($"[Demo3] 结局动画 \"{stateName}\" 超过 {maxPlaySeconds} 秒还没播完——剪辑是不是勾了 Loop Time？勾掉它才会在末帧定格。", this);

            // 定格：不再交还控制（Demo 到此为止）
            onComplete?.Invoke();
        }
    }
}
