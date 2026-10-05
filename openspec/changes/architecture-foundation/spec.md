# 架构基础规格

## 1. 目录边界

### 要求：生产代码必须位于 Assets/Scripts

项目生产代码 MUST 位于 `Assets/Scripts/` 的嵌套目录中，不得在项目根目录创建 `AssetsScripts`、`AssetsData`、`AssetsTests` 等拼接名称目录。

`Assets/Scripts/Architecture/` MUST 至少包含：

- `Contracts/`
- `Domain/`
- `Application/`
- `Adapters/`
- `Composition/`

`Assets/Scripts/Features/` MUST 作为后续 Feature 程序集的父目录。

测试代码 MUST 位于 `Assets/Tests/EditMode/` 或 `Assets/Tests/PlayMode/`。

### 要求：程序集定义必须与目录边界一致

每个生产程序集 MUST 有对应的 `.asmdef` 和 `.meta` 文件。程序集定义不得通过无关目录或隐式默认程序集绕过边界。

## 2. 程序集与依赖

### 要求：Domain 保持 Unity 无关

`Spotlight.Domain` MUST 不引用 `UnityEngine`、Unity 场景、UI、AudioSource、SceneManager 或具体存档实现程序集。

Domain 代码只能依赖 .NET 基础类型和本程序集内的领域代码。若当前没有领域规则需要实现，不得为了填充目录创建假服务或空业务接口。

### 要求：Application 依赖方向

`Spotlight.Application` MUST 只依赖 `Spotlight.Domain` 及必要的基础运行库，不得引用 `Spotlight.Adapters` 或具体 Unity 适配器。

### 要求：Adapters 依赖方向

`Spotlight.Adapters` MAY 依赖 `Spotlight.Application` 和 `Spotlight.Domain`，用于承载 Unity 生命周期和外部系统适配。生产代码不得反向让 Domain 或 Application 引用 Adapters。

### 要求：Feature 依赖方向

每个 `Spotlight.Features.*` 程序集 MUST 只依赖公开契约所在程序集、`Spotlight.Domain` 和 `Spotlight.Application`，不得引用其他 Feature 的具体程序集或具体 MonoBehaviour。

### 要求：测试依赖方向

`Spotlight.Tests.EditMode` 和 `Spotlight.Tests.PlayMode` MUST 只引用被测试生产程序集以及 Unity Test Framework 所需程序集。生产程序集 MUST NOT 引用测试程序集。

## 3. 当前交付范围

### 要求：已完成的架构边界

Issue #1 已交付目录、asmdef 和依赖边界；本变更后续任务可以在这些边界内实现已确认的纯 C# 契约和内存适配器。

### 要求：实现必须遵守已确认范围

每个后续 Issue MUST 只实现其已确认的契约、适配器和验证，不得顺手创建正式场景、文件存档、静态单例、Service Locator 或未确认的业务流程。

### 要求：入口类型不能是假实现

为了让程序集被 Unity 保留和编译，可以提供说明程序集职责的最小真实类型，但不得提供假成功、空方法、未实现业务返回值或误导性服务入口。

## 4. 存档、永久进度与设置契约

### 要求：纯数据模型不依赖 Unity

`SaveContainer`、`SaveSnapshot`、`PermanentProgress`、`SettingsData` 和 `CheckpointData` MUST 只使用 .NET 基础类型、不可变值和稳定字符串 ID，不得引用 `UnityEngine`、场景对象、`MonoBehaviour` 或具体存档实现。

`SaveSnapshot` MUST 包含稳定且唯一的 `checkpointId`、场景 ID、检查点数据和可重建的临时状态。`PermanentProgress` MUST 与临时快照分离。

### 要求：存档服务返回明确结果

`ISaveService` MUST 提供 `HasSave`、`SaveCheckpoint`、`LoadLatestSnapshot`、`LoadSnapshot`、`ListSnapshots` 和 `ResetSave`。不存在快照、空存档和重置结果 MUST 可观察，不得返回假成功或静默降级。

`IProgressService` MUST 支持 Demo 解锁、完成状态和剧情标记的查询与更新。`SettingsData` MUST 提供后续音频服务使用的稳定设置字段，包括 `masterVolume`、`bgmVolume`、`sfxVolume` 和 `narrationVolume`。

### 要求：内存适配器只承担验证职责

