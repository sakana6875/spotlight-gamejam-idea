using System.Collections;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 石碑视图（L1/L2）：玩家靠近按 E → 场景根判定后点亮。
    /// 点亮表现：碑文浮现 + 白色粒子升起；区域附加效果（飞鱼/星星/防护罩）由子组件挂接。
    /// 判定本身（是否已点亮、记入旗标）在 Act3Session，本类只做表现与距离查询。
    /// </summary>
    public sealed class StoneTabletView : MonoBehaviour
    {
        [SerializeField] private string tabletId = "L1";
        [SerializeField] private float interactRadius = 1.6f;
        [SerializeField] private SpriteRenderer inscription;   // 碑文层（初始隐藏）
        [SerializeField] private ParticleSystem riseParticles; // 升起粒子
        [SerializeField] private ShieldBurstView shieldBurst;  // 仅 L2：防护罩
        [SerializeField] private StarfallView starfall;        // 仅 L2：星星坠落
        [SerializeField] private float inscriptionFadeIn = 1.5f;

        public string TabletId => tabletId;
        public bool IsInRange(Vector2 playerPos)
            => ((Vector2)transform.position - playerPos).sqrMagnitude <= interactRadius * interactRadius;

        public void PlayLitEffect()
        {
            if (riseParticles != null) riseParticles.Play();
            if (shieldBurst != null) shieldBurst.Play();
            if (starfall != null) starfall.Play();
            if (inscription != null) StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            inscription.enabled = true;
            for (float t = 0f; t < inscriptionFadeIn; t += Time.deltaTime)
            {
                var c = inscription.color; c.a = t / inscriptionFadeIn;
                inscription.color = c;
                yield return null;
            }
        }
    }
}
