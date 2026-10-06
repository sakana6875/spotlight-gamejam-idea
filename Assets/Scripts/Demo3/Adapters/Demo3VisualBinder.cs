using Healing.Demo3.Application;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 视觉表现绑定：压迫值 → 暗角 / 轻度扭曲 / 色差 / 舞台灯光 / 镜头拉近。
    /// 全部通过 URP Volume 覆盖与 Light 2D 实现，不需要自定义 Shader。
    /// （需求中"画面扭曲【待定】"：此处用 Volume 的 Lens Distortion 提供轻量方案，
    ///   可用 enableDistortion 开关；若后续换成自定义全屏扭曲，只改本类。）
    /// </summary>
    public sealed class Demo3VisualBinder : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private Volume volume;
        [SerializeField] private Light2D spotLight;     // 聚光灯，运行时跟随小女孩
        [SerializeField] private Transform girl;
        [SerializeField] private Camera targetCamera;

        [Header("镜头")]
        [SerializeField] private float baseOrthoSize = 7f;
        [SerializeField] private float endOrthoSize = 4f;   // 压迫满时拉近到此值
        [SerializeField] private float zoomStartPressure = 0.8f;

        [Header("扭曲（待定的轻量方案）")]
        [SerializeField] private bool enableDistortion = true;
        [SerializeField] private float maxLensDistortion = -0.25f;
        [SerializeField] private float maxChromaticAberration = 0.35f;

        [Header("暗角与灯光")]
        [SerializeField] private float vignetteStart = 0.12f;
        [SerializeField] private float vignetteEnd = 0.8f;
        [SerializeField] private float spotIntensityStart = 0.4f;
        [SerializeField] private float spotIntensityEnd = 1.6f;
        [SerializeField] private float spotRadiusStart = 6f;
        [SerializeField] private float spotRadiusEnd = 2.5f;

        private Vignette vignette;
        private LensDistortion lensDistortion;
        private ChromaticAberration chromaticAberration;

        private void Awake()
        {
            // 运行时克隆 profile，避免改到资产本体
            volume.profile = Instantiate(volume.profile);
            volume.profile.TryGet(out vignette);
            volume.profile.TryGet(out lensDistortion);
            volume.profile.TryGet(out chromaticAberration);
        }

        public void Apply(Demo3Session s)
        {
            float p = s.Pressure;

            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(vignetteStart, vignetteEnd, p);

            if (lensDistortion != null)
                lensDistortion.intensity.value = enableDistortion ? maxLensDistortion * p : 0f;
            if (chromaticAberration != null)
                chromaticAberration.intensity.value = maxChromaticAberration * p;

            float lightT = Mathf.InverseLerp(0.3f, 1f, p); // 0.3 起"舞台灯光"逐渐显现
            spotLight.intensity = Mathf.Lerp(spotIntensityStart, spotIntensityEnd, lightT);
            spotLight.pointLightOuterRadius = Mathf.Lerp(spotRadiusStart, spotRadiusEnd, lightT);
            spotLight.transform.position = girl.position;

            float zoomT = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(zoomStartPressure, 1f, p));
            targetCamera.orthographicSize = Mathf.Lerp(baseOrthoSize, endOrthoSize, zoomT);
        }
    }
}
