using System;
using System.Collections.Generic;
using Spotlight.Application.Services;

namespace Spotlight.Adapters.Session
{
    /// <summary>
    /// 同步、进程内、强类型事件总线。只负责订阅关系和发布，不承载业务逻辑。
    /// </summary>
    public sealed class InMemoryEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers =
            new Dictionary<Type, List<Delegate>>();

        public void Publish<TEvent>(TEvent eventData)
        {
            Type eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out List<Delegate> handlers))
            {
                return;
            }

            Delegate[] snapshot = handlers.ToArray();
            for (int index = 0; index < snapshot.Length; index++)
            {
                ((Action<TEvent>)snapshot[index]).Invoke(eventData);
            }
        }

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out List<Delegate> handlers))
            {
                handlers = new List<Delegate>();
                _handlers.Add(eventType, handlers);
            }

            if (!handlers.Contains(handler))
            {
                handlers.Add(handler);
            }
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                return;
            }

            Type eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out List<Delegate> handlers))
            {
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _handlers.Remove(eventType);
            }
        }
    }
}
