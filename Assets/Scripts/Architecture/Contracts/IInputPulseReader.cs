namespace Spotlight.Contracts
{
    /// <summary>
    /// 读取玩家 Pulse 动作（敲击/点击）状态的端口。
    /// 由输入适配器实现；玩法层不直接读取 Input System 设备。
    /// </summary>
    public interface IInputPulseReader
    {
        /// <summary>本帧 Pulse 动作是否被按下。</summary>
        bool PulsePressed { get; }
    }
}