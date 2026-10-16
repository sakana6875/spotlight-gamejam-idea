using System;
using System.Collections;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 第二幕开场救援播片（表现层协程，不含游戏规则）：
    /// 画面暗下 → 聚光灯打女孩 → 男孩脚步声 → 男孩自暗处走来，
    /// 停在明暗交界伸手 → 女孩抬头迟疑 → 被拉起 → 男孩向上走一点提示方向 → 交还操控。
    /// 演出全程接管两位角色的动画与位移（SetExternalAnimControl），
    /// 动作按状态名直切（Extend / RaiseHead / Stand），控制器里建同名状态即可，无需转换线。
    /// </summary>
    public sealed class Act2IntroDirector : MonoBehaviour
    {
        [SerializeField] private WalkerViewAdapter boy;
        [SerializeField] private WalkerViewAdapter girl;
        [SerializeField] private HandholdFollower handhold;
        [SerializeField] private Light spotLight;              // 或 Light2D，二选一
        [SerializeField] private Transform boyEntryPoint;      // 暗处入场点
        [SerializeField] private Transform boyStandPoint;      // 明暗交界站位
        [SerializeField] private float walkSpeed = 1.6f;
        [SerializeField] private float hesitatePause = 1.2f;

        // 演出动作状态名（在 Animator 控制器里建同名状态，把美术片段拖进去）
        private const string ExtendState = "Extend";       // 男孩伸手
        private const string RaiseHeadState = "RaiseHead"; // 女孩抬头
        private const string StandState = "Stand";         // 女孩被拉起站直

        public void Play(Action onComplete)
        {
            if (boy == null || girl == null)
            {
                Debug.LogError("[Demo3] Act2IntroDirector 缺少 boy/girl 引用，播片跳过。", this);
                onComplete?.Invoke();
                return;
            }
            StartCoroutine(Routine(onComplete));
        }

        private IEnumerator Routine(Action onComplete)
        {
            // 接管动画与位移，防止场景根每帧 Apply 与演出打架
            boy.SetExternalAnimControl(true);
            girl.SetExternalAnimControl(true);

            Vector2 girlPos = girl.transform.position;
            boy.Bind(boyEntryPoint != null ? (Vector2)boyEntryPoint.position : girlPos + Vector2.right * 8f);

            yield return new WaitForSeconds(1.2f); // 画面暗、聚光灯收拢（Volume 淡入由场景根或手动做）

            // 男孩走到明暗交界
            Vector2 standPos = boyStandPoint != null ? (Vector2)boyStandPoint.position : girlPos + Vector2.right * 1.5f;
            yield return MoveWalker(boy, standPos, walkSpeed);

            // 伸手 → 女孩抬头迟疑 → 拉起
            boy.PlayAction(ExtendState);
            yield return new WaitForSeconds(0.8f);
            girl.PlayAction(RaiseHeadState);
            yield return new WaitForSeconds(hesitatePause);
            girl.PlayAction(StandState);
            yield return new WaitForSeconds(0.6f);

            // 牵手建立：男孩领队
            if (handhold != null)
            {
                handhold.SetFollower(girl.transform);
                handhold.Snap(boy.transform.position, girl.transform.position);
                handhold.Active = true;
            }

            // 男孩向上走一点，提示玩家向上
            Vector2 hintPos = (Vector2)boy.transform.position + Vector2.up * 1.5f;
            yield return MoveWalker(boy, hintPos, walkSpeed);

            // 归还控制权（内部会清直驱缓存，下一帧强制重选状态）
            boy.SetExternalAnimControl(false);
            girl.SetExternalAnimControl(false);
            onComplete?.Invoke();
        }

        private IEnumerator MoveWalker(WalkerViewAdapter w, Vector2 target, float speed)
        {
            Vector2 pos = w.transform.position;
            while (((Vector2)w.transform.position - target).sqrMagnitude > 0.01f)
            {
                pos = Vector2.MoveTowards(pos, target, speed * Time.deltaTime);
                Vector2 vel = (target - pos).normalized * speed;
                w.ApplyExternal(pos, vel);
                if (handhold != null) handhold.SetLeaderPos(pos);
                yield return null;
            }
            w.ApplyExternal(target, Vector2.zero);
        }
    }
}
