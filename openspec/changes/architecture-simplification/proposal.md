# 架构简化提案

## 背景

当前项目已经完成一套架构烟测骨架，但尚无真实 Feature C# 实现。默认生产组装主要由 `SessionRoot`、`SessionSmokeEntry` 和内存/记录/不可用适配器驱动。当前代码提前建立了多层组合根转发、七类全局服务端口、通用 EventBus、两套场景流程、通用 Demo 流程、完整对话剧情契约以及大量测试替身。

这些边界原则本身仍有价值，但在没有真实调用方前预建完整 API、结果模型和替身，增加了 GameJam 的阅读、调试和迁移成本。Issue #15 要求将架构收敛到真实 Feature 可以直接使用的规模。

## 目标

1. 保留 `SessionRoot` 作为唯一组合根。
2. 保留 `Spotlight.Domain` 作为纯 C# 依赖边界，合并 Unity 运行时代码为一个 `Spotlight.Game` 程序集，并保留独立 EditMode/PlayMode 测试程序集。
3. 删除或延后没有真实生产调用方的全局端口、记录适配器、结果模型和未来场景预留。
4. 将固定服务组装折叠到组合根，避免无替换实现的 Bootstrapper/Factory/Services 多层转发。
5. 让首个真实 Feature 按功能组织，并只接收实际需要的最小依赖。
6. 收缩测试和 OpenSpec 负担，使测试验证消费者可观察行为，而不是记录型替身内部字段。

## 范围

- 收敛 `SessionRoot`、`SessionBootstrapper`、`ISessionServiceFactory`、`DefaultSessionServiceFactory` 和 `SessionServices` 的组装链。
- 审核并删除或延后默认运行时没有真实消费者的 `IEventBus`、`SessionInitializedEvent`、对话/输入/音频占位服务及其替身。
- 将场景流程收敛为一个真实 Unity 加载边界，限制 `SceneManager` 依赖位置，并删除没有调用方的顶层场景 API。
- 删除或延后 `IDemoFlow`、`DemoRunResult`、`RecordingDemoFlow` 等第二套 Demo 流程抽象。
- 收窄对话、剧情和结局预留；保留稳定内容 ID 的实际价值，但不提前实现完整剧情系统。
- 保留检查点覆盖、永久进度保留和明确失败结果等已验证存档规则；按真实需求收窄额外设置和列表契约。
- 清理空 Feature 程序集、替身内部状态测试和重复 OpenSpec 状态。
- 不创建正式玩法、正式 UI、剧情资产、音频资源或新的通用管理器。

## 程序集策略

本 Change 将生产程序集收敛为：

```text
Spotlight.Domain       # 纯 C# 领域规则和数据，不引用 Unity
Spotlight.Game         # SessionRoot、Services、场景边界、Adapters、Features、UI 和 Data
Spotlight.Tests.EditMode
Spotlight.Tests.PlayMode
```

当前不保留独立的 `Spotlight.Application`、`Spotlight.Adapters` 或空的 `Spotlight.Features.*` 程序集。首个 Feature 出现后，只有在需要独立编译隔离或明确模块边界时，才新增对应 Feature 程序集。

这不是取消依赖规则：`Spotlight.Domain` 仍不得引用 Unity，`Spotlight.Game` 仍是唯一 Unity 运行时程序集，Feature 仍不得依赖其他 Feature 的具体实现。

## 物理目录收敛

程序集收敛后继续删除只表达架构层次、却没有真实 Feature 价值的目录层级。运行时代码按 `Bootstrap`、`Save`、`Scene` 和真实 `Features` 组织；不再保留 `Application`、`Adapters`、`Composition`、`Contracts` 四层目录。保留 `Spotlight.Domain` 仅用于已有纯 C# 存档规则，避免为简单玩法继续增加接口和转发层。
## 非目标

- 不复制 `ydqt-demo` 的静态 `GameContainer.Resolve<T>()` 或引入 Service Locator。
- 不引入跨模块业务静态单例、万能 GameManager 或万能上下文。
- 不删除 `SessionRoot`，不解除 Domain 与 Unity 的依赖边界。
- 不允许 Feature 直接依赖其他 Feature 的具体 MonoBehaviour。
- 不修改稳定存档 ID、Build Settings、URP、Packages 或非必要场景资源。
- 不实现首个 Demo 的具体玩法；该功能由独立 Feature Issue 定义。
- 不因为未来可能复用而保留无调用方抽象。

## 主要决策

### 1. 保留组合根，折叠固定组装

`SessionRoot` 继续负责创建和持有跨场景服务。当前只有一个组装方案，不再通过不可替换的工厂和初始化转发层表达可替换性。重复初始化、失败可观察和跨场景生命周期仍必须保留。

### 2. 真实边界优先于通用端口

输入、音频、对话和事件总线只有在真实 Feature 或外部资源需要时才建立对应接口。不得用 `SessionServices` 作为万能上下文，也不得用烟测替身证明正式运行时能力。

### 3. Feature 靠近 Unity，但职责清楚

普通 Feature 可以由多个职责明确的 MonoBehaviour 直接组合。只有复杂、可复用且不依赖 Unity 的规则才提取纯 C# Domain 类型。Feature 不直接调用 `SceneManager`，场景加载仍集中在一个窄边界中。

### 4. 存档核心规则不随架构简化丢失

检查点稳定 ID 覆盖、临时快照与永久进度分离、旧快照读取不撤销永久进度和失败可观察是行为约束，不属于可删除的脚手架。

## 风险

- 未来 Feature 出现时，可能需要重新建立一个更窄的专用端口；这是按真实调用方设计，而不是当前保留全部未来 API。
- 场景数量增加后，最小场景加载边界可能需要异步状态或加载 UI；这些应通过新的 Feature/场景 Change 明确扩展。
- 删除内存/记录替身后，部分验证必须迁移到真实 Domain 测试或 Unity Editor/PlayMode 烟测。

## 前置条件

本 Change 的文档已通过 `openspec validate architecture-simplification`，用户已确认程序集收敛方案。代码、测试、资源或配置实现按 `tasks.md` 顺序执行；Unity Editor 未执行的验证必须明确记录。