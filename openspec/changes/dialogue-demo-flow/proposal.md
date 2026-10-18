# 建立对话剧情与 Demo 通用流程最小契约

## 背景

当前项目已有最小 `IDialogueService.StartDialogue(string)`、`DemoEntryMode` 和 `ISceneFlow`，但对话生命周期、稳定内容 ID、剧情标记和 Demo 运行结果尚未形成统一契约。若玩法直接引用 NPC、Hub、Menu 或其他场景对象，跨场景流程会产生具体实现耦合。

Issue #7 需要建立可被 Application/Domain 使用、由适配器记录或替换的最小边界。但本项目当前阶段不应搭建完整剧情系统或万能 Demo 管理器，因此本 Change 只定义真实可验证的请求、结果和状态，不实现内容资产或玩法。

## 目标

1. 用稳定 `ContentId` 表达对话内容请求。
2. 明确对话开始、跳过、完成、播放状态和移动锁定状态。
3. 用纯数据契约表达 `StoryFlag`，不依赖场景对象。
4. 区分 Demo 成功、失败和主动退出，并用 `DemoEntryMode` 表达 Continue/Replay。
5. 将 Demo 运行和统一流程控制通过接口隔离于 Hub、Menu 与剧情对象。
6. 为 `EndingId.BadEnding` 保留值契约，但本 Issue 不创建或发布 `EndingReachedEvent`。

## 范围

- 在现有程序集边界内定义 `ContentId`、`DialogueRequest`、`DialogueResult`、`StoryFlag`。
- 将 `IDialogueService` 收敛为显式请求和生命周期操作：`StartDialogue(DialogueRequest)`、`Skip()`、`Complete()`，并暴露 `IsPlaying`、`IsMovementLocked`。
- 定义 `IDemoFlow`，统一表达 Continue、Replay、Retry、Exit 和 Demo 结果；不拆分成多个互相编排的通用服务。
- 定义 `DemoRunResult`，区分 `Succeeded`、`Failed`、`Abandoned`。
- 提供纯 C# `RecordingDialogueService` 和 `RecordingDemoFlow`，只记录调用、状态和最后结果，不引用 Unity 场景对象。
- 添加 EditMode 行为测试。

现有 `SessionServices` 和 `SessionRoot` 继续作为组合根注入边界；具体接口归属按现有程序集依赖确定，不创建独立 Contracts 程序集。

## 非目标

- 不实现 JSON 导入、ScriptableObject 剧情资产、具体台词或旁白。
- 不实现 NPC、UI、失败弹窗、暂停菜单、Demo 玩法或实际场景加载。
- 不把普通 Demo 失败自动转换为 BadEnding。
- 不创建或发布 `EndingReachedEvent`；真实结局流程出现时另行定义。
- 不创建万能 `GameManager`、Demo 超级控制器或跨 Feature 具体引用。
- 不增加异步对话、网络内容、存档迁移或事件重放。

## 主要决策

- 对话采用显式 `DialogueRequest`，避免把未来字段继续堆进字符串参数。
- Demo 采用统一 `IDemoFlow`，接受少量职责集中以避免当前阶段同时引入 `IDemoRun`、`IDemoRetryFlow` 两套编排接口。
- `Continue`、`Replay` 使用已有/统一的 `DemoEntryMode`；`Retry`、`Exit` 使用明确方法，结果不通过事件猜测。
- `EndingId.BadEnding` 只作为预留契约，不被普通失败路径使用。
