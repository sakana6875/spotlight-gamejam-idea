using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>L2 石碑点亮：以石碑为中心炸开一个完整球形防护罩（由小扩大）</summary>
    public sealed class ShieldBurstView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer shield;   // 圆形精灵
        [SerializeField] private float maxRadius = 4f;
        [SerializeField] private float expandTime = 0.8f;
        [SerializeField] private float holdTime = 1.2f;

        public void Play() => StartCoroutine(Routine());

        private IEnumerator Routine()
        {
            shield.enabled = true;
            for (float t = 0f; t < expandTime; t += Time.deltaTime)
            {
                float k = t / expandTime;
                shield.transform.localScale = Vector3.one * Mathf.Lerp(0.01f, maxRadius, k);
                SetA(1f);
                yield return null;
            }
            for (float t = 0f; t < holdTime; t += Time.deltaTime)
            { SetA(1f - t / holdTime); yield return null; }
            shield.enabled = false;
        }

        private void SetA(float a) { var c = shield.color; c.a = a; shield.color = c; }
    }

    /// <summary>L2 石碑点亮后：星星缓慢向下坠落（无限，用粒子系统循环即可）</summary>
    public sealed class StarfallView : MonoBehaviour
    {
        [SerializeField] private ParticleSystem stars; // 预配：向下、低速、循环、若隐若现
        public void Play() { if (stars != null) stars.Play(); }
    }

    /// <summary>
    /// L1 氛围：白色飞鱼在天空右侧盘旋，水面倒影同步镜像。
    /// 纯表现：每尾鱼绕各自圆心缓慢盘旋；倒影 = 关于水线的镜像 + 半透明。
    /// </summary>
    public sealed class FlyingFishView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] fishes;
        [SerializeField] private SpriteRenderer[] reflections; // 与 fishes 一一对应（或留空自动不管）
        [SerializeField] private float circleRadius = 2.5f;
        [SerializeField] private float angularSpeed = 0.4f;    // 慢慢盘旋
        [SerializeField] private float waterLineY = 0f;        // 水线高度（反射面）

        private readonly List<Vector2> centers = new List<Vector2>();
        private readonly List<float> phases = new List<float>();

        private void Start()
        {
            for (int i = 0; i < fishes.Length; i++)
            {
                centers.Add(fishes[i].transform.position);
                phases.Add(Random.value * Mathf.PI * 2f);
            }
        }

        private void Update()
        {
            for (int i = 0; i < fishes.Length; i++)
            {
                phases[i] += angularSpeed * Time.deltaTime;
                Vector2 p = centers[i] + new Vector2(Mathf.Cos(phases[i]), Mathf.Sin(phases[i])) * circleRadius;
                fishes[i].transform.position = p;
                if (reflections != null && i < reflections.Length && reflections[i] != null)
                {
                    float dy = p.y - waterLineY;
                    reflections[i].transform.position = new Vector2(p.x, waterLineY - dy);
                }
            }
        }
    }
}
