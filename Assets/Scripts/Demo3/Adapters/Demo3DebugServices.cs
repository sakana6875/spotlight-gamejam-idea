using System;
using System.Collections.Generic;
using Healing.Demo3.Application;
using Healing.Demo3.Domain;   // Demo3AudioMix 定义在 Domain 层
using UnityEngine;

namespace Healing.Demo3.Adapters
{
    /// <summary>
    /// Demo3 独立运行时的本地服务集合：输入与音频来自场景内适配器组件，
    /// 事件总线与结果上报使用本地实现（结果打印到 Console）。
    /// 正式接入全局架构后，SessionRoot 通过 Demo3SceneRoot.Configure 注入
    /// 真实服务，本类不再参与运行。
    /// </summary>
    public sealed class Demo3LocalServices : IDemo3Services
    {
        public IInputService Input { get; }
        public IEventBus Bus { get; } = new LocalEventBus();
        public IDemoRun Run { get; } = new LoggingDemoRun();
        public IDemo3Audio Audio { get; }
        public IDemoSceneFlow Flow { get; set; }       // 场景内兜底为空
        public IDemo3RunFlags Flags { get; set; }      // 场景内兜底为空

        public Demo3LocalServices(IInputService input, IDemo3Audio audio)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Audio = audio ?? new NullDemo3Audio();
        }
    }

    /// <summary>本地事件总线：只负责订阅/发布/退订，不做业务判断</summary>
    public sealed class LocalEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();

        public void Publish<T>(T evt)
        {
            if (!handlers.TryGetValue(typeof(T), out var list)) return;
            foreach (var h in list.ToArray()) ((Action<T>)h).Invoke(evt);
        }

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (!handlers.TryGetValue(typeof(T), out var list))
            { list = new List<Delegate>(); handlers[typeof(T)] = list; }
            list.Add(handler);
            return new Subscription(() => Unsubscribe(handler));
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handlers.TryGetValue(typeof(T), out var list)) list.Remove(handler);
        }

        private sealed class Subscription : IDisposable
        {
            private Action dispose;
            public Subscription(Action dispose) { this.dispose = dispose; }
            public void Dispose() { dispose?.Invoke(); dispose = null; }
        }
    }

    public sealed class LoggingDemoRun : IDemoRun
    {
        public void Report(DemoRunResult result)
            => Debug.Log($"[Demo3] Demo 结果上报：{result}（本地模式，正式流程由 DemoFlowManager 接管）");
    }

    public sealed class NullDemo3Audio : IDemo3Audio
    {
        public void EnterAmbience() { }
        public void ApplyMix(in Demo3AudioMix mix) { }
        public void ExitAmbience() { }
    }
}
