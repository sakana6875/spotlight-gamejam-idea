using System.Collections;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 蘑菇平台视图（适配层）。
    /// 踩踏：任何角色都会触发凹陷回弹；
    /// 点亮：只有小女孩触发——花纹与顶缘一圈发白光粒子（向上飘散）。
    /// 可见性按身份配置：Hidden（男孩看不见未点亮的）/ Faint（女孩看见微弱轮廓）/ Lit。
    /// 纯表现组件，不决定游戏规则；踩踏事实由场景根从会话事件转发。
    /// </summary>
    public sealed class MushroomPlatformView : MonoBehaviour
    {
        public enum Visibility { Hidden = 0, Faint = 1, Lit = 2 }

        [SerializeField] private string platformId;
        [SerializeField] private SpriteRenderer cap;          // 蘑菇顶
        [SerializeField] private SpriteRenderer patternGlow;  // 花纹发光层
        [SerializeField] private ParticleSystem burstParticles; // 白色粒子，向上飘散
        [SerializeField] private float faintAlpha = 0.18f;    // 微弱轮廓透明度
        [SerializeField] private float squashDepth = 0.25f;   // 凹陷深度（Y 缩放）
        [SerializeField] private float squashTime = 0.35f;    // 回弹总时长

        public string PlatformId => platformId;
        public bool IsLit { get; private set; }

        private Coroutine squashCo;

        public void SetVisibility(Visibility v)
        {
            switch (v)
            {
                case Visibility.Hidden:
                    cap.enabled = false;
                    if (patternGlow != null) patternGlow.enabled = false;
                    break;
                case Visibility.Faint:
                    cap.enabled = true;
                    SetAlpha(cap, faintAlpha);
                    if (patternGlow != null) patternGlow.enabled = false;
                    break;
                case Visibility.Lit:
                    cap.enabled = true;
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
            SetVisibility(Visibility.Lit); // 第三幕"再踩还会亮一下并放粒子"
            if (burstParticles != null) burstParticles.Play();
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
