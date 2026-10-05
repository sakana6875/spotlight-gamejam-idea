# Issue #6 现有同步事件总线行为固化任务

## OpenSpec

- [x] 读取并记录 Issue #6、现有 EventBus 契约、实现、组合根注入和测试基线
- [x] 完成 proposal、spec、plan、design、tasks 和 delta spec
- [x] 执行 `openspec validate event-bus` 并修复校验问题：Change valid
- [x] 用户确认本 Change 文档后开始代码/测试实现

## 最小行为实现

- [x] 确认不新增事件类型、不创建独立 Contracts 程序集、不移动现有生产类型
- [x] 复核 `IEventBus` 三项操作和现有 `SessionInitializedEvent` 事实语义
- [x] 复核同步发布、精确类型隔离和稳定快照行为；现有实现满足规格，无需扩大修复范围
- [x] 复核订阅去重、解除订阅、空参数和异常传播行为；现有实现满足规格
- [x] 确认 `SessionRoot` 通过现有默认工厂注入唯一 EventBus；既有 Bootstrapper 行为测试覆盖服务集合和幂等初始化

## 验证

- [x] 添加多订阅者、订阅顺序和同步执行行为测试
- [x] 添加解除订阅、类型隔离、重复订阅和无订阅者行为测试
- [x] 添加处理器异常传播测试，确认不被静默吞掉
- [x] 执行 Application/Adapters 针对性编译：`dotnet build Spotlight.Application.csproj --no-restore` 和 `dotnet build Spotlight.Adapters.csproj --no-restore` 均 0 警告、0 错误
- [x] 执行 Unity EditMode 测试：`issue6-editmode-results.xml` 返回 Passed，25 个测试全部通过
- [x] 更新本 Change 的任务状态和验证结果

## 明确不在本 Issue 实现

- [x] 不新增 `SceneLoadedEvent`、`StageChangedEvent` 或无真实调用方的示例事件
- [x] 不新增事件基类、注册中心、过滤器、优先级、重试或异步队列
- [x] 不实现网络、跨进程、线程安全、持久化或事件重放
- [x] 不实现命令总线、请求/响应总线或事件返回命令结果
- [x] 不进行正式玩法事件和正式场景架构扩展
