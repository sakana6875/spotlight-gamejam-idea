using System;

namespace Spotlight.Application.Services
{
    /// <summary>
    /// 创建并验证一组全局服务，保持初始化过程与 Unity 生命周期解耦。
    /// </summary>
    public sealed class SessionBootstrapper
    {
        private readonly ISessionServiceFactory _serviceFactory;
        private SessionServices _services;

        public SessionBootstrapper(ISessionServiceFactory serviceFactory)
        {
            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
        }

        public bool IsInitialized => _services != null;
        public SessionServices Services => _services;

        public SessionInitializationResult Initialize()
        {
            if (IsInitialized)
            {
                return SessionInitializationResult.AlreadyInitialized(_services);
            }

            try
            {
                SessionServices services = _serviceFactory.Create();
                if (services == null)
                {
                    return SessionInitializationResult.Failed("服务工厂返回了空的服务集合。");
                }

                _services = services;
                return SessionInitializationResult.Succeeded(services);
            }
            catch (Exception exception)
            {
                return SessionInitializationResult.Failed(
                    $"全局服务初始化失败：{exception.GetType().Name}: {exception.Message}");
            }
        }
    }

    /// <summary>
    /// 由组合根持有的服务创建端口；默认实现由 Composition 层提供。
    /// </summary>
    public interface ISessionServiceFactory
    {
        SessionServices Create();
    }

    public enum SessionInitializationCode
    {
        Success,
        AlreadyInitialized,
        Failed
    }

    public sealed class SessionInitializationResult
    {
        private SessionInitializationResult(
            SessionInitializationCode code,
            SessionServices services,
            string errorMessage)
        {
            Code = code;
            Services = services;
            ErrorMessage = errorMessage;
        }

        public SessionInitializationCode Code { get; }
        public SessionServices Services { get; }
        public string ErrorMessage { get; }
        public bool IsSuccess => Code != SessionInitializationCode.Failed;

        public static SessionInitializationResult Succeeded(SessionServices services)
        {
            return new SessionInitializationResult(
                SessionInitializationCode.Success,
                services,
                null);
        }

        public static SessionInitializationResult AlreadyInitialized(SessionServices services)
        {
            return new SessionInitializationResult(
                SessionInitializationCode.AlreadyInitialized,
                services,
                null);
        }

        public static SessionInitializationResult Failed(string errorMessage)
        {
            return new SessionInitializationResult(
                SessionInitializationCode.Failed,
                null,
                errorMessage);
        }
    }
}
