using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 区域名称横幅（纯表现）：在屏幕右侧纵向显示地域名，淡入 → 停留 2s → 淡出。
    /// 需求：进入区域时触发（正反向都算），看客区域不显示；字体无特殊要求。
    /// 搭建：Canvas(Screen Space - Overlay) → 右侧中部放一个 Text 并挂在 CanvasGroup 下，
    ///       把两个引用拖给本组件即可。
    /// </summary>
    public sealed class AreaNameBannerView : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float fadeInSeconds = 0.3f;
        [SerializeField] private float holdSeconds = 2f;   // 需求：显示时间暂定 2s
        [SerializeField] private float fadeOutSeconds = 0.4f;

        private Coroutine routine;

        private void Awake()
        {
            if (group != null) group.alpha = 0f;
        }

        /// <summary>由 AreaNameAnnouncer.Entered 调用；重复触发会重新开始一轮显示</summary>
        public void Show(string areaName)
        {
            if (label == null || group == null || string.IsNullOrEmpty(areaName)) return;
            label.text = Verticalize(areaName);
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(ShowRoutine());
        }

        // 纵向排列：逐字换行（最简单的竖排实现，不依赖字体特性）
        private static string Verticalize(string s)
        {
            var sb = new StringBuilder(s.Length * 2);
            for (int i = 0; i < s.Length; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        private IEnumerator ShowRoutine()
        {
            for (float t = 0f; t < fadeInSeconds; t += Time.deltaTime)
            {
                group.alpha = t / fadeInSeconds;
                yield return null;
            }
            group.alpha = 1f;
            yield return new WaitForSeconds(holdSeconds);
            for (float t = 0f; t < fadeOutSeconds; t += Time.deltaTime)
            {
                group.alpha = 1f - t / fadeOutSeconds;
                yield return null;
            }
            group.alpha = 0f;
            routine = null;
        }
    }
}
