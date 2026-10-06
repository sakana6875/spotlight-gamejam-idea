using Spotlight.Application.Services.Scene;

namespace Spotlight.Application.Services.Demo
{
    /// <summary>
    /// 统一表达 Demo 进入、重试和退出意图，不引用具体场景对象。
    /// </summary>
    public interface IDemoFlow
    {
        DemoRunResult EnterDemo(DemoId demoId, DemoEntryMode entryMode);
        DemoRunResult RetryDemo(DemoId demoId);
        DemoRunResult ExitDemo(DemoId demoId);
    }

    /// <summary>
    /// Demo 本次运行的明确结果。
    /// </summary>
    public sealed class DemoRunResult
    {
        private DemoRunResult(DemoRunResultCode code, DemoId demoId, DemoEntryMode? entryMode)
        {
            Code = code;
            DemoId = demoId;
            EntryMode = entryMode;
        }

        public DemoRunResultCode Code { get; }
        public DemoId DemoId { get; }
        public DemoEntryMode? EntryMode { get; }
        public bool IsSuccess => Code == DemoRunResultCode.Succeeded;

        public static DemoRunResult Succeeded(DemoId demoId, DemoEntryMode? entryMode = null)
        {
            return new DemoRunResult(DemoRunResultCode.Succeeded, demoId, entryMode);
        }

        public static DemoRunResult Failed(DemoId demoId, DemoEntryMode? entryMode = null)
        {
            return new DemoRunResult(DemoRunResultCode.Failed, demoId, entryMode);
        }

        public static DemoRunResult Abandoned(DemoId demoId, DemoEntryMode? entryMode = null)
        {
            return new DemoRunResult(DemoRunResultCode.Abandoned, demoId, entryMode);
        }
    }

    /// <summary>
    /// Demo 运行成功、失败和主动退出的互斥结果代码。
    /// </summary>
    public enum DemoRunResultCode
    {
        Succeeded,
        Failed,
        Abandoned
    }
}
