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

        /// <summary>
        /// 按订阅顺序同步发布事件；处理器异常直接传播且停止本次发布。
        /// </summary>
        public void Publish<TEvent>(TEvent eventData)
        {
            Type eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out List<Delegate> handlers))
            {
                return;
            }

            // 快照保证处理器可以在回调期间解除订阅，变更只影响后续发布。
            Delegate[] snapshot = handlers.ToArray();
            for (int index = 0; index < snapshot.Length; index++)
            {
                ((Action<TEvent>)snapshot[index]).Invoke(eventData);
            }
        }

        /// <summary>
        /// 注册事件处理器；同一处理器重复注册不会重复通知。
        /// </summary>
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

        /// <summary>
        /// 解除事件处理器；不存在的处理器或事件类型安全返回。
        /// </summary>
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
