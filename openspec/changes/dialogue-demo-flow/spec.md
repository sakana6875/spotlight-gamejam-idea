# 对话剧情与 Demo 流程契约规格

## 1. 稳定内容和剧情标记

### 要求：内容请求使用稳定 ContentId

`ContentId` MUST 是不依赖场景对象、NPC 名称或显示文案的稳定标识。空白或非法 ID MUST 返回明确的无效结果，不得当作成功播放。

### 要求：StoryFlag 是纯数据

`StoryFlag` MUST 只表达稳定剧情标记 ID，不持有 Unity 对象、场景引用或服务实例。设置和读取由已有进度/应用边界负责；本 Change 不把标记逻辑塞进 MonoBehaviour。

## 2. 对话生命周期

### 要求：对话服务使用显式请求

`IDialogueService` MUST 支持：

- `DialogueResult StartDialogue(DialogueRequest request)`
- `DialogueResult Skip()`
- `DialogueResult Complete()`
- `bool IsPlaying`
- `bool IsMovementLocked`

`DialogueRequest` MUST 至少包含 `ContentId` 和移动锁定意图。对话服务不得直接操作 UI、AudioSource、NPC 或场景对象。

### 要求：对话结果可观察

`DialogueResult` MUST 区分开始、跳过、完成、无效内容、不可用和当前没有播放等结果。`IsSuccess` 不得把 `Unavailable` 或无效请求当作成功。

## 3. Demo 统一流程

### 要求：入口模式和结果明确

`DemoEntryMode` MUST 区分 `Continue` 和 `Replay`；如现有契约包含 `New`，不得在本 Change 删除其已有语义。`DemoRunResult` MUST 区分：

- `Succeeded`
- `Failed`
- `Abandoned`

普通 `Failed` MUST NOT 自动产生或映射为 `BadEnding`。

### 要求：IDemoFlow 隔离场景对象

`IDemoFlow` MUST 用明确方法表达进入、重试和退出流程，并返回明确结果；调用方不得直接引用 Hub、Menu、NPC 或具体 Demo 场景对象。实现只通过稳定 `DemoId`、`DemoEntryMode` 和纯数据结果通信。

## 4. 记录适配器

### 要求：记录实现不伪造外部行为

`RecordingDialogueService` 和 `RecordingDemoFlow` MUST 只记录请求、调用顺序、当前状态和结果，不能把未实现的 UI、剧情资产或 Demo 玩法伪装成成功。对未提供真实内容的对话应返回明确 `Unavailable`；Demo 记录实现应允许测试显式得到三种结果。

## 5. 组合根和程序集

### 要求：依赖方向保持不变

契约 MUST 放在现有 Domain/Application 程序集可引用的位置，适配器放在 Adapters；不创建新的独立 Contracts 程序集。`SessionRoot` 继续是唯一组合根，其他模块不得自行创建或查找全局服务。

## 6. 验收测试

EditMode 测试 MUST 覆盖：三种 Demo 结果、Retry、Exit、Continue、Replay、ContentId 校验、对话 Skip/Complete、移动锁定状态、剧情标记纯数据行为和普通失败不触发 BadEnding。测试不得检查 Unity 私有字段或具体场景对象。
