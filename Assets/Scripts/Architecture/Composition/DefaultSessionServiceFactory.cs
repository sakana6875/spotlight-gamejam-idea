using Spotlight.Adapters.Save;
using Spotlight.Adapters.Session;
using Spotlight.Application.Services;

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
                new InMemorySceneFlow(),
                saveService,
                new InMemoryProgressService(saveService.PermanentProgress),
                new InMemoryAudioService(),
                new InMemoryInputService(),
                new UnavailableDialogueService());
        }
    }
}
