using System;
using Spotlight.Application.Services;

namespace Spotlight.Adapters.Session
{
    /// <summary>
    /// 记录场景流程请求的内存适配器；实际 SceneManager 接入属于后续场景流程任务。
    /// </summary>
    public sealed class InMemorySceneFlow : ISceneFlow
    {
        public string CurrentSceneId { get; private set; }

        public SceneFlowResult LoadScene(string sceneId)
        {
            if (string.IsNullOrWhiteSpace(sceneId))
            {
                return SceneFlowResult.Failed(SceneFlowResultCode.InvalidSceneId);
            }

            if (string.Equals(CurrentSceneId, sceneId, StringComparison.Ordinal))
            {
                return SceneFlowResult.Failed(SceneFlowResultCode.AlreadyActive);
            }

            CurrentSceneId = sceneId;
            return SceneFlowResult.Succeeded(sceneId);
        }
    }
}
