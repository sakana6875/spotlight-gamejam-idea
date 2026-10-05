using System;
using System.Collections.Generic;
using Spotlight.Application.Services;

namespace Spotlight.Adapters.Session
{
    /// <summary>
    /// 记录统一动作注册的最小适配器，不绑定具体键位或旧输入 API。
    /// </summary>
    public sealed class InMemoryInputService : IInputService
    {
        private readonly HashSet<string> _registeredActions =
            new HashSet<string>(StringComparer.Ordinal);

        public bool IsActionRegistered(string actionId)
        {
            return !string.IsNullOrWhiteSpace(actionId) && _registeredActions.Contains(actionId);
        }

        public InputServiceResult RegisterAction(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return InputServiceResult.Failed(InputServiceResultCode.InvalidActionId);
            }

            if (!_registeredActions.Add(actionId))
            {
                return InputServiceResult.Failed(InputServiceResultCode.AlreadyRegistered);
            }

            return InputServiceResult.Succeeded();
        }
    }
}
