namespace Spotlight.Demos.Demo1.Application
{
    /// <summary>
    /// 向 Demo1 单局驱动提供当前帧的 Pulse 动作状态。
    /// </summary>
    public interface IInputPulseReader
    {
        /// <summary>
        /// 获取当前帧是否执行了 Pulse 动作。
        /// </summary>
        bool PulsePressed { get; }
    }
}
