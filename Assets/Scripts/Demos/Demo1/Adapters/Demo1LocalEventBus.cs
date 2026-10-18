using System;
using System.Collections.Generic;
using Spotlight.Application.Services;

namespace Spotlight.Demos.Demo1.Adapters
{
    /// <summary>
    /// Demo1 专属的局部事件总线。仅用于 Demo1 内部解耦（如 Controller 到 UI 的通讯）。
    /// </summary>
    public sealed class Demo1LocalEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public void Publish<TEvent>(TEvent eventData)
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                var snapshot = handlers.ToArray();
                foreach (var handler in snapshot)
                {
                    ((Action<TEvent>)handler).Invoke(eventData);
                }
            }
        }

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null) return;
            var type = typeof(TEvent);
            if (!_handlers.TryGetValue(type, out var handlers))
            {
                handlers = new List<Delegate>();
                _handlers[type] = handlers;
            }
            if (!handlers.Contains(handler))
            {
                handlers.Add(handler);
            }
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null) return;
            if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    _handlers.Remove(typeof(TEvent));
                }
            }
        }
    }
}