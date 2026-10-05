# 架构基础设计

## 设计目标

本变更只建立程序集和目录边界，为后续组合根、Ports、领域规则和 Unity 适配器提供稳定落点。它不实现任何完整业务流程。

## 目录设计

```text
Assets/
├── Scripts/
│   ├── Architecture/
│   │   ├── Contracts/
│   │   ├── Domain/
│   │   ├── Application/
│   │   ├── Adapters/
│   │   └── Composition/
│   └── Features/
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

`Contracts` 是代码组织目录，不单独创建程序集。契约的程序集归属必须由实际依赖决定，避免建立没有明确职责的万能 Contracts 程序集。

## 程序集设计

```text
Spotlight.Domain       → 基础运行库
Spotlight.Application  → Spotlight.Domain
Spotlight.Adapters     → Spotlight.Application, Spotlight.Domain
Spotlight.Features.*   → 公开契约所在程序集, Spotlight.Domain, Spotlight.Application
Spotlight.Tests.EditMode → 被测试程序集, Unity Test Framework
Spotlight.Tests.PlayMode → 被测试程序集, Unity Test Framework
```

Domain 不引用 Unity 场景相关程序集。Application 不引用具体适配器。只有 Adapters 和后续 Composition 承担 Unity 生命周期与具体实现组装。

## 空目录处理

Unity 需要实际资源或脚本才能持续保留目录。asmdef 本身是实际资源，可以保留程序集边界。除非编译器或 Unity 导入流程明确需要，否则不添加只有空方法、假返回值或未确定业务 API 的占位类。

如果某个程序集必须有入口代码，入口只能是无业务副作用、明确表达程序集职责的真实类型，并使用中文 XML 文档解释其约束；不得借入口类型提前实现后续 Issue。

## 资源保护

本变更不触碰现有场景、URP 资产、Build Settings 和 Packages。SampleScene 继续作为当前项目已有场景，不在本变更中改名或升级为 Bootstrap。

## 验证策略

先通过静态检查确认路径、asmdef 名称和引用关系，再由 Unity 2022.3.62f3c1 导入并编译。若 Agent 无法运行 Unity Editor，必须保留静态验证结果，并在任务和交付中明确标记 Editor 验证未完成。
## 存档契约设计

Issue #2 的纯 C# 数据模型和 Ports 归属 `Spotlight.Domain`，具体内存验证适配器归属 `Spotlight.Adapters`。虽然目录保留 `Contracts/` 作为架构组织落点，但当前不创建独立 Contracts 程序集；可执行契约必须位于已有程序集边界内，避免落入默认程序集。

```text
Assets/Scripts/Architecture/Domain/Save/
├── SaveContainer.cs
├── SaveSnapshot.cs
├── PermanentProgress.cs
├── SettingsData.cs
├── CheckpointData.cs
└── ISaveService.cs / IProgressService.cs

Assets/Scripts/Architecture/Adapters/Save/
├── InMemorySaveService.cs
└── InMemoryProgressService.cs
```

`SaveContainer` 同时持有永久进度、设置和按 `checkpointId` 索引的快照；服务操作返回明确结果对象，适配器不把内存状态伪装成可靠文件存档。读取快照时只替换临时状态，永久进度保持独立。

## Issue 2 验证策略

EditMode 测试通过 Ports 验证空存档、覆盖、旧快照读取、永久进度保留、未知 ID、重置和设置边界。测试不得检查私有字段或实现调用次数。Unity 批处理导入和 EditMode Test Runner 用于确认 asmdef、适配器和行为测试可编译运行。

## Issue 3 组合根与全局服务设计

`SessionRoot` 位于 `Composition` 程序集，是唯一知道具体服务实现的入口。`SessionBootstrapper` 保持初始化状态和幂等规则，`ISessionServiceFactory` 负责由组合根创建一组接口实现，`SessionServices` 只暴露接口引用给场景入口。

```text
SessionRoot : MonoBehaviour
    ↓ 创建
DefaultSessionServiceFactory
    ↓ 组装
SessionServices
    ├── IEventBus
    ├── ISceneFlow
    ├── ISaveService
    ├── IProgressService
    ├── IAudioService
    ├── IInputService
    └── IDialogueService
