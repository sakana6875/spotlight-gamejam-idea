# Issue #7 对话剧情与 Demo 流程设计

## 现状

当前 `IDialogueService` 只有字符串 `StartDialogue`，实现为 `UnavailableDialogueService`；`DemoEntryMode` 已位于 Scene Application 契约中，`ISceneFlow` 已支持顶层 Demo 进入和重启。Issue #7 在这些边界上补齐纯数据和流程结果，不重新设计场景流。

## 主要决策

### 1. 对话请求显式化

使用 `ContentId` 和 `DialogueRequest` 替代裸字符串。请求携带是否锁定移动的意图；服务公开 `IsPlaying` 和 `IsMovementLocked`，但不直接操作玩家、UI 或输入适配器。

对话生命周期由 `StartDialogue`、`Skip`、`Complete` 三个明确操作表示。尚无真实内容资产时，记录适配器返回 `Unavailable`，不得伪造成功。

### 2. Demo 使用统一流程接口

本次采用用户确认的统一 `IDemoFlow`，不同时创建 `IDemoRun` 和 `IDemoRetryFlow` 两个通用编排接口。统一接口只处理流程意图：进入、重试、退出；`Continue` 和 `Replay` 仍由 `DemoEntryMode` 表达，运行结果由 `DemoRunResult` 表达。

这样保留最小调用面，避免把尚未存在的 Demo 玩法状态机提前抽象进架构。

### 3. 结果与坏结局隔离

`Succeeded`、`Failed`、`Abandoned` 是互斥的运行结果。普通 `Failed` 只表示本次 Demo 失败，不修改剧情结局。`EndingId.BadEnding` 仅保留稳定值契约；本 Change 不创建 `EndingReachedEvent`，也不由任何记录适配器发布坏结局。

### 4. 记录适配器只记录，不伪造

`RecordingDialogueService` 记录请求和生命周期调用；`RecordingDemoFlow` 记录入口模式、Retry、Exit 和显式结果。它们是测试替身/最小适配器，不依赖 Unity 场景对象，不承担业务规则。

## 不做的抽象

不创建剧情图、对话脚本解释器、Demo 超级控制器、统一游戏管理器、事件驱动结局编排器、JSON 运行时解析或 UI 状态机。真实需求出现时通过新的 OpenSpec 变更扩展。
