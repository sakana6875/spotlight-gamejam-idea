# Issue #6 现有事件总线行为固化计划

## 实施顺序

1. 读取 Issue #6、现有 `IEventBus`、`InMemoryEventBus`、`SessionRoot`、默认工厂和已有事件测试，确认现有实现已经覆盖的行为。
2. 校验本 Change；用户确认 OpenSpec 前不修改生产代码、测试或资源。
3. 不移动现有类型、不创建新事件类型、不增加 Contracts 程序集；只在现有边界内补齐行为测试和必要文档。
4. 如果测试发现现有实现缺少已确认的最小行为，仅做局部修复；不得借机引入异步、过滤、优先级、重试或业务编排。
5. 通过 `SessionRoot`/默认工厂确认 EventBus 仍由组合根创建，确认没有静态入口、Service Locator 或重复全局实例。
6. 增加或调整 EditMode 测试，覆盖多订阅者、订阅顺序、同步执行、解除订阅、类型隔离、异常传播、重复订阅、空参数和无订阅者发布。
7. 执行针对性编译和 Unity EditMode 测试；失败后定位修复，不放宽断言、不删除测试、不吞异常。
8. 将真实命令、测试数量和未覆盖范围写回 `tasks.md`。

## 预期影响文件

优先只涉及：

- `Assets/Scripts/Architecture/Adapters/Session/InMemoryEventBus.cs`（仅确有规格缺口时）
- `Assets/Scripts/Architecture/Application/Services/GlobalServiceContracts.cs`（仅文档或确有契约缺口时）
- `Assets/Tests/EditMode/` 下 EventBus 行为测试
- 本 Change 的 OpenSpec 文档

不修改 SessionRoot 组装方式、SampleScene、Build Settings、URP 资源、Input Actions、AudioMixer 或正式玩法资源。

## 验证路径

- `openspec validate event-bus`
- `dotnet build Spotlight.Application.csproj --no-restore`
- `dotnet build Spotlight.Adapters.csproj --no-restore`
- Unity EditMode 批处理测试，记录真实通过数量。
- 静态检查没有新增事件类型、静态服务入口或业务逻辑进入 EventBus。
