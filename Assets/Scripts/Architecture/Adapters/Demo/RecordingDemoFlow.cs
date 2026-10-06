using Spotlight.Application.Services.Demo;
using Spotlight.Application.Services.Scene;

namespace Spotlight.Adapters.Demo
{
    /// <summary>
    /// 记录 Demo 流程意图和显式结果，不加载场景、不访问 Hub 或 Menu。
    /// </summary>
    public sealed class RecordingDemoFlow : IDemoFlow
    {
        public DemoFlowOperation LastOperation { get; private set; }
        public DemoId LastDemoId { get; private set; }
        public DemoEntryMode? LastEntryMode { get; private set; }
        public DemoRunResult LastResult { get; private set; }

        public DemoRunResult EnterDemo(DemoId demoId, DemoEntryMode entryMode)
        {
            LastOperation = DemoFlowOperation.Enter;
            LastDemoId = demoId;
            LastEntryMode = entryMode;
            LastResult = NextResult(demoId, entryMode);
            return LastResult;
        }

        public DemoRunResult RetryDemo(DemoId demoId)
        {
            LastOperation = DemoFlowOperation.Retry;
            LastDemoId = demoId;
            LastEntryMode = DemoEntryMode.Replay;
            LastResult = NextResult(demoId, DemoEntryMode.Replay);
            return LastResult;
        }

        public DemoRunResult ExitDemo(DemoId demoId)
        {
            LastOperation = DemoFlowOperation.Exit;
            LastDemoId = demoId;
            LastEntryMode = null;
            LastResult = NextResult(demoId, null);
            return LastResult;
        }

        public void SetNextResult(DemoRunResult result)
        {
            _nextResult = result;
        }

        private DemoRunResult NextResult(DemoId demoId, DemoEntryMode? entryMode)
        {
            DemoRunResult result = _nextResult ?? DemoRunResult.Succeeded(demoId, entryMode);
            _nextResult = null;
            return result;
        }

        private DemoRunResult _nextResult;
    }

    /// <summary>
    /// 最近一次记录的 Demo 流程操作。
    /// </summary>
    public enum DemoFlowOperation
    {
        None,
        Enter,
        Retry,
        Exit
    }
}
