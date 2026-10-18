# 建立核心程序集与架构目录边界

## 背景

当前 Unity 项目只有 `Assets/Scenes/SampleScene.unity` 和现有 URP 2D 配置，尚未建立生产代码、测试代码和程序集边界。若直接把玩法、场景适配器和全局服务放入无边界目录，后续会形成跨模块引用、静态单例和 Unity 依赖泄漏，难以验证架构约束。

项目已确定使用组合根、Ports and Adapters 和分层依赖。实现前需要先建立能够被 Unity 识别、能够被测试引用、并且与 `AGENTS.md` 一致的目录和 asmdef 基础。

## 目标

1. 在 `Assets/Scripts` 下建立架构与 Feature 的目录边界。
2. 建立核心程序集定义，明确 Domain、Application、Adapters、Features 和测试程序集的引用方向。
3. 保证 Domain/Application 的基础程序集不依赖具体场景对象或未确认的外部实现。
4. 为后续 `SessionRoot`、事件总线、存档、场景、输入、音频和 Demo 契约提供稳定落点。
5. 保持现有 SampleScene、URP 2D Renderer 和项目资源不变。

## 范围

本次只完成 Issue #1 的程序集与目录基础：

- `Assets/Scripts/Architecture/Contracts/`
- `Assets/Scripts/Architecture/Domain/`
- `Assets/Scripts/Architecture/Application/`
- `Assets/Scripts/Architecture/Adapters/`
- `Assets/Scripts/Architecture/Composition/`
- `Assets/Scripts/Features/`
- `Assets/Tests/EditMode/`
- `Assets/Tests/PlayMode/`
- 对应 asmdef 与 `.meta` 文件。

程序集名称固定为：

- `Spotlight.Domain`
- `Spotlight.Application`
- `Spotlight.Adapters`
- `Spotlight.Features.*`
- `Spotlight.Tests.EditMode`
- `Spotlight.Tests.PlayMode`

本次只建立可编译的程序集边界和必要的最小占位入口，不实现具体全局服务、玩法流程或 Unity 场景行为。

## 非目标

- 不创建或修改 Bootstrap、Menu、Hub、Demo1、Demo2、Demo3、ChaosDemo 场景。
- 不修改 Build Settings、SampleScene 或现有 URP 资源。
- 不实现 `SessionRoot`、SceneFlow、Save、Audio、Input、Dialogue、EventBus 或 Demo 流程。
- 不添加正式剧情、音频、输入动作、存档格式或关卡数据。
- 不引入 Odin、第三方测试框架或新的 Unity 包。
- 不创建跨模块业务静态单例、Service Locator、万能 GameManager 或未确定行为的空接口。

## 主要决策

- `Contracts` 不单独创建程序集，契约按领域归属放入 Domain 或 Application，避免为小类增加无意义程序集。
- `Spotlight.Domain` 不引用 Unity 场景相关程序集；纯领域规则和数据契约优先放入此程序集。
- `Spotlight.Application` 只引用 Domain，不引用具体 Adapters。
- `Spotlight.Adapters` 引用 Application 和 Domain，用于后续 Unity 生命周期与外部系统适配。
- Feature 程序集只依赖公开契约、Domain 和 Application，不依赖其他 Feature 的具体实现。
- 测试程序集只引用被测试程序集和 Unity Test Framework，不反向成为生产程序集依赖。
- 目录和 asmdef 必须同时提交对应 `.meta` 文件；Unity 资源优先通过 Unity 识别和验证，不手工改写复杂 YAML。

## 风险

- asmdef 引用配置错误会导致 Unity 编译顺序或程序集解析失败，因此必须执行 Unity 重新导入/编译验证；若当前环境无法运行 Unity Editor，必须明确记录 Editor 验证未完成。
- 空目录无法被版本控制保留，因此每个生产程序集需要一个最小、真实且可编译的入口类型；该入口不得伪装成未实现的业务服务。
- 后续接口归属可能随着具体契约出现调整；本次只建立稳定的程序集边界，不提前创建未确认的业务接口。

## Issue 3：SessionRoot 与全局服务初始化骨架

