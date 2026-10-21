using System;
using System.Collections;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 全屏纯动画播放器（适配层）：盖屏 Canvas + Animator，两种用法——
    /// ①结局（Play）：按判定播 Ending1「还是你」/ Ending2「是你吗？」，播完定格最后一帧，Demo 结束不交还控制；
    /// ②一次性演出（PlayOnce）：淡入盖屏 → 屏幕盖严时回调 onCovered（如销毁男孩）→ 播指定状态 →
    ///   播完淡出 → 收起画布 → 回调 onComplete（如大树送别演出，播完交还控制）。
    /// 控制器不用拉转换线——按状态名直切；状态名都可在 Inspector 改。
    /// 平时（Awake 起）画布自动隐藏。同一场景可放多个实例（如大树送别一个、结局一个），各配各的 Canvas+Animator。
    /// </summary>
    public sealed class FullscreenAnimPlayer : MonoBehaviour
    {
        [Header("全屏画布（Screen Space - Overlay，Sort Order 拉最高，盖过渐黑画布）")]
        [SerializeField] private CanvasGroup group;   // 全屏 Canvas 上的 CanvasGroup
        [SerializeField] private Animator animator;   // 铺满 Canvas 的 Image 上的 Animator

        [Header("结局动画状态名（Play 用；与 Animator 根层级状态逐字一致）")]
        [SerializeField] private string ending1State = "Ending1"; // 结局一「还是你」
        [SerializeField] private string ending2State = "Ending2"; // 结局二「是你吗？」

        [Header("节奏")]
        [SerializeField] private float fadeInSeconds = 1.2f;   // 盖屏淡入时长
        [Tooltip("一次性演出（PlayOnce）播完后的淡出时长；结局（Play）播完定格不淡出")]
        [SerializeField] private float fadeOutSeconds = 1.0f;
        [Tooltip("兜底上限：剪辑必须勾掉 Loop Time（循环播放检测不到结束），异常时最多等这么久")]
        [SerializeField] private float maxPlaySeconds = 180f;

        private void Awake()
        {
            // 平时隐藏：演出前玩家看不到这块画布
            if (group != null)
            {
                group.alpha = 0f;
                group.gameObject.SetActive(false);
            }
        }

        /// <summary>结局用：盖屏淡入 → 播对应结局动画 → 播完定格末帧（不再交还控制，Demo 结束）。</summary>
        public void Play(Demo3Ending ending, Action onComplete)
        {
            if (!CheckRefs()) { onComplete?.Invoke(); return; }
            StartCoroutine(Routine(ending == Demo3Ending.IsItYou ? ending2State : ending1State, fadeOut: false, null, onComplete));
        }

        /// <summary>
        /// 一次性演出用（如大树送别）：淡入盖屏 → 屏幕盖严回调 onCovered（此时做不可见切换，如销毁男孩）
        /// → 播 stateName → 播完淡出 → 收起画布 → onComplete（交还控制）。
        /// </summary>
        public void PlayOnce(string stateName, Action onCovered, Action onComplete)
        {
            if (!CheckRefs()) { onCovered?.Invoke(); onComplete?.Invoke(); return; }
            StartCoroutine(Routine(stateName, fadeOut: true, onCovered, onComplete));
        }

        private bool CheckRefs()
        {
            if (group != null && animator != null) return true;
            Debug.LogError("[Demo3] FullscreenAnimPlayer 缺少引用（Group / Animator）——播不了全屏动画。" +
                           "把 Canvas 的 CanvasGroup 和铺满画面的 Image 上的 Animator 拖进字段。", this);
            return false;
        }

        private IEnumerator Routine(string stateName, bool fadeOut, Action onCovered, Action onComplete)
        {
            int hash = Animator.StringToHash(stateName);
            group.gameObject.SetActive(true);
            // 先切到目标状态再淡入：淡入过程露出的就是演出本体（不会先闪一帧默认状态）
            if (!animator.HasState(0, hash))
                Debug.LogError($"[Demo3] 全屏动画控制器里找不到状态 \"{stateName}\"——在控制器根层级建同名状态，把全屏动画剪辑拖进去。", this);
            else animator.Play(hash, 0, 0f);

            yield return FadeTo(1f, fadeInSeconds);
            onCovered?.Invoke(); // 屏幕已盖严：此刻做不可见切换（销毁男孩等），玩家看不见

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
                Debug.LogWarning($"[Demo3] 全屏动画 \"{stateName}\" 超过 {maxPlaySeconds} 秒还没播完——剪辑是不是勾了 Loop Time？勾掉它才会在末帧结束。", this);

            if (fadeOut)
            {
                yield return FadeTo(0f, fadeOutSeconds);
                group.gameObject.SetActive(false); // 一次性演出播完收起画布，场景继续
            }
            // 结局（fadeOut=false）：定格末帧，画布保持盖屏，Demo 到此为止
            onComplete?.Invoke();
        }

        private IEnumerator FadeTo(float target, float seconds)
        {
            float start = group.alpha;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            group.alpha = target;
        }
    }
}
