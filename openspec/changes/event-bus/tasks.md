# Issue #6 现有同步事件总线行为固化任务

## OpenSpec

- [x] 读取并记录 Issue #6、现有 EventBus 契约、实现、组合根注入和测试基线
- [x] 完成 proposal、spec、plan、design、tasks 和 delta spec
- [x] 执行 `openspec validate event-bus` 并修复校验问题
- [ ] 用户确认本 Change 文档后开始代码/测试实现

## 最小行为实现

- [ ] 确认不新增事件类型、不创建独立 Contracts 程序集、不移动现有生产类型
- [ ] 复核 `IEventBus` 三项操作和现有 `SessionInitializedEvent` 事实语义
- [ ] 仅在测试暴露规格缺口时修复同步发布、精确类型隔离或稳定快照行为
- [ ] 仅在测试暴露规格缺口时修复订阅去重、解除订阅、空参数和异常传播行为
- [ ] 确认 `SessionRoot` 通过现有默认工厂注入唯一 EventBus

## 验证

- [ ] 添加多订阅者、订阅顺序和同步执行行为测试
- [ ] 添加解除订阅、类型隔离、重复订阅和无订阅者行为测试
- [ ] 添加处理器异常传播测试，确认不被静默吞掉
- [ ] 执行 Application/Adapters 针对性编译
- [ ] 执行 Unity EditMode 测试并记录真实通过数量
- [ ] 更新本 Change 的任务状态和验证结果

## 明确不在本 Issue 实现

- [x] 不新增 `SceneLoadedEvent`、`StageChangedEvent` 或无真实调用方的示例事件
- [x] 不新增事件基类、注册中心、过滤器、优先级、重试或异步队列
- [x] 不实现网络、跨进程、线程安全、持久化或事件重放
- [x] 不实现命令总线、请求/响应总线或事件返回命令结果
- [x] 不进行正式玩法事件和正式场景架构扩展
