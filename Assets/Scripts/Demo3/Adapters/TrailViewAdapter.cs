using System.Collections.Generic;
using Healing.Demo3.Domain;
using UnityEngine;
using UnityEngine.Pool;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 轨迹视图：脚印/水纹的对象池渲染（UnityEngine.Pool）。
    /// 脚印：小精灵贴图，左右脚交替偏移，随时间缓慢变淡；
    /// 水纹：圆环精灵，生成后扩散并快速消隐。
    /// 支持渲染"旧脚印"（第三幕：更淡地复现第二幕走过的路）。
    /// </summary>
    public sealed class TrailViewAdapter : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer footprintPrefab; // 脚印精灵
        [SerializeField] private SpriteRenderer ripplePrefab;    // 水纹圆环精灵
        [SerializeField] private float footLifetime = 30f;       // 脚印存活（秒）
        [SerializeField] private float rippleLifetime = 1.6f;    // 水纹扩散时长
        [SerializeField] private float rippleScale = 2.2f;       // 水纹扩散倍率
        [SerializeField] private float footSideOffset = 0.14f;   // 左右脚横向偏移
        [SerializeField, Range(0f, 1f)] private float oldMarkAlpha = 0.35f; // 旧脚印透明度

        private sealed class Active
        {
            public SpriteRenderer View; public float T; public float Life;
            public bool IsRipple; public float BaseAlpha;
        }

        private ObjectPool<SpriteRenderer> footPool;
        private ObjectPool<SpriteRenderer> ripplePool;
        private readonly List<Active> actives = new List<Active>(128);
        private bool leftFoot;

        private void Awake()
        {
            footPool = MakePool(footprintPrefab);
            ripplePool = MakePool(ripplePrefab);
        }

        /// <summary>订阅 TrailRecorder.MarkPlaced 即可</summary>
        public void OnMarkPlaced(TrailMark mark) => Spawn(mark, 1f);

        /// <summary>第三幕复现旧脚印（一次性，更淡）</summary>
        public void RenderOldMarks(IReadOnlyList<TrailMark> marks)
        {
            foreach (var m in marks) Spawn(m, oldMarkAlpha, persistent: true);
        }

        private void Spawn(TrailMark mark, float alphaMul, bool persistent = false)
        {
            bool isRipple = mark.Kind == TrailKind.BoyRipple || mark.Kind == TrailKind.GirlRipple;
            var pool = isRipple ? ripplePool : footPool;
            var view = pool.Get();
            Vector2 pos = mark.Pos;
            if (!isRipple)
            {
                // 左右脚交替：垂直于移动方向偏移
                leftFoot = !leftFoot;
                Vector2 dir = new Vector2(Mathf.Cos(mark.AngleDeg * Mathf.Deg2Rad),
                                          Mathf.Sin(mark.AngleDeg * Mathf.Deg2Rad));
                Vector2 side = new Vector2(-dir.y, dir.x) * (leftFoot ? footSideOffset : -footSideOffset);
                pos += side;
                view.transform.rotation = Quaternion.Euler(0, 0, mark.AngleDeg - 90f);
            }
            else
            {
                view.transform.rotation = Quaternion.identity;
                view.transform.localScale = Vector3.one * 0.4f;
            }
            view.transform.position = pos;
            float a = Mathf.Clamp01(mark.Strength) * alphaMul;
            view.color = WithAlpha(view.color, a);
            actives.Add(new Active
            {
                View = view, T = 0f,
                Life = persistent ? float.MaxValue : (isRipple ? rippleLifetime : footLifetime),
                IsRipple = isRipple, BaseAlpha = a,
            });
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = actives.Count - 1; i >= 0; i--)
            {
                var a = actives[i];
                a.T += dt;
                float k = a.Life <= 0f ? 1f : Mathf.Clamp01(a.T / a.Life);
                if (a.IsRipple)
                {
                    a.View.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, rippleScale, k);
                    a.View.color = WithAlpha(a.View.color, a.BaseAlpha * (1f - k));
                }
                else if (a.Life < float.MaxValue)
                {
                    // 脚印前半段保持、后半段淡出
                    a.View.color = WithAlpha(a.View.color, a.BaseAlpha * (1f - Mathf.InverseLerp(0.5f, 1f, k)));
                }
                if (a.T >= a.Life)
                {
                    (a.IsRipple ? ripplePool : footPool).Release(a.View);
                    actives.RemoveAt(i);
                }
            }
        }

        private ObjectPool<SpriteRenderer> MakePool(SpriteRenderer prefab) =>
            new ObjectPool<SpriteRenderer>(
                createFunc: () =>
                {
                    var v = Instantiate(prefab, transform);
                    v.gameObject.SetActive(false);
                    return v;
                },
                actionOnGet: v => v.gameObject.SetActive(true),
                actionOnRelease: v => v.gameObject.SetActive(false),
                actionOnDestroy: v => { if (v != null) Destroy(v.gameObject); },
                collectionCheck: false, defaultCapacity: 64, maxSize: 512);

        private static Color WithAlpha(Color c, float a) { c.a = a; return c; }

        private void OnDestroy() { footPool?.Dispose(); ripplePool?.Dispose(); }
    }
}