在 Issue #1 的程序集边界和 Issue #2 的存档契约基础上，本阶段建立唯一组合根 `SessionRoot`。它负责创建并组装已确认的全局服务实现，通过显式依赖注入向场景入口和适配器提供接口，不让其他模块自行查找、创建或替换服务。

本阶段使用现有 `SampleScene` 作为临时启动验证目标，不创建正式 Bootstrap、Menu、Hub 或 Demo 场景。`SessionRoot` 只验证初始化、生命周期和依赖组装边界，不实现完整的场景、输入、音频、对话或存档业务流程。

### Issue 3 范围

- 在 `Assets/Scripts/Architecture/Composition/` 创建 `SessionRoot : MonoBehaviour`；
- 定义并组装 `IEventBus`、`ISceneFlow`、`ISaveService`、`IProgressService`、`IAudioService`、`IInputService` 和 `IDialogueService` 的最小明确契约；
- 由组合根创建当前阶段可验证的具体实现，并将接口依赖显式传递给启动入口；
- 使用 `DontDestroyOnLoad` 保持组合根跨场景存活；
- 防止同一运行周期重复初始化，并报告初始化失败原因；
- 使用 EditMode 行为测试验证初始化边界。

### Issue 3 非目标

- 不创建正式 Bootstrap 场景或修改 Build Settings；
- 不实现完整场景加载、文件存档、AudioMixer、Input Actions、Dialogue 资源或 Demo 流程；
- 不提供 `GameManager.Instance`、跨模块静态单例或 Service Locator；
- 不允许其他模块通过 `FindObjectOfType`、`FindFirstObjectByType` 或静态字段获取服务；
- 不为尚未确定的业务行为创建假成功、空方法或静默降级实现。

## Issue 4：场景流程契约与 Unity 场景适配器骨架

### 背景

Menu、Hub 和各个 Demo 后续需要作为独立场景加载。若场景加载逻辑散落在玩法脚本中，玩法模块就会直接依赖 `SceneManager`，同时难以测试未登记场景、重复加载和 Demo 入口参数。

### 目标

建立稳定的场景与 Demo 标识、统一场景流程端口和最小 Unity 场景加载适配器。调用方只依赖 `ISceneFlow`，具体 `SceneManager.LoadSceneAsync` 只能出现在场景适配器内。

### 范围

- 定义 `SampleScene`、`Bootstrap`、`Menu`、`Hub`、`Demo1`、`Demo2`、`Demo3`、`ChaosDemo` 稳定场景 ID；
- 定义 `DemoId`、`DemoEntryMode`、`SceneLoadResult` 和场景流程相关结果类型；
- 提供 `LoadScene`、`LoadMenu`、`LoadHub`、`EnterDemo`、`RestartDemo` 操作；
- 通过集中式 `SceneCatalog` 管理稳定 ID 到 Unity 场景名的映射；
- 提供只允许加载已登记场景的 `UnitySceneFlowAdapter`；
- 为已登记的 `SampleScene` 提供最小真实加载路径；
- 为未知或当前未创建的场景返回明确失败结果；
- 添加 EditMode 契约/记录型替身测试，并记录 `SampleScene` 实际加载烟测状态。

### 目录归属决策

Issue #4 原始描述中的 `Contracts/Scene/` 作为概念组织位置保留，但当前项目已明确不创建独立 Contracts 程序集。为避免脚本落入默认程序集，实际可执行场景契约继续放在已有 `Spotlight.Application` 的 `Application/Services/Scene/` 目录；Unity 实现场景放在 `Adapters/Scene/`。这沿用 Issue #3 的服务契约归属，不新增万能 Contracts 程序集。

### 非目标

- 不创建 Bootstrap、Menu、Hub 或任意 Demo 正式场景；
- 不修改 `SampleScene`、Build Settings、URP 资源或 Packages；
- 不实现解锁规则、检查点恢复、加载界面、玩家出生点或 Demo 业务状态；
- 不让 `SceneFlow` 持有具体场景对象或跨场景玩法引用；
- 不把未创建的场景报告为加载成功。

### 风险

