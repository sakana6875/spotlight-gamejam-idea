using Healing.Demo3.Domain;
using UnityEngine;

namespace Healing.Demo3.Data
{
    /// <summary>
    /// Demo3 调参资产（Data 层）。
    /// 在 Project 窗口右键 Create → Demo3 → Tuning 创建；
    /// 默认值即需求文档"参考参数"表的初始建议。
    /// </summary>
    [CreateAssetMenu(menuName = "Demo3/Tuning", fileName = "Demo3Tuning")]
    public sealed class Demo3Tuning : ScriptableObject
    {
        [Header("玩家")]
        public float PlayerBaseSpeed = 3f;

        [Header("人群回避（回避区域规则）")]
        public float AvoidRadius = 2.8f;
        public float SafeRadius = 3.3f;
        public float GatherOuterRadius = 9f;
        public float AvoidSpeedMul = 1.35f;
        public float GatherSpeedMul = 0.55f;
        public float WanderSpeedMul = 0.45f;
        public float TurnResponse = 0.2f;
        [Tooltip("回避方向的侧向偏移强度，0=直线远离，越大弧线越明显")]
        public float TangentBias = 0.45f;
        public float SeparationRadius = 0.9f;
        public float SeparationPush = 2.2f;
        public float EdgeBand = 1.6f;

        [Header("人群生成")]
        public int MaxCrowd = 240;
        public float SpawnFullTime = 70f;
        public float SpawnMargin = 2.5f;

        [Header("压迫值")]
        public float PressureTimeRate = 0.01f;
        public float PressureCrowdRate = 0.00625f;
        public float PressureCrowdReference = 200f;

        [Header("演出")]
        public float CollapseDelay = 2.5f;

        public Demo3TuningData ToData()
        {
            return new Demo3TuningData
            {
                PlayerBaseSpeed = PlayerBaseSpeed,
                AvoidRadius = AvoidRadius,
                SafeRadius = SafeRadius,
                GatherOuterRadius = GatherOuterRadius,
                AvoidSpeedMul = AvoidSpeedMul,
                GatherSpeedMul = GatherSpeedMul,
                WanderSpeedMul = WanderSpeedMul,
                TurnResponse = TurnResponse,
                TangentBias = TangentBias,
                SeparationRadius = SeparationRadius,
                SeparationPush = SeparationPush,
                EdgeBand = EdgeBand,
                MaxCrowd = MaxCrowd,
                SpawnFullTime = SpawnFullTime,
                SpawnMargin = SpawnMargin,
                PressureTimeRate = PressureTimeRate,
                PressureCrowdRate = PressureCrowdRate,
                PressureCrowdReference = PressureCrowdReference,
                CollapseDelay = CollapseDelay,
            };
        }
    }
}
