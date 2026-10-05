# Issue #6 同步事件总线设计

## 现状与边界

仓库已有 `IEventBus` 和 `InMemoryEventBus`：接口位于 `Spotlight.Application.Services`，实现位于 `Spotlight.Adapters.Session`，默认工厂由 `SessionRoot` 所在 Composition 程序集创建。Issue #5 已通过 `SessionInitializedEvent` 验证事件发布和解除订阅。Issue #6 的实现重点是把已有最小能力收敛为明确、可测试的公开契约，而不是另起一套事件系统。

## 主要决策

### 1. 保留现有程序集归属

项目规则禁止为小型契约创建独立 Contracts 程序集，且 Application 不得依赖 Adapters。因此保留 `IEventBus` 在 Application、`InMemoryEventBus` 在 Adapters。事件类型按其依赖归属 Application 或 Domain；目录名不凌驾于程序集无环约束。

### 2. 精确类型索引

实现按 `typeof(TEvent)` 建立订阅列表。这样不同事件类型天然隔离，不把继承、多态或字段相似误认为同一种事实事件。发布无订阅者时返回，不产生隐式业务行为。

### 3. 同步快照和异常传播

发布时复制当前订阅列表，再按订阅顺序同步调用。回调可以安全解除订阅，变更作用于下一次发布。处理器异常不被 EventBus 捕获；异常直接暴露给调用方或 Unity Console，当前发布在异常处停止，避免把失败伪装成完整成功。

### 4. 订阅生命周期

EventBus 只管理委托关系，不推断对象生命周期。Unity 适配器和场景对象必须在销毁、禁用或用例结束时解除订阅。重复订阅去重，重复解除安全，避免事件重复通知和残留引用。

## 不做的抽象

不增加事件基类、反射注册、事件优先级、异步调度、线程锁、重放存储、过滤器、重试器或事件到命令的转换。出现真实跨线程或持久化需求时另立 OpenSpec Change。
