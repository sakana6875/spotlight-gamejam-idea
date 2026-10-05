# 同步进程内强类型事件总线

## ADDED Requirements

### Requirement: 事件总线提供强类型同步发布订阅
系统 MUST 提供 `IEventBus`，包含 `Publish<TEvent>`、`Subscribe<TEvent>` 和 `Unsubscribe<TEvent>` 三项操作。发布者只依赖事件类型和接口，不依赖订阅者具体类型。

#### Scenario: 多个订阅者按顺序同步收到事件
- **WHEN** 两个处理器按顺序订阅同一种事件，发布者调用 `Publish`
- **THEN** 两个处理器在同一次调用中同步执行
- **AND** 执行顺序与订阅顺序一致

#### Scenario: 没有订阅者时发布正常返回
- **WHEN** 某事件没有任何订阅者
- **THEN** `Publish` 正常返回
- **AND** 不创建业务状态或伪造处理结果

### Requirement: 事件类型彼此隔离
事件总线 MUST 按精确事件类型隔离订阅者，不得因为字段相同、继承关系或名称相似而交叉通知。

#### Scenario: 不同事件类型不交叉通知
- **WHEN** 处理器订阅 `EventA`，发布 `EventB`
- **THEN** `EventA` 处理器不被调用

### Requirement: 解除订阅和重复订阅行为明确
成功解除订阅后，处理器 MUST NOT 收到后续同类型事件；同一处理器重复订阅不得导致一次发布重复调用；重复解除不存在的处理器 MUST 安全返回。

#### Scenario: 解除订阅后停止通知
- **WHEN** 处理器订阅、收到一次事件、随后解除订阅，再发布同类型事件
- **THEN** 处理器只收到第一次事件

#### Scenario: 重复订阅不重复通知
- **WHEN** 同一个处理器对同一事件类型订阅两次，再发布一次事件
- **THEN** 处理器只被调用一次

### Requirement: 处理器异常保持可观察
处理器抛出的异常 MUST 传播到 `Publish` 调用方或 Unity Console；事件总线不得静默捕获、转换为成功或自动重试。当前发布在异常处停止。

#### Scenario: 处理器异常传播
- **WHEN** 订阅处理器在同步发布期间抛出异常
- **THEN** `Publish` 向调用方传播该异常
- **AND** 调用方可以观察原始失败原因

### Requirement: EventBus 由组合根创建
`SessionRoot` MUST 通过服务工厂创建并注入唯一的 EventBus 实例。其他模块 MUST NOT 使用静态单例、Service Locator 或自行查找和替换全局 EventBus。

#### Scenario: SessionRoot 提供 EventBus
- **WHEN** `SessionRoot` 首次初始化
- **THEN** `SessionServices.EventBus` 非空
- **AND** 后续幂等初始化不创建第二组全局服务

### Requirement: 订阅者负责生命周期解除
订阅者 MUST 在场景销毁、禁用或用例结束时显式解除订阅；EventBus 不得用业务状态或 Unity 查找替代生命周期管理。

#### Scenario: 解除订阅后对象不再接收事件
- **WHEN** 场景适配器在生命周期结束时解除订阅
- **THEN** 后续发布不再调用该适配器处理器
