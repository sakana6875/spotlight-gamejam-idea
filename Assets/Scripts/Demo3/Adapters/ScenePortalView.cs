using System.Collections;
using Healing.Demo3.Application;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 场景传送门（纯适配器）：玩家走进触发区 → 渐黑 → 切到目标场景。
    /// 用法：隐藏区入口（Act2-1 → Hidden1/Hidden2）、幕间手动跳转。
    /// 触发器由本组件强制 Is Trigger；换场走载体的组合根定位（Demo3FlowCarrier.LocateFlow），
    /// 不碰 SceneManager、不跨模块 FindObjectOfType。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ScenePortalView : MonoBehaviour
    {
        [Tooltip("目标场景名（Build Settings 里的名字，如 Hidden1 / Hidden2）")]
        [SerializeField] private string targetSceneId = "Hidden1";
        [Tooltip("只认这个角色：拖女孩的 WalkerViewAdapter；留空则任何碰撞都触发")]
        [SerializeField] private WalkerViewAdapter playerFilter;
        [SerializeField] private CanvasGroup fadeToBlack;   // 可留空（留空直接切）
        [SerializeField] private float fadeSeconds = 0.8f;

        private bool fired;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true; // 强制触发器，忘勾也能用
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (fired) return;
            if (playerFilter != null && other.GetComponentInParent<WalkerViewAdapter>() != playerFilter) return;
            var flow = Demo3FlowCarrier.LocateFlow();
            if (flow == null)
            {
                Debug.LogError($"[Demo3] 传送门（→{targetSceneId}）找不到 Demo3FlowCarrier，无法切场景。", this);
                return;
            }
            fired = true;
            StartCoroutine(GoRoutine(flow));
        }

        private IEnumerator GoRoutine(IDemoSceneFlow flow)
        {
            for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
            {
                if (fadeToBlack != null) fadeToBlack.alpha = Mathf.Clamp01(t / fadeSeconds);
                yield return null;
            }
            if (fadeToBlack != null) fadeToBlack.alpha = 1f;
            flow.GoTo(targetSceneId);
        }
    }
}
