# Issue #6 同步进程内强类型事件总线执行任务

## OpenSpec

- [ ] 读取并记录 Issue #6、现有 EventBus 契约、实现、组合根注入和测试基线
- [ ] 完成 proposal、spec、plan、design 和 tasks
- [ ] 执行 `openspec validate event-bus` 并修复校验问题
- [ ] 用户确认本 Change 文档后开始代码实现

## 契约与实现

- [ ] 复核 `IEventBus` 的三项操作和程序集归属，不创建独立 Contracts 程序集
- [ ] 复核 `SessionInitializedEvent` 的事实事件语义和中文 XML 文档
- [ ] 完善同步按订阅顺序发布、精确事件类型隔离和无订阅者行为
- [ ] 完善订阅去重、解除订阅、空参数和处理器异常传播边界
- [ ] 确认 `SessionRoot` 通过默认工厂创建并注入唯一 EventBus

## 验证

- [ ] 添加多订阅者、顺序和同步执行行为测试
- [ ] 添加解除订阅、类型隔离、重复订阅和无订阅者行为测试
- [ ] 添加处理器异常传播测试，确认不被静默吞掉
- [ ] 执行 Application/Adapters 针对性编译
- [ ] 执行 Unity EditMode 测试并记录真实通过数量
- [ ] 更新本 Change 的任务状态和验证结果

## 明确未覆盖

- [ ] 网络、跨进程、异步队列、线程安全和事件持久化
- [ ] 命令总线、事件重放、过滤器、优先级和重试
- [ ] 正式玩法事件和正式场景运行验证
