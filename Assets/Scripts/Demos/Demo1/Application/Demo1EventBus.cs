using System;
using System.Collections.Generic;

namespace Spotlight.Demos.Demo1.Application
{
    /// <summary>
    /// 仅在 Demo1 单局内传递已经发生的判定和完成事实。
    /// </summary>
    public sealed class Demo1EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        /// <summary>
        /// 同步发布一个已发生事实。
        /// </summary>
        public void Publish<TEvent>(TEvent eventData)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out List<Delegate> handlers))
            {
                return;
            }

            Delegate[] snapshot = handlers.ToArray();
            for (int index = 0; index < snapshot.Length; index++)
            {
                ((Action<TEvent>)snapshot[index]).Invoke(eventData);
            }
        }

        /// <summary>
        /// 注册一个事实处理器；重复注册同一委托不会产生重复回调。
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
        /// 解除一个事实处理器。
        /// </summary>
        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null || !_handlers.TryGetValue(typeof(TEvent), out List<Delegate> handlers))
            {
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _handlers.Remove(typeof(TEvent));
            }
        }
    }
}
