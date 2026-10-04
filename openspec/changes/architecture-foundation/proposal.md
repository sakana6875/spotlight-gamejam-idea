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
