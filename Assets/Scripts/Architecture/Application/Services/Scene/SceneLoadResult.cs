namespace Spotlight.Application.Services.Scene
{
    /// <summary>
    /// 场景流程请求的可观察结果。
    /// </summary>
    public sealed class SceneLoadResult
    {
        private SceneLoadResult(SceneLoadResultCode code, SceneId? sceneId, DemoId? demoId, string errorMessage)
        {
            Code = code;
            SceneId = sceneId;
            DemoId = demoId;
            ErrorMessage = errorMessage;
        }

        public SceneLoadResultCode Code { get; }
        public SceneId? SceneId { get; }
        public DemoId? DemoId { get; }
        public string ErrorMessage { get; }
        public bool IsSuccess => Code == SceneLoadResultCode.Success;

        public static SceneLoadResult Succeeded(SceneId sceneId, DemoId? demoId = null)
        {
            return new SceneLoadResult(SceneLoadResultCode.Success, sceneId, demoId, null);
        }

        public static SceneLoadResult Failed(SceneLoadResultCode code, string errorMessage = null)
        {
            return new SceneLoadResult(code, null, null, errorMessage);
        }
    }

    /// <summary>
    /// 场景流程请求的失败和成功分类。
    /// </summary>
    public enum SceneLoadResultCode
    {
        Success,
        InvalidSceneId,
        UnknownScene,
        NotRegistered,
        NotAvailable,
        InvalidDemoId,
        DemoSceneMismatch,
        AlreadyActive,
        LoadFailed
    }
}
