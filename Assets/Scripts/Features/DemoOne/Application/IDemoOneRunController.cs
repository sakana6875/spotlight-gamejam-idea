namespace Spotlight.Features.DemoOne.Application
{
    /// <summary>
    /// Demo1 单局控制器的对外接口。
    /// 由 Adapters 层的 RunnerBehaviour 每帧驱动 Tick；
    /// 表现层与 UI 通过订阅事件消费结果，不直接调用控制器方法。
    /// </summary>
    public interface IDemoOneRunController
    {
        /// <summary>当前一局的状态。可随时读取，结束后仍保留最终值供结算展示。</summary>
        DemoOneRunState State { get; }

        /// <summary>
        /// 推进一步。由 Adapters 层的 MonoBehaviour 在 Update 中调用。
        /// </summary>
        /// <param name="songTimeSeconds">当前歌曲时间（秒），由音频时钟适配器计算。</param>
        /// <param name="pulsePressed">本帧玩家是否按下了 Pulse 动作。</param>
        void Tick(float songTimeSeconds, bool pulsePressed);

        /// <summary>重置本局状态。仅供重开流程使用，不负责音频与场景重启。</summary>
        void Restart();
    }
}