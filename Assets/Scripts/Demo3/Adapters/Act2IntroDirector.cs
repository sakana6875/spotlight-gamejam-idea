using System;
using System.Collections;
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// 第二幕开场救援播片（表现层协程，不含游戏规则）：
    /// 画面暗下 → 聚光灯打女孩 → 男孩脚步声 → 男孩自暗处走来，
    /// 停在明暗交界伸手 → 女孩抬头迟疑 → 被拉起 → 男孩向上走一点提示方向 → 交还操控。
    /// 动画靠 Animator Trigger（Extend / RaiseHead / PulledUp / Stand），占位期可只做位移。
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

        private static readonly int ExtendHash = Animator.StringToHash("Extend");
        private static readonly int RaiseHeadHash = Animator.StringToHash("RaiseHead");
        private static readonly int StandHash = Animator.StringToHash("Stand");

        public void Play(Action onComplete) => StartCoroutine(Routine(onComplete));

        private IEnumerator Routine(Action onComplete)
        {
            Vector2 girlPos = girl.transform.position;
            boy.Bind(boyEntryPoint != null ? (Vector2)boyEntryPoint.position : girlPos + Vector2.right * 8f);

            yield return new WaitForSeconds(1.2f); // 画面暗、聚光灯收拢（Volume 淡入由场景根或手动做）

            // 男孩走到明暗交界
            Vector2 standPos = boyStandPoint != null ? (Vector2)boyStandPoint.position : girlPos + Vector2.right * 1.5f;
            yield return MoveWalker(boy, standPos, walkSpeed);

            // 伸手 → 女孩抬头迟疑 → 拉起
            Trigger(boy, ExtendHash);
            yield return new WaitForSeconds(0.8f);
            Trigger(girl, RaiseHeadHash);
            yield return new WaitForSeconds(hesitatePause);
            Trigger(girl, StandHash);
            yield return new WaitForSeconds(0.6f);

            // 牵手建立：男孩领队
            handhold.SetFollower(girl.transform);
            handhold.Snap(boy.transform.position, girl.transform.position);
            handhold.Active = true;

            // 男孩向上走一点，提示玩家向上
            Vector2 hintPos = (Vector2)boy.transform.position + Vector2.up * 1.5f;
            yield return MoveWalker(boy, hintPos, walkSpeed);

            onComplete?.Invoke();
        }

        private IEnumerator MoveWalker(WalkerViewAdapter w, Vector2 target, float speed)
        {
            Vector2 pos = w.transform.position;
            while (((Vector2)w.transform.position - target).sqrMagnitude > 0.01f)
            {
                pos = Vector2.MoveTowards(pos, target, speed * Time.deltaTime);
                Vector2 vel = (target - pos).normalized * speed;
                w.Apply(pos, vel);
                handhold.SetLeaderPos(pos);
                yield return null;
            }
            w.Apply(target, Vector2.zero);
        }

        private static void Trigger(WalkerViewAdapter w, int hash)
        {
            var anim = w.GetComponentInChildren<Animator>();
            if (anim != null) anim.SetTrigger(hash);
        }
    }
}
