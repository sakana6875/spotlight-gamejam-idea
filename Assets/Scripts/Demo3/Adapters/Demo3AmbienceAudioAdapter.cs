using Healing.Demo3.Application;
using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// IDemo3Audio 的 Unity 实现（属于音频适配层，因此允许持有 AudioSource；
    /// Gameplay 永远看不到这些组件）。
    /// 注意：本组件必须放在 Demo3 场景内部（如场景的 Demo3Audio 节点），
    /// 由 Demo3SceneRoot 同场景序列化引用——不要挂到跨场景的 SessionRoot 上，
    /// 否则就构成"场景对象引用另一场景对象"。
    /// 4 个 AudioSource 均为 loop=true、playOnAwake=false，
    /// 并分别路由到 Mixer 的 SFX/Player 等分组。
    /// 若项目 AudioManager 更完整，可改为由 AudioManager 实现 IDemo3Audio，
    /// 届时场景内不再放置本组件，接口由注入的服务提供。
    /// </summary>
    public sealed class Demo3AmbienceAudioAdapter : MonoBehaviour, IDemo3Audio
    {
        [SerializeField] private AudioSource crowdNoise;   // 人群嘈杂（循环）
        [SerializeField] private AudioSource crowdSteps;   // 人群脚步（循环）
        [SerializeField] private AudioSource girlSteps;    // 小女孩脚步（循环）
        [SerializeField] private AudioSource heartbeat;    // 心跳（循环）
        [SerializeField] private float fadeSpeed = 4f;     // 音量平滑速度

        public void EnterAmbience()
        {
            foreach (var s in Sources())
            {
                if (s == null) continue;
                s.volume = 0f;
                if (!s.isPlaying) s.Play();
            }
        }

        public void ApplyMix(in Demo3AudioMix mix)
        {
            float k = fadeSpeed * Time.deltaTime;
            Fade(crowdNoise, mix.CrowdNoise, k);
            Fade(crowdSteps, mix.CrowdSteps, k);
            Fade(girlSteps, mix.GirlSteps, k);
            Fade(heartbeat, mix.Heartbeat, k);
        }

        public void ExitAmbience()
        {
            foreach (var s in Sources())
                if (s != null && s.isPlaying) s.Stop();
        }

        private void Fade(AudioSource src, float target, float k)
        {
            if (src == null) return;
            src.volume = Mathf.MoveTowards(src.volume, Mathf.Clamp01(target), k);
        }

        private AudioSource[] Sources() => new[] { crowdNoise, crowdSteps, girlSteps, heartbeat };
    }
}
