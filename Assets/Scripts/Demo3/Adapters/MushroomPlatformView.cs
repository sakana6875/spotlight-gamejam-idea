using System.Collections;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 蘑菇平台视图（适配层）。
    /// 踩踏：任何角色都会触发凹陷回弹；
    /// 点亮：只有小女孩触发——花纹与顶缘发光层亮起。
    /// 可见性按身份配置：Hidden（男孩看不见未点亮的）/ Faint（女孩看见微弱轮廓）/ Lit。
    /// 纯表现组件，不决定游戏规则；踩踏事实由场景根从会话事件转发。
    /// </summary>
    public sealed class MushroomPlatformView : MonoBehaviour
    {
        public enum Visibility { Hidden = 0, Faint = 1, Lit = 2 }

        [SerializeField] private string platformId;
        [Tooltip("跳跃落点：拖入子物体 Point；不拖则用蘑菇自身位置")]
        [SerializeField] private Transform landingPoint;
        [SerializeField] private SpriteRenderer cap;          // 蘑菇顶
        [SerializeField] private SpriteRenderer patternGlow;  // 花纹发光层
        [Tooltip("未点亮素材（可选）：拖入后，未点亮的蘑菇显示这张图而不是消失/半透明；点亮后自动切回 Cap 原图")]
        [SerializeField] private Sprite unlitSprite;
        [SerializeField] private float faintAlpha = 0.18f;    // 微弱轮廓透明度（仅在没拖未点亮素材时生效）
        [SerializeField] private float squashDepth = 0.25f;   // 凹陷深度（Y 缩放）
        [SerializeField] private float squashTime = 0.35f;    // 回弹总时长

        private Sprite litSprite;   // 点亮后的图（Awake 缓存 Cap 原图）

        public string PlatformId => platformId;
        public bool IsLit { get; private set; }

        /// <summary>跳跃落点：拖了 Point 用 Point 的世界坐标，没拖退回蘑菇自身位置。</summary>
        public Vector2 LandingPos =>
            landingPoint != null ? (Vector2)landingPoint.position : (Vector2)transform.position;

        private Coroutine squashCo;

        private void Awake()
        {
            // 防呆1：Landing Point 留空时，自动找名叫 Point 的子物体
            if (landingPoint == null)
            {
                var found = transform.Find("Point");
                if (found != null)
                {
                    landingPoint = found;
                    Debug.Log($"[Demo3] {name} 自动找到落点子物体 Point，跳跃将落在 {found.position}", this);
                }
                else
                {
                    Debug.LogWarning($"[Demo3] {name} 没拖 Landing Point、也没有叫 Point 的子物体，" +
                                     $"跳跃会落在蘑菇根部 {transform.position}！" +
                                     $"请建一个名为 Point 的子物体放到蘑菇顶，或把它拖进 Landing Point 槽。", this);
                }
            }
            // 防呆2：Landing Point 拖成了蘑菇自己（等价于没拖，且无提示最难查）
            else if (landingPoint == transform)
            {
                Debug.LogError($"[Demo3] {name} 的 Landing Point 拖成了蘑菇自己！应拖子物体 Point。已自动忽略。", this);
                landingPoint = null;
            }
            if (cap != null) litSprite = cap.sprite; // 点亮后要切回的图
            Debug.Log($"[Demo3] MushroomPlatformView v10.18f 已加载（{name}，未点亮素材版）", this);
        }

        public void SetVisibility(Visibility v)
        {
            switch (v)
            {
                case Visibility.Hidden:
                    // 拖了未点亮素材：未点亮 = 显示未点亮图（不再消失）；没拖才保持旧行为（隐藏）
                    if (unlitSprite != null)
                    {
                        cap.enabled = true;
                        cap.sprite = unlitSprite;
                        SetAlpha(cap, 1f);
                    }
                    else cap.enabled = false;
                    if (patternGlow != null) patternGlow.enabled = false;
                    break;
                case Visibility.Faint:
                    cap.enabled = true;
                    if (unlitSprite != null) { cap.sprite = unlitSprite; SetAlpha(cap, 1f); }
                    else SetAlpha(cap, faintAlpha);
                    if (patternGlow != null) patternGlow.enabled = false;
                    break;
                case Visibility.Lit:
                    cap.enabled = true;
                    if (litSprite != null) cap.sprite = litSprite; // 切回点亮图
                    SetAlpha(cap, 1f);
                    if (patternGlow != null) { patternGlow.enabled = true; SetAlpha(patternGlow, 1f); }
                    break;
            }
        }

        /// <summary>踩踏入口：isGirl 决定是否点亮</summary>
        public void OnLanded(bool isGirl)
        {
            if (squashCo != null) StopCoroutine(squashCo);
            squashCo = StartCoroutine(SquashRoutine());
            if (!isGirl) return;          // 男孩只触发凹陷回弹
            IsLit = true;
            SetVisibility(Visibility.Lit); // 第三幕"再踩还会亮一下"
        }

        private IEnumerator SquashRoutine()
        {
            Vector3 s0 = Vector3.one;
            float half = squashTime * 0.5f;
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                float k = t / half;
                cap.transform.localScale = new Vector3(1f + 0.15f * k, 1f - squashDepth * k, 1f);
                yield return null;
            }
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                float k = t / half;
                cap.transform.localScale = Vector3.Lerp(
                    new Vector3(1.15f, 1f - squashDepth, 1f), s0, k);
                yield return null;
            }
            cap.transform.localScale = s0;
            squashCo = null;
        }

        private static void SetAlpha(SpriteRenderer r, float a)
        { var c = r.color; c.a = a; r.color = c; }
    }
}