Unity 场景名、Build Settings 和稳定 ID 若分散维护，后续场景重命名容易产生运行时错误。本阶段通过集中式目录和明确失败结果降低风险；由于不修改 Build Settings，除 `SampleScene` 外的场景只能登记为不可用或未创建，不能伪造成功。

### 多关卡范围收敛

一个 Demo 可以包含多个内部关卡，但 Issue #4 不提前实现通用 `IDemoLevelFlow`、独立关卡场景加载或完整关卡恢复编排。

当前约定：

- Demo 顶层场景通过 `ISceneFlow` 管理；
- Demo 内部关卡默认由该 Demo 的普通状态机或控制器管理；
- 只有确认某个 Demo 的关卡确实需要独立 Unity 场景时，才在对应 Demo Issue 中扩展场景流程端口；
- 关卡 ID 和检查点字段只在出现真实关卡恢复需求时加入存档契约；
- 不为未来可能出现的关卡拆分提前创建通用层、事件或适配器。

这样保留多关卡的产品方向，但避免在没有真实调用方前增加第二套场景系统。
## Issue #5：架构骨架集成与 SampleScene 烟测

### 背景与目标

Issue #3 和 Issue #4 已完成组合根、七类服务和场景流程骨架，但尚未在真实 Unity 生命周期中连通验证。本阶段将现有 `SessionRoot` 与最小烟测入口放入 `SampleScene`，验证初始化、同步事实事件、服务端口调用和事件订阅解除。

### 范围

- 增加无 Unity 依赖的 `SessionInitializedEvent`；
- 增加只依赖同一 GameObject `SessionRoot` 的 `SessionSmokeEntry`；
- 增加通过 Unity Editor API 重复执行的 `SampleScene` 场景装配工具；
- 增加 EventBus EditMode 行为测试和 SampleScene PlayMode 集成测试；
- 记录真实 Unity 验证结果和未覆盖的正式资源范围。

### 非目标

不创建正式 Bootstrap、Menu、Hub 或 Demo 场景，不修改 Build Settings、URP、Packages，不添加正式玩法、剧情、AudioMixer、Input Actions、文件存档或 UI。烟测中的对话不可用结果必须保持失败边界，不能伪装成功。

### 验收

SampleScene 启动后 `SessionRoot` 初始化成功，`SessionInitializedEvent` 同步收到；场景流程、存档、进度、音频、输入端口均返回预期成功，对话端口返回已知 `Unavailable`；订阅解除后不再收到事件；初始化或服务调用失败均可观察并使烟测失败。
***
## Issue #7：对话生命周期与 Demo 流程契约

### 背景与目标

现有全局服务只有不可用对话占位边界，尚未固定剧情内容 ID、移动锁定、跳过/完成生命周期，也没有统一表达 Demo 成功、失败和主动退出的流程端口。本阶段补齐可由纯 C# 验证的契约与记录型适配器，不把具体 UI、场景、坏结局或存档规则塞入通用服务。

### 范围

- 定义稳定 `ContentId`、`StoryFlag`、`EndingId` 和 `DialogueRequest` 数据契约；
- 将 `IDialogueService` 迁移为请求对象和明确 `DialogueResult` 生命周期结果；
- 实现记录型对话适配器，验证播放、移动锁定、跳过、完成和非法请求边界；
- 定义 `IDemoFlow`、`DemoRunResult` 和 `DemoFlowOperation`，实现记录型 Demo 进入、重试和退出适配器；
- 验证 Demo 成功、失败和主动退出互斥，且流程不自动触发 `BadEnding` 或其他剧情规则；
- 更新组合根默认服务、调用方和 EditMode 行为测试。

### 非目标

不创建剧情导入器、ScriptableObject 内容资产、正式 UI、音频播放、场景加载、Demo 玩法、解锁/完成持久化或坏结局判定；不添加通用 Demo 关卡流程、静态单例、兼容别名或隐式成功回退。

### 验收与验证

`ContentId` 和 `StoryFlag` 必须是稳定、可比较的纯值；非法对话请求必须返回明确失败；播放结束后必须解除移动锁定；Demo 结果必须区分成功、失败和主动退出。验证使用目标程序集编译、Unity EditMode 测试和既有 SampleScene PlayMode 烟测。
***

