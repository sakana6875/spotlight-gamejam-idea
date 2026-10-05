using Spotlight.Adapters.Save;
using Spotlight.Adapters.Scene;
using Spotlight.Adapters.Session;
using Spotlight.Application.Services;
using Spotlight.Application.Services.Scene;

namespace Spotlight.Composition
{
    /// <summary>
    /// 创建当前架构骨架可验证的服务实现；正式 Unity 外部适配器由后续任务替换。
    /// </summary>
    public sealed class DefaultSessionServiceFactory : ISessionServiceFactory
    {
        public SessionServices Create()
        {
            InMemorySaveService saveService = new InMemorySaveService();

            return new SessionServices(
                new InMemoryEventBus(),
                new RecordingSceneFlow(SceneCatalog.CreateDefault()),
                saveService,
                new InMemoryProgressService(saveService.PermanentProgress),
                new InMemoryAudioService(),
                new InMemoryInputService(),
                new UnavailableDialogueService());
        }
    }
}
