using System;

namespace Healing.Demo3.Domain
{
    /// <summary>
    /// Demo3 全部可调参数的纯数据形态（POCO）。
    /// 由 Data 层的 Demo3Tuning（ScriptableObject）转换而来，
    /// Domain / Application 只认这个类型，不认 ScriptableObject。
    /// </summary>
    [Serializable]
    public class Demo3TuningData
    {
        // ---- 玩家 ----
        public float PlayerBaseSpeed = 3f;      // 小女孩基础移动速度（单位/秒）

        // ---- 人群回避（对应需求"参考参数"表）----
        public float AvoidRadius      = 2.8f;   // 回避触发距离（2.5~3 身位）
        public float SafeRadius       = 3.3f;   // 回避结束距离（略大于触发距离，形成滞回）
        public float GatherOuterRadius= 9f;     // 超过此距离视为"被挤在外圈"→ 游荡
        public float AvoidSpeedMul    = 1.35f;  // NPC 回避速度 = 玩家速度 × 此值（120%~150%）
        public float GatherSpeedMul   = 0.55f;  // NPC 聚集速度 = 玩家速度 × 此值（40%~70%）
        public float WanderSpeedMul   = 0.45f;  // 游荡 NPC 在屏幕边沿的移动速度倍率
        public float TurnResponse     = 0.2f;   // NPC 转向响应时间秒（0.1~0.3）
        public float TangentBias      = 0.45f;  // 回避方向上的侧向偏移强度（防直线四散）
        public float SeparationRadius = 0.9f;   // NPC 之间的拥挤分离半径
        public float SeparationPush   = 2.2f;   // 拥挤分离推力
        public float EdgeBand         = 1.6f;   // "屏幕边沿附近"的判定带宽

        // ---- 人群生成 ----
        public int   MaxCrowd      = 240;       // 人群上限
        public float SpawnFullTime = 70f;       // 从 0 到满员的时长（秒）
        public float SpawnMargin   = 2.5f;      // 在视野外多少距离生成

        // ---- 压迫值 ----
        public float PressureTimeRate       = 1f / 100f; // 仅靠时间涨满所需约 100s
        public float PressureCrowdRate      = 1f / 160f; // 满员时额外速率
        public float PressureCrowdReference = 200f;      // 看客数量归一化参考值

        // ---- 演出 ----
        public float CollapseDelay = 2.5f;      // 压迫满后到上报结果的跪倒演出时长
    }
}
