namespace Spotlight.Application.Services.Scene
{
    /// <summary>
    /// 进入 Demo 的请求模式；具体解锁和存档规则由上层用例负责。
    /// </summary>
    public enum DemoEntryMode
    {
        New,
        Continue,
        Replay
    }
}