内存存档适配器 MUST 使用 `checkpointId` 作为唯一键，同一 ID 的后写入快照覆盖旧快照，并保留独立的永久进度。适配器不得声称完成正式文件持久化，不得序列化 Unity 对象。

### 验收

1. 空存档、未知快照 ID 和重置操作均返回明确结果。
2. 同一 `checkpointId` 的后写入快照覆盖旧快照。
3. 读取旧快照不会撤销永久进度。
4. `SettingsData` 可被后续音频服务读取。
5. EditMode 测试覆盖上述边界行为。

## 5. 资源与现有项目保护

### 要求：不改变现有 Unity 资源

本次 MUST 保持以下内容不变：

- `Assets/Scenes/SampleScene.unity`
- `Assets/Settings/Renderer2D.asset`
- `Assets/Settings/UniversalRP.asset`
- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`

不得创建正式场景或改变 Build Settings。

## 6. 总体验收

满足以下条件才算完成：

1. 正确的嵌套目录位于 `Assets/Scripts` 和 `Assets/Tests` 下，项目根目录不存在 `AssetsScripts*`、`AssetsData*` 或 `AssetsTests*` 形式的错误目录。
2. 所有目标 asmdef 和 `.meta` 文件存在，名称与程序集规范一致。
3. asmdef 引用方向满足 Domain → Application → Adapters 的反向禁止关系，以及 Feature 和测试边界。
4. Unity 能识别这些 asmdef；Domain/Application 不因 Unity 场景依赖而产生编译引用。
5. 现有 SampleScene、URP 2D 资源和包依赖未被修改。
6. 针对可执行环境完成程序集编译或 Editor 验证；无法由 Agent 执行的 Unity Editor 验证必须明确标注“Editor 验证未完成”。

## 7. SessionRoot 与全局服务初始化

### 要求：唯一组合根

`SessionRoot` MUST 是当前运行周期创建和组装全局服务的唯一组合根。其他生产模块 MUST NOT 通过静态单例、Service Locator、`FindObjectOfType` 或 `FindFirstObjectByType` 查找、创建或替换全局服务。

`SessionRoot` MUST 位于 `Assets/Scripts/Architecture/Composition/SessionRoot.cs`，并负责依赖组装，而不是承载菜单、Demo、场景或存档业务规则。

### 要求：最小全局服务契约

本阶段 MUST 为以下服务提供最小且可观察的接口契约，并由组合根负责提供实例：

- `IEventBus`；
- `ISceneFlow`；
- `ISaveService`；
- `IProgressService`；
- `IAudioService`；
- `IInputService`；
- `IDialogueService`。

已经在 Issue #2 建立的 `ISaveService` 和 `IProgressService` MUST 复用，不得创建重复接口或平行实现。其余接口只允许声明当前已确认的最小操作；未确认的完整业务能力 MUST NOT 以假成功、空方法或静默降级形式加入。

### 要求：显式依赖注入

场景入口和适配器 MUST 通过构造器、初始化方法或明确的依赖参数接收服务接口。服务使用方 MUST NOT 自行创建具体服务、缓存跨模块静态引用或依赖具体实现类型。

组合根 MUST 能向启动入口提供完整的服务依赖集合，使后续场景适配器可以只依赖接口。初始化失败 MUST 返回或记录明确的失败原因，不能继续伪装为初始化成功。

### 要求：初始化幂等和生命周期

同一运行周期内重复触发初始化 MUST 不创建第二组全局服务。`SessionRoot` MUST 在首次有效初始化后使用 `DontDestroyOnLoad` 跨场景存活；重复入口 MUST 被明确拒绝或复用已完成初始化的组合根，并可观察其结果。

本阶段 MUST 保持现有 `SampleScene` 不变，不创建正式 Bootstrap 场景，不修改 Build Settings。`SampleScene` 的临时验证入口可以承载 `SessionRoot` 的初始化烟测，但不得把该场景升级为正式流程入口。

### 验收场景

#### 场景：首次启动完成服务组装
- **当** `SampleScene` 启动并触发 `SessionRoot` 初始化
- **那么** 七类服务均由组合根创建或接收明确实现，初始化结果可观察，且启动入口只接收接口依赖。

#### 场景：重复初始化不创建重复服务
- **当** 同一运行周期再次触发 `SessionRoot` 初始化
- **那么** 不创建第二组服务，并返回或记录明确的已初始化结果。

#### 场景：初始化失败可观察
- **当** 某个必需服务创建或组装失败
- **那么** 初始化返回失败结果或报告包含服务标识和原因的错误，不能静默跳过该服务。

#### 场景：模块不能自行查找服务
- **当** 场景适配器或玩法模块使用全局服务
- **那么** 它通过显式接口依赖获得服务，不通过静态单例、Service Locator 或 Unity 全局查找获得服务。

#### 场景：组合根跨场景存活
- **当** 场景发生切换
- **那么** `SessionRoot` 不因场景卸载而销毁，且不得因此重复创建服务。

### Issue 3 保护边界

本阶段 MUST 不修改 `Assets/Scenes/SampleScene.unity`、`ProjectSettings/EditorBuildSettings.asset`、URP 资源或 `Packages/manifest.json`。Unity 场景启动和 `DontDestroyOnLoad` 的实际 Editor/PlayMode 验证若未由 Agent 执行，任务记录 MUST 标记“Editor 验证未完成”。

## 8. 场景流程契约与适配器

### 要求：稳定场景与 Demo 标识

场景流程 MUST 使用稳定英文 ID，不得让调用方直接传递散落的 Unity 场景名称。首批场景 ID MUST 覆盖：`SampleScene`、`Bootstrap`、`Menu`、`Hub`、`Demo1`、`Demo2`、`Demo3` 和 `ChaosDemo`。

Demo 入口 MUST 使用稳定 `DemoId` 和明确的 `DemoEntryMode`，至少区分正常进入、继续和重新开始所需的入口语义；本阶段只定义契约，不实现解锁或恢复规则。

### 要求：场景流程端口

`ISceneFlow` MUST 提供以下操作：

- `LoadScene`；
- `LoadMenu`；
- `LoadHub`；
- `EnterDemo`；
- `RestartDemo`。

每个操作 MUST 返回可观察的 `SceneLoadResult` 或等价结果对象，至少区分成功、无效 ID、未登记场景、当前场景和加载失败。调用方 MUST 只依赖端口，不引用 `SceneManager` 或具体 Unity 场景名。

### 要求：集中式场景目录

`SceneCatalog` MUST 集中管理稳定场景 ID 到 Unity 场景名的映射，并提供登记查询。映射不存在、场景尚未创建或场景未被当前适配器支持时 MUST 返回明确失败，不得自动回退到 `SampleScene` 或报告假成功。

### 要求：Unity 适配器边界

只有 `UnitySceneFlowAdapter` MAY 调用 `SceneManager.LoadSceneAsync`。适配器 MUST 在发起加载前校验稳定 ID 和目录登记状态；加载请求使用异步 API，并将 Unity 加载异常或失败转换为可观察结果。`Application` 和 `Domain` MUST 不引用 `UnityEngine.SceneManagement`。

Issue #4 的可执行契约继续放在 `Spotlight.Application` 的 `Application/Services/Scene/` 目录，不创建独立 Contracts 程序集；Unity 实现放在 `Spotlight.Adapters` 的 `Adapters/Scene/` 目录。

### 验收场景

#### 场景：登记的 SampleScene 可请求加载

- **当** 调用方使用 `SampleScene` 稳定 ID 请求加载
- **那么** 记录型适配器返回明确成功；Unity 适配器仅在场景登记且资源可用时发起加载。

#### 场景：未知或未创建场景不可假成功

- **当** 调用方使用未知 ID，或使用已定义但当前未创建/未登记的场景 ID
- **那么** 返回明确失败原因，不调用 Unity 场景加载 API，也不回退到其他场景。

#### 场景：Demo 入口保留模式

- **当** 调用方使用稳定 `DemoId` 和 `DemoEntryMode` 进入或重启 Demo
- **那么** 适配器/记录型替身收到对应的 Demo 标识和模式；本阶段不自行推进解锁、存档或玩法状态。

#### 场景：调用方不依赖 SceneManager

- **当** Application、Gameplay 或 UI 请求场景切换
- **那么** 它们只调用 `ISceneFlow`，`SceneManager` 依赖仅存在于 Adapter。

### Issue 4 保护边界

本阶段 MUST 不修改 `Assets/Scenes/SampleScene.unity`、`ProjectSettings/EditorBuildSettings.asset`、URP 资源或 `Packages/manifest.json`。`SampleScene` 实际加载烟测若未由 Agent 在 Unity Editor/PlayMode 中执行，任务记录 MUST 标记“Editor 验证未完成”。

### 多关卡复杂度边界

一个 Demo MAY 包含多个内部关卡，但 Issue #4 只定义顶层场景流程，不要求创建通用 Demo 关卡流程系统。

当前 MUST 遵守：

- Demo 顶层场景使用 `ISceneFlow`；
- Demo 内部关卡默认由具体 Demo 的 Application/Domain 状态机管理；
- Demo 玩法代码不得直接调用 `SceneManager`；
- 未确认需要独立 Unity 场景前，不得添加 `IDemoLevelFlow`、`LoadDemoLevel`、`RestartDemoLevel` 或通用关卡适配器；
- 只有具体 Demo Issue 确认关卡需要独立 Unity 场景时，才新增对应稳定 ID、加载操作、存档字段和恢复验收；
- 关卡流程不得因此自动承担 Demo 解锁、完成、剧情或坏结局规则。

若当前 Demo 只是在同一场景中切换区域或阶段，应使用该 Demo 自己的普通状态机，不经过 `ISceneFlow`。

### 架构复杂度边界

本变更遵循“保留真实边界，延后未来抽象”：

- 保留 `SessionRoot`、`ISaveService`、`IProgressService` 和 `ISceneFlow` 等已有真实跨模块边界；
- 不为没有真实调用方的功能增加通用 EventBus 事件、Demo 关卡端口、场景栈、Additive 场景系统或通用编排器；
- 新增接口必须对应已确认的调用方、替换需求或可验证的外部边界；
- Demo 内部规则优先使用具体 Demo 的普通 C# 类、状态机和组件组合；
- 当前 Issue #4 的验收只覆盖顶层场景 ID、场景目录、场景加载适配器和明确失败结果。
## 9. Issue #5 架构骨架集成与 SampleScene 烟测

### 要求：初始化事实事件

`SessionInitializedEvent` MUST 是无 Unity 依赖的只读值类型，只表示组合根初始化成功完成。烟测入口在 `SessionRoot.Initialize()` 成功后通过 `SessionServices.EventBus` 同步发布，不得把 EventBus 依赖塞入 `SessionBootstrapper`。

### 要求：烟测入口依赖与观察结果

`SessionSmokeEntry` MUST 通过同一 GameObject 的 `GetComponent<SessionRoot>()` 获取组合根，不得使用 Unity 全局查找、静态服务入口或创建第二个组合根。入口必须公开可观察的完成、失败、事件接收以及六类服务结果属性：场景、存档、进度、音频、输入和对话不可用。

初始化失败 MUST 不发布成功事件、不继续调用服务，并记录包含原因的错误。组件必须解除事件订阅；正式游戏逻辑、UI、文件存档、音频播放和场景跳转不属于该入口。

### 要求：SampleScene 装配

Editor 工具 MUST 通过 `EditorSceneManager` 打开并保存 `Assets/Scenes/SampleScene.unity`，复用名为 `ArchitectureSmoke` 的对象，并确保其只有一个 `SessionRoot` 和一个 `SessionSmokeEntry`。工具不得修改 Build Settings 或其他资源，不得手工编辑场景 YAML。

### 验收场景

- **当** SampleScene 启动：**那么** 入口在有限帧数内完成初始化并收到同步事件，`SessionRoot.IsInitialized` 为真。
- **当** 入口调用七类服务端口：**那么** `SceneFlow.LoadScene(SceneId.SampleScene)`、有效 `SaveCheckpoint`、`SetVolume("SFX", 0.5f)` 和注册 `Interact` 成功；对话返回 `DialogueServiceResultCode.Unavailable`。
- **当** 入口解除事件订阅后再次发布：**那么** 已解除处理器不再收到事件。
- **当** 组合根或服务创建失败：**那么** 入口失败并保留可定位原因，不伪装完成。

### 保护范围与限制

本阶段仅验证 SampleScene 架构连通性；Bootstrap、Menu、Hub、Demo、正式 UI、Input Actions、AudioMixer、文件存档和剧情资产不在覆盖范围。未实际执行的 Editor/PlayMode 验证 MUST 标记“Editor 验证未完成”。
***
