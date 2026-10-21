# 对话剧情与 Demo 流程最小契约

## ADDED Requirements

### Requirement: 对话请求使用稳定内容标识
系统 MUST 使用稳定 `ContentId` 和 `DialogueRequest` 请求对话；请求不得依赖场景对象或显示文案。

#### Scenario: 合法内容 ID 创建对话请求
- **WHEN** 调用方使用合法 `ContentId` 构造 `DialogueRequest`
- **THEN** 对话服务接收该请求并保留其移动锁定意图

#### Scenario: 空内容 ID 被拒绝
- **WHEN** 调用方提交空白或非法 `ContentId`
- **THEN** 服务返回明确的无效内容结果
- **AND** 不进入播放状态

### Requirement: 对话生命周期和移动锁定可观察
`IDialogueService` MUST 支持 `StartDialogue(DialogueRequest)`、`Skip()`、`Complete()`，并暴露 `IsPlaying` 和 `IsMovementLocked`。结果 MUST 区分成功生命周期、Unavailable、无效请求和未播放状态。

#### Scenario: 对话开始并锁定移动
- **WHEN** 记录适配器收到要求锁定移动的合法对话请求
- **THEN** 对话进入播放状态
- **AND** `IsMovementLocked` 为 true

#### Scenario: 对话跳过或完成后解除移动锁定
- **WHEN** 正在播放的对话执行 `Skip` 或 `Complete`
- **THEN** 返回对应结果
- **AND** 对话不再播放且移动锁定解除

### Requirement: Demo 统一流程区分入口和结果
系统 MUST 通过统一 `IDemoFlow` 表达 Continue、Replay、Retry 和 Exit；`DemoRunResult` MUST 区分 `Succeeded`、`Failed`、`Abandoned`。

#### Scenario: Continue 和 Replay 保留入口语义
- **WHEN** 调用方分别以 `Continue` 和 `Replay` 进入 Demo
- **THEN** 记录适配器保留对应入口模式
- **AND** 不把两者合并成无语义的布尔值

#### Scenario: 三种 Demo 结果可区分
- **WHEN** Demo 流程分别记录成功、普通失败和主动退出
- **THEN** 调用方能读取 `Succeeded`、`Failed`、`Abandoned` 三种不同结果

#### Scenario: Retry 和 Exit 通过接口表达
- **WHEN** 调用方请求 Retry 或 Exit
- **THEN** 记录适配器记录对应操作并返回明确结果
- **AND** 不要求调用方直接访问 Hub、Menu 或场景对象

### Requirement: 普通失败不触发坏结局
普通 `Failed` 结果 MUST NOT 自动映射为 `EndingId.BadEnding`。本 Change 只保留 `BadEnding` 值契约，不创建或发布 `EndingReachedEvent`。

#### Scenario: 普通失败保持普通失败
- **WHEN** Demo 流程返回 `Failed`
- **THEN** 结果仍为 `Failed`
- **AND** 不产生坏结局事件或隐式剧情标记

### Requirement: 记录适配器与场景解耦
`RecordingDialogueService` 和 `RecordingDemoFlow` MUST 不引用 Unity 场景对象、NPC、Hub、Menu 或具体玩法控制器。

#### Scenario: 记录调用不依赖场景对象
- **WHEN** EditMode 测试调用记录适配器
- **THEN** 可以只用纯 C# 数据读取请求、状态和结果
