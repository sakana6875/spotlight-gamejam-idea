using System;

namespace Healing.Demo3.Domain
{
    /// <summary>小女孩姿态（压迫值驱动，供 Animator 整数参数使用）</summary>
    public enum Demo3Posture
    {
        Normal    = 0, // 正常直立
        SlightBow = 1, // 轻微低头
        CoverEars = 2, // 弯腰捂耳朵
        Crouch    = 3, // 逐渐蜷缩
        Kneel     = 4  // 抱腿埋头（跪倒）
    }

    /// <summary>Demo3 运行阶段（Domain 状态机）</summary>
    public enum Demo3Stage
    {
        Gathering  = 0, // 人群聚集中（可游玩）
        Collapsing = 1, // 压迫值满，跪倒演出中（输入已无效）
        Finished   = 2  // 结果已上报，等待 DemoFlowManager 接管
    }

    /// <summary>
    /// 环境音频混合比例（纯数据，0~1）。
    /// 放在 Domain 是因为 AmbienceCurve 的输出类型；具体播放由音频适配层完成。
    /// </summary>
    [Serializable]
    public struct Demo3AudioMix
    {
        public float CrowdNoise;  // 人群嘈杂声
        public float CrowdSteps;  // 人群脚步声
        public float GirlSteps;   // 小女孩脚步声
        public float Heartbeat;   // 心跳声
    }
}
