# Issue #7 对话剧情与 Demo 流程契约实施计划

## 实施顺序

1. 读取 Issue #7、现有 `IDialogueService`、`DialogueServiceResult`、`DemoEntryMode`、`ISceneFlow`、进度契约和组合根工厂，确认迁移边界。
2. 初始化并校验本 Change；用户确认 OpenSpec 前不修改代码、资源或测试。
3. 在现有程序集边界内定义纯数据契约和明确结果，不创建独立 Contracts 程序集。
4. 将现有 `IDialogueService` 从字符串入口迁移为 `DialogueRequest` 生命周期接口，迁移 `UnavailableDialogueService`、`SessionServices`、烟测入口和所有测试调用方。
5. 定义统一 `IDemoFlow`、`DemoRunResult` 和 `RecordingDemoFlow`；复用 `DemoEntryMode`，不新增第二套场景加载系统。
6. 定义 `ContentId`、`StoryFlag`、`DialogueRequest`、`DialogueResult` 和 `EndingId.BadEnding`；本 Change 不创建 `EndingReachedEvent`。
7. 添加 EditMode 行为测试，覆盖请求校验、播放状态、移动锁定、Skip/Complete、三种 Demo 结果、Retry/Exit/Continue/Replay 和坏结局隔离。
8. 执行针对性编译与 EditMode 测试；失败后继续修复，不放宽结果断言。
9. 更新 `tasks.md`，记录真实文件、命令、测试数量和未覆盖的正式内容。

## 预期影响文件

- `Assets/Scripts/Architecture/Application/Services/GlobalServiceContracts.cs` 或对应 Application 契约文件
- `Assets/Scripts/Architecture/Application/Services/Scene/DemoEntryMode.cs`
- `Assets/Scripts/Architecture/Domain/` 下纯数据契约
- `Assets/Scripts/Architecture/Adapters/Session/UnavailableDialogueService.cs` 的迁移或替换
- `Assets/Scripts/Architecture/Adapters/Dialogue/RecordingDialogueService.cs`
- `Assets/Scripts/Architecture/Adapters/Demo/RecordingDemoFlow.cs`
- `Assets/Scripts/Architecture/Composition/DefaultSessionServiceFactory.cs`
- `Assets/Scripts/Architecture/Adapters/Smoke/SessionSmokeEntry.cs`（迁移调用）
- `Assets/Tests/EditMode/` 下相关测试
- 本 Change 的 OpenSpec 文档

不修改正式场景、Build Settings、URP、Input Actions、AudioMixer 或剧情资产。

## 验证路径

- `openspec validate dialogue-demo-flow`
- Application、Domain、Adapters、Composition 和 EditMode 测试程序集针对性编译
- Unity EditMode 测试
- 静态检查无 `FindObjectOfType`、场景对象引用、静态服务单例和普通失败到 BadEnding 的隐式映射
