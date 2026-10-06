using System;
using System.Collections;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 结局演出导演（表现层协程）。
    /// 共同段：女孩到树下 → 倚树抱腿埋头（与第一幕崩溃姿态一致）→ 画面渐暗。
    /// 结局一：黑屏后啜泣声渐起，Demo 结束。
    /// 结局二：大姐姐走近 → 蹲下摘帽给女孩戴上（露出相同发饰）→ 相拥 →
    ///         渐暗 → 大姐姐声音"Are you cold?" + 轻笑。
    /// 动画用 Animator Trigger（KneelDown / SisterApproach / SisterHat / SisterHug），
    /// 占位期只做位移与淡入淡出即可演示。
    /// </summary>
    public sealed class EndingDirector : MonoBehaviour
    {
        [SerializeField] private WalkerViewAdapter girl;
        [SerializeField] private WalkerViewAdapter sister;          // 结局二才用，可留空
        [SerializeField] private CanvasGroup fadeToBlack;
        [SerializeField] private AudioSource voiceSource;           // 啜泣 / "Are you cold?"
        [SerializeField] private AudioClip sobbingClip;
        [SerializeField] private AudioClip areYouColdClip;
        [SerializeField] private float fadeSeconds = 4f;

        private static readonly int KneelHash = Animator.StringToHash("KneelDown");
        private static readonly int HatHash = Animator.StringToHash("SisterHat");
        private static readonly int HugHash = Animator.StringToHash("SisterHug");

        public void Play(Demo3Ending ending, Action onComplete)
            => StartCoroutine(Routine(ending, onComplete));

        private IEnumerator Routine(Demo3Ending ending, Action onComplete)
        {
            Trigger(girl, KneelHash);
            yield return new WaitForSeconds(1.5f);

            if (ending == Demo3Ending.IsItYou && sister != null)
            {
                // 大姐姐走近 → 摘帽 → 相拥
                Vector2 girlPos = girl.transform.position;
                Vector2 from = girlPos + new Vector2(6f, 1.5f);
                sister.Bind(from);
                Vector2 near = girlPos + new Vector2(0.9f, 0.2f);
                while (((Vector2)sister.transform.position - near).sqrMagnitude > 0.02f)
                {
                    Vector2 p = Vector2.MoveTowards(sister.transform.position, near, 1.2f * Time.deltaTime);
                    sister.Apply(p, (near - p).normalized * 1.2f);
                    yield return null;
                }
                sister.Apply(near, Vector2.zero);
                yield return new WaitForSeconds(0.8f);
                Trigger(sister, HatHash);       // 摘帽给女孩戴上（发饰露出的特写在动画里）
                yield return new WaitForSeconds(2.0f);
                Trigger(sister, HugHash);
                yield return new WaitForSeconds(1.5f);
            }

            yield return Fade(1f, fadeSeconds);

            if (voiceSource != null)
            {
                voiceSource.clip = ending == Demo3Ending.IsItYou ? areYouColdClip : sobbingClip;
                if (voiceSource.clip != null) voiceSource.Play();
            }
            yield return new WaitForSeconds(3f);

            onComplete?.Invoke();
        }

        private IEnumerator Fade(float target, float seconds)
        {
            if (fadeToBlack == null) yield break;
            float start = fadeToBlack.alpha;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                fadeToBlack.alpha = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            fadeToBlack.alpha = target;
        }

        private static void Trigger(WalkerViewAdapter w, int hash)
        {
            var anim = w != null ? w.GetComponentInChildren<Animator>() : null;
            if (anim != null) anim.SetTrigger(hash);
        }
    }
}
