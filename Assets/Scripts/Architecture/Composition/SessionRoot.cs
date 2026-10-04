using UnityEngine;
using Spotlight.Application.Services;

namespace Spotlight.Composition
{
    /// <summary>
    /// 唯一组合根，负责创建全局服务并将接口依赖提供给场景入口。
    /// </summary>
    public sealed class SessionRoot : MonoBehaviour
    {
        private SessionBootstrapper _bootstrapper;

        public SessionServices Services => _bootstrapper == null ? null : _bootstrapper.Services;
        public bool IsInitialized => _bootstrapper != null && _bootstrapper.IsInitialized;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// 初始化全局服务并保持组合根跨场景存活；重复调用不会创建第二组服务。
        /// </summary>
        public SessionInitializationResult Initialize()
        {
            if (_bootstrapper == null)
            {
                _bootstrapper = new SessionBootstrapper(new DefaultSessionServiceFactory());
            }

            SessionInitializationResult result = _bootstrapper.Initialize();
            if (!result.IsSuccess)
            {
                Debug.LogError(result.ErrorMessage, this);
                return result;
            }

            DontDestroyOnLoad(gameObject);
            return result;
        }
    }
}
