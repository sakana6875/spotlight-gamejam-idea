using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// Act2 开场演出（简化版）：
    /// ①聚光灯渐亮 → ②男孩从右侧入场，向左走到女孩身边（直驱自动播 MoveLeft）
    /// → ③男孩停下转 Idle → ④女孩从 Kneel 起身转 Idle → ⑤交还操控
    /// （onComplete → 会话进入 WalkUp；场景根在段落事件里激活女孩的 SetFollow，两人牵手走动）。
    /// 期间两个角色的外部动画接管开启，场景根的 Apply 与玩家输入都被忽略。
    /// 注意：女孩 Animator 控制器的默认状态必须是 Kneel（播片前她一直保持跪姿）；
    /// 聚光灯用 Light2D（URP 2D Renderer 下 3D Light 照不亮 2D 精灵）。
    /// </summary>
    public sealed class Act2IntroDirector : MonoBehaviour
    {
        [Header("演员（拖入）")]
        [SerializeField] private WalkerViewAdapter boy;
        [SerializeField] private FollowerViewAdapter girl;

        [Header("灯光（URP 2D 聚光灯）")]
        [SerializeField] private Light2D spotLight;

        [Header("走位（拖入）")]
        [Tooltip("出发点：男孩从这里出发，向左走 Walk Distance 到女孩身边（放在女孩右侧，间距=Walk Distance）")]
        [SerializeField] private Transform boyStartPoint;
        [Tooltip("男孩向左步行的距离（出发点放在女孩右侧这么远的地方）")]
        [SerializeField] private float walkDistance = 3f;

        [Header("数值")]
        [SerializeField] private float walkSpeed = 1.6f;
        [SerializeField] private float hesitatePause = 1.2f;   // 女孩起身后、牵手前的停顿

        [Header("调试")]
        [SerializeField] private bool verboseLog = true;

        public void Play(Action onComplete)
        {
            if (boy == null || girl == null)
            {
                Debug.LogError("[Demo3] Act2IntroDirector：Boy / Girl 没拖入，播片直接跳过。", this);
                onComplete?.Invoke();
                return;
            }
            Debug.Log("[Demo3] Act2IntroDirector v10.18c：单出发点 + 落点取女孩左侧版", this);
            StartCoroutine(Routine(onComplete));
        }

        private IEnumerator Routine(Action onComplete)
        {
            boy.SetExternalAnimControl(true);
            girl.SetExternalAnimControl(true);

            // 目的地永远取"女孩左侧身旁"：她摆在哪，男孩就走到哪，不会走空
            Vector2 destPos = (Vector2)girl.transform.position + new Vector2(-0.45f, 0.1f);
            Vector2 startPos = boyStartPoint != null ? (Vector2)boyStartPoint.position
                : destPos + Vector2.right * walkDistance;
            boy.Bind(startPos);
            Beat("播片开始");

            // ① 灯光渐亮
            float spotTarget = 1f;
            if (spotLight != null)
            {
                spotTarget = spotLight.intensity;
                spotLight.intensity = 0f;
            }
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                if (spotLight != null) spotLight.intensity = Mathf.Lerp(0f, spotTarget, t / 1.2f);
                yield return null;
            }
            if (spotLight != null) spotLight.intensity = spotTarget;
            Beat("灯光亮起");

            // ② 男孩从出发点向左走到女孩身边（速度朝左 → 直驱自动播 MoveLeft）
            yield return MoveWalker(boy, destPos);
            Beat("男孩走到女孩身边");

            // ③ 男孩停下 → Idle
            boy.ApplyExternal(destPos, Vector2.zero);
            yield return new WaitForSeconds(0.4f);

            // ④ 女孩从 Kneel 起身 → Idle（清零速度即可，直驱自动从默认 Kneel 交叉淡入 Idle）
            girl.ApplyExternal(girl.transform.position, Vector2.zero);
            yield return new WaitForSeconds(hesitatePause);
            Beat("女孩起身");

            // ⑤ 交还操控（牵手跟随由场景根在 WalkUp 段落事件里自动激活：女孩 SetFollow(男孩)）
            boy.SetExternalAnimControl(false);
            girl.SetExternalAnimControl(false);
            Beat("播片完成，交还操控");
            onComplete?.Invoke();
        }

        /// <summary>把角色以固定速度直送到目标点（带超时保护，卡死时直接放置并报警）。</summary>
        private IEnumerator MoveWalker(WalkerViewAdapter walker, Vector2 target)
        {
            Vector2 pos = walker.transform.position;
            if (Vector2.Distance(pos, target) < 0.1f)
                Debug.LogWarning($"[Demo3] Intro：{walker.name} 的出发点和目的地几乎重合（{target}），看不到走路演出" +
                                 "——检查 Boy Start Point 的位置与 Walk Distance 是否合适。", this);
            float timeout = Vector2.Distance(pos, target) / Mathf.Max(0.01f, walkSpeed) + 3f;
            float t = 0f;
            while (Vector2.Distance(pos, target) > 0.05f && t < timeout)
            {
                t += Time.deltaTime;
                var next = Vector2.MoveTowards(pos, target, walkSpeed * Time.deltaTime);
                var vel = (next - pos) / Mathf.Max(Time.deltaTime, 1e-5f);
                walker.ApplyExternal(next, vel);
                pos = next;
                yield return null;
            }
            if (t >= timeout)
            {
                Debug.LogWarning($"[Demo3] Intro：{walker.name} 走位超时，直接放置到目标点（一直没走到 {target}）。", walker);
                walker.ApplyExternal(target, Vector2.zero);
            }
        }

        private void Beat(string msg)
        {
            if (verboseLog) Debug.Log($"[Demo3] Intro：{msg}", this);
        }
    }
}
