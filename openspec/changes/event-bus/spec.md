# 现有同步进程内事件总线规格

## 1. 保留的最小接口

### 要求：事件端口仅保留三项操作

`IEventBus` MUST 提供：

- `void Publish<TEvent>(TEvent eventData)`
- `void Subscribe<TEvent>(Action<TEvent> handler)`
- `void Unsubscribe<TEvent>(Action<TEvent> handler)`

本 Issue 不增加事件基类、返回值、过滤器、优先级、异步 API 或命令 API。

### 要求：事件只表示事实

事件 MUST 表示已经发生的事实。保留 `SessionInitializedEvent` 作为现有真实示例；本 Issue 不新增没有真实调用方的 `SceneLoadedEvent`、`StageChangedEvent` 或其他事件。

## 2. 现有同步行为

### 要求：同一事件按订阅顺序同步通知

存在多个订阅者时，事件 MUST 在同一次 `Publish` 调用中同步通知，并按照订阅顺序调用。没有订阅者时 MUST 正常返回。

### 要求：发布期间使用稳定快照

处理器在回调期间解除自身或其他订阅者时，当前发布不得因为修改列表而失败；订阅变更影响后续发布。

### 要求：处理器异常保持可观察

处理器抛出的异常 MUST 传播到 `Publish` 调用方或 Unity Console。EventBus 不得静默捕获、转换为成功或自动重试；当前发布在异常处停止。

## 3. 订阅边界

### 要求：事件类型严格隔离

对 `TEventA` 的订阅 MUST NOT 收到 `TEventB` 的发布。事件总线按精确事件类型分组，不做继承匹配或字段相似匹配。

### 要求：解除订阅和重复订阅行为稳定

成功解除订阅后，处理器 MUST NOT 收到后续同类型事件。同一处理器重复订阅不得导致一次发布重复调用。重复解除不存在的处理器 MUST 安全返回；空处理器订阅 MUST 抛出明确参数异常，空处理器解除 MUST 安全返回。

## 4. 组合根边界

`SessionRoot` MUST 继续通过现有服务工厂创建并注入唯一 EventBus。其他模块不得自行创建、查找或替换全局 EventBus，不得使用静态单例或 Service Locator。

订阅者 MUST 在场景销毁、禁用或用例结束时解除订阅。EventBus 不负责推断 Unity 生命周期，也不保存业务状态。

## 5. Issue 验收

EditMode 行为测试 MUST 覆盖：多订阅者和顺序、同步执行、解除订阅、事件类型隔离、异常传播、重复订阅去重和无订阅者发布。测试不得检查私有字段、具体容器类型或未使用的未来扩展点。

本规格不要求任何新的生产事件类型或新的运行时架构层。
