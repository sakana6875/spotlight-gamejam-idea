# Issue #6 同步事件总线实施计划

## 实施顺序

1. 读取 Issue #6、现有 `IEventBus`、`InMemoryEventBus`、`SessionRoot`、`DefaultSessionServiceFactory` 和现有事件测试，确认不重复创建服务或程序集。
2. 初始化并校验本 Change；在用户确认 OpenSpec 前不修改生产代码、测试或资源。
3. 复核事件契约归属：继续使用现有 `Spotlight.Application.Services.IEventBus` 和 `SessionInitializedEvent`，除非实现过程中发现明确的程序集边界冲突；不创建独立 Contracts 程序集。
4. 按规格补足 `InMemoryEventBus` 的边界行为和中文 XML 文档；保持同步发布、精确类型隔离、订阅顺序和异常传播。
5. 通过 `SessionRoot`/默认工厂确认 EventBus 仍由组合根创建，并检查无静态入口、Service Locator 或重复实例。
6. 增加或调整 EditMode 行为测试，覆盖多订阅者、顺序、同步执行、解除订阅、类型隔离、异常传播、重复订阅、空参数和无订阅者发布。
7. 执行针对性编译和 Unity EditMode 测试；失败后定位修复，不放宽断言或吞异常。
8. 更新 `tasks.md`，写入真实命令、测试数量和未覆盖边界。

## 影响文件

预期涉及：

- `Assets/Scripts/Architecture/Application/Services/GlobalServiceContracts.cs`
- `Assets/Scripts/Architecture/Adapters/Session/InMemoryEventBus.cs`
- `Assets/Scripts/Architecture/Composition/DefaultSessionServiceFactory.cs`（仅在验证注入边界确需调整时）
- `Assets/Tests/EditMode/` 下事件总线行为测试
- 本 Change 的 OpenSpec 文档

不修改 SampleScene、Build Settings、URP 资源、Input Actions、AudioMixer 或正式玩法资源。

## 验证路径

- `openspec validate event-bus`
- `dotnet build Spotlight.Application.csproj --no-restore`
- `dotnet build Spotlight.Adapters.csproj --no-restore`
- Unity EditMode 批处理测试，记录通过总数和失败原因。
- 静态检查事件总线未引用 Unity 场景对象、静态单例或业务决策代码。
