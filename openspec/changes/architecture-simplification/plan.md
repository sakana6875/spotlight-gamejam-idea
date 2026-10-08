# 架构简化实施计划

## 实施顺序

### 1. 建立基线

- 读取 Issue #15、当前 OpenSpec、所有相关生产调用方、测试调用方和 asmdef 引用。
- 确认当前工作区已有改动，不覆盖与本 Change 无关的内容。
- 生成删除/延后清单，区分生产代码、测试替身、程序集和文档。
- 执行 OpenSpec 校验；用户确认本 Change 前不修改代码、资源、测试或配置。

### 2. 收敛 SessionRoot 组装

- 将固定服务创建和保存折叠到 `SessionRoot` 的明确组装路径。
- 迁移所有仍有真实调用方的初始化和依赖传递。
- 删除无调用方的 `SessionBootstrapper`、`ISessionServiceFactory`、`DefaultSessionServiceFactory` 或 `SessionServices` 层；若某类型仍有必要，只保留最小职责。
- 保留初始化幂等、失败可观察和 `DontDestroyOnLoad` 生命周期。
- 移除烟测入口的跨场景存活风险。

### 3. 移除无真实消费者的服务脚手架

- 根据实际调用图处理 `IEventBus`、`SessionInitializedEvent`、音频/输入/对话占位服务和对应内存/记录实现。
- 保留已经有真实行为价值的存档与进度边界。
- 不创建新的万能 `SessionServices`、静态注册表或 Service Locator。
- 将剩余服务通过场景入口以最小依赖提供给消费者。

### 4. 收窄场景边界

- 选择并保留唯一实际 Unity 场景加载边界。
- 将默认运行时从 `RecordingSceneFlow` 迁移到真实 Unity adapter，或在确认尚未接入时明确将其限定为测试用途；不得让 fake 冒充正式运行时。
- 删除没有调用方的顶层场景 API、未创建场景预登记和无生产来源的结果码。
- 固定加载结果的成功语义，确保记录实现和 Unity 实现一致。

### 5. 删除第二套 Demo/对话预留

- 通过生产引用和测试引用确认 `IDemoFlow`、`DemoRunResult`、`RecordingDemoFlow` 的迁移边界。
- 删除或延后通用 Demo 流程及其记录测试；不删除首个真实 Demo 必需的本地状态。
- 删除或延后没有真实消费者的对话生命周期、剧情标记、坏结局和不可用占位服务。
- 保留稳定内容 ID 或存档标记的已确认行为，不把普通失败映射为坏结局。

### 6. 收窄存档契约

- 保留并迁移检查点覆盖、永久进度保留、空存档/未知 ID/失败结果测试。
- 对 `ListSnapshots`、设置、Reset 和版本字段逐项确认是否有生产调用方；无调用方的契约与内部转发一并删除或延后。
- 不在本 Change 引入文件存档迁移、多个槽位或新存档格式。

### 7. 清理测试、程序集和目录

- 删除验证替身内部记录字段而非消费者行为的测试，保留必要领域边界测试。
- 将生产代码程序集保持为 `Spotlight.Domain` 与 `Spotlight.Game`。
- 将组合根、存档和场景代码迁移到 `Bootstrap`、`Save`、`Scene` 目录。
- 删除 `Architecture/Application`、`Architecture/Adapters`、`Architecture/Composition`、`Architecture/Contracts` 目录及其 `.meta`。
- 合并当前唯一场景加载边界为 `SceneLoader`；不再保留没有真实调用方的 `ISceneFlow`、`SceneCatalog`、`RecordingSceneFlow` 和场景结果模型。
- 合并当前存档实现为 `SaveService`；保留已验证的检查点和永久进度行为。
- 更新 `Spotlight.Domain`、`Spotlight.Game`、测试 asmdef、`.meta`、测试引用和生产调用方。
- 选择 canonical OpenSpec Change，处理旧 Change 的重复内容和历史状态。

### 8. 验证和交付

- 执行目标程序集编译和相关 EditMode 测试。
- 执行静态引用检查，确认无遗留旧目录、旧类型、Service Locator 或 `SceneManager` 越界引用。
- 使用 Unity Editor/PlayMode 验证实际受影响的场景生命周期；无法执行的部分明确写“Editor 验证未完成”。
- 将真实命令、测试结果、删除清单和未覆盖范围写回 `tasks.md`。

## 目录与程序集边界

本 Change 优先修改现有文件，不创建新的通用程序集。目标程序集为：

```text
Spotlight.Domain       # Assets/Scripts/Domain
Spotlight.Game         # Assets/Scripts/Bootstrap、Assets/Scripts/Save、Assets/Scripts/Scene、真实 Features/UI/Data
Spotlight.Tests.EditMode
Spotlight.Tests.PlayMode
```

`Spotlight.Domain` MUST 继续不依赖 Unity。`Spotlight.Game` 承载 SessionRoot、存档服务、场景边界、Unity 适配、Features、UI 和 Data。不再保留只表达分层的 `Architecture/Application`、`Architecture/Adapters`、`Architecture/Composition` 或空 `Features` 目录。Feature 程序集只在真实 Feature 需要独立隔离时创建。
## 资源保护

本 Change 不修改以下内容，除非后续任务明确证明其为必需迁移目标：

- `Assets/Scenes/SampleScene.unity`
- `ProjectSettings/EditorBuildSettings.asset`
- URP 资源
- `Packages/manifest.json`
- 非本 Change 产生的未提交资源和配置

如果为移除烟测装配必须修改 SampleScene，必须单独记录资源影响并通过 Unity Editor 保存，禁止手工编辑复杂 YAML。

## 验证路径

- `openspec validate architecture-simplification`
- 目标程序集编译
- 相关 EditMode 行为测试
- 静态生产引用和 asmdef 依赖检查
- Unity Editor/PlayMode 的实际场景烟测；未执行时标记“Editor 验证未完成”