```

本阶段的场景、音频、输入和对话实现是可验证的内存或明确不可用适配器：它们只验证接口边界和失败结果，不伪装为 `SceneManager`、`AudioMixer`、Input Actions 或内容资产的正式实现。`InMemoryEventBus` 是同步强类型事件总线，只负责发布、订阅和解除订阅。

`SessionRoot.Initialize()` 在同一实例内重复调用时返回 `AlreadyInitialized` 并复用同一 `SessionServices`；首次成功后调用 `DontDestroyOnLoad`。当前不修改 `SampleScene`，因此实际场景启动和跨场景生命周期记录为“Editor 验证未完成”，由后续场景/Bootstrap Issue 完成。

组合根和服务实现不提供全局静态访问入口。测试通过 `ISessionServiceFactory` 注入抛出明确错误的替身验证失败路径，不依赖 Unity 全局对象查找。

## Issue 4 场景流程与适配器设计

Issue #4 复用 `Spotlight.Application` 中的场景服务契约，不创建独立 `Spotlight.Contracts` 程序集。虽然原 Issue 描述了 `Assets/Scripts/Architecture/Contracts/Scene/`，但当前架构已确定 Contracts 只是组织概念；将可执行脚本放入该目录会落入默认程序集或迫使项目增加无明确边界的程序集。因此实际落点为：

```text
Assets/Scripts/Architecture/
├── Application/Services/Scene/
│   ├── SceneId.cs
│   ├── DemoId.cs
│   ├── DemoEntryMode.cs
│   ├── SceneCatalog.cs
│   ├── ISceneFlow.cs
│   └── SceneLoadResult.cs
└── Adapters/Scene/
    ├── UnitySceneFlowAdapter.cs
    └── RecordingSceneFlow.cs
```

稳定 ID 是领域/应用层数据，不等于 Unity 场景名。`SceneCatalog` 是唯一映射入口，负责回答“该稳定 ID 是否登记、对应哪个场景名、当前适配器是否允许加载”。它不调用 Unity API，也不负责解锁和存档。

`ISceneFlow` 只表达场景流程请求和结果。`EnterDemo`、`RestartDemo` 接收 `DemoId` 与 `DemoEntryMode`，但本阶段不推进 Demo 解锁、恢复检查点或重置玩法状态；这些属于后续 Application 用例。`RecordingSceneFlow` 用于 EditMode 验证调用参数，`UnitySceneFlowAdapter` 是唯一可以调用 `SceneManager.LoadSceneAsync` 的类型。

未创建场景和未知 ID 必须失败，不得回退到 `SampleScene`。当前 Build Settings 和场景资源不修改，因此只有现有 `SampleScene` 可以作为真实加载烟测目标；其他稳定 ID 可以在目录中定义，但必须返回未登记/不可用结果。

现有 `InMemorySceneFlow` 将迁移为记录型场景流程适配器，保持 `SessionRoot` 的服务组装接口不变。迁移后所有调用方仍通过 `ISceneFlow`，不直接依赖具体适配器或 `SceneManager`。

## Issue 4 多关卡与复杂度边界

Demo 的多个内部关卡首先视为该 Demo 内部的业务状态：

```text
Demo2Controller
    └── currentLevel = Level02
```

只要关卡仍在同一个 Demo Unity 场景内，具体 Demo 使用自己的状态机或普通 C# 控制器管理，不经过通用 `ISceneFlow`。这样可以直接表达关卡目标、出生点和临时状态，不为简单枚举切换增加跨层接口。

只有后续确认关卡需要独立 Unity 场景时，才在对应 Demo 的专用 Issue 中增加：

- 稳定 `DemoLevelId`；
- 关卡归属校验；
- 独立场景加载操作；
- 关卡检查点字段；
- 具体恢复顺序和 PlayMode 验收。

当前不创建通用 `IDemoLevelFlow`，不在 `ISceneFlow` 中加入 `LoadDemoLevel`，也不创建 Additive 场景系统。场景流程模块只负责已经确认的顶层场景切换。

整体复杂度控制原则：

```text
真实跨模块边界 → 接口
Demo 内部规则   → 具体状态机/普通类
Unity API       → 对应 Adapter
未来可能需求   → 不提前抽象
```

`SessionRoot` 继续负责服务创建和组装，但不扩展为万能游戏管理器；`EventBus` 只在出现真实的多订阅方事实通知时使用，不把所有方法都改成事件。
## Issue #5 烟测设计

`SessionSmokeEntry` 是临时场景适配器，不承担业务规则。它在 `Awake` 中从同一 GameObject 读取 `SessionRoot`，显式调用幂等 `Initialize()`，成功后订阅并发布 `SessionInitializedEvent`，再通过 `SessionServices` 调用各接口。场景流程使用 `SceneId.SampleScene`，存档使用稳定检查点数据，音频使用 `SFX` 通道，输入注册 `Interact`，对话以 `Unavailable` 作为已知边界。

入口保持事件处理器引用并在销毁时解除订阅，避免跨场景的 `SessionRoot` 存活导致订阅泄漏。`[DefaultExecutionOrder(100)]` 只降低同对象生命周期顺序差异，不能替代缺失组合根校验。

Editor 工具通过 `EditorSceneManager.OpenScene`、对象组件去重和 `SaveScene` 完成可重复装配；不手工写入复杂 Unity YAML。PlayMode 测试只读取入口的公开观察属性，EditMode 测试验证初始化事件的同步发布与解除订阅。
由于 `Spotlight.Adapters` 已被 `Spotlight.Composition` 依赖，烟测入口不能放入 Adapters，否则会形成程序集循环引用。实际文件放在 `Assets/Scripts/Architecture/Composition/Smoke/SessionSmokeEntry.cs`，仍保持临时 Unity 适配器职责；它与 `SessionRoot` 同属 Composition 程序集，服务调用仍只依赖 `SessionServices` 接口。
***
***
