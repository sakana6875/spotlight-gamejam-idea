# Issue #6 现有事件总线最小设计

## 设计结论

Issue #6 不再作为“搭建一套新事件架构”实施，而作为“验证并固化已有最小 EventBus”实施。Issue #3 已提供接口、实现和组合根注入；Issue #5 已验证 `SessionInitializedEvent`。继续增加抽象会产生没有真实调用方的复杂度。

## 保留的结构

```text
SessionRoot
  └── SessionBootstrapper
      └── ISessionServiceFactory
          └── InMemoryEventBus : IEventBus
```

`IEventBus` 保持在 Application，`InMemoryEventBus` 保持在 Adapters，事件类型按现有依赖归属 Application 或 Domain。不创建独立 Contracts 程序集，不移动现有类型。

## 行为边界

- 同步发布，不排队、不跨线程、不延迟。
- 按精确事件类型隔离订阅者。
- 按订阅顺序调用处理器。
- 发布时使用稳定订阅快照。
- 重复订阅去重，解除订阅后停止通知。
- 处理器异常直接传播，EventBus 不静默捕获或重试。
- 订阅者自己负责生命周期结束时解除订阅。

## 事件范围

只保留已有并正在使用的 `SessionInitializedEvent`。`SceneLoadedEvent`、`StageChangedEvent` 和其他事件等出现真实发布者与订阅者后再定义；不为未来可能需求预留类型。

## 复杂度控制

本 Issue 不增加事件基类、注册中心、路由层、过滤器、优先级、重放、异步队列或命令转换。后续如果真实功能证明当前边界不足，必须通过新的 OpenSpec 变更重新评估架构，而不是在本 Issue 中隐式扩张。
