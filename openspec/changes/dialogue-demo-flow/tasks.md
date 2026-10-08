# Issue #7 对话剧情与 Demo 流程契约执行任务

## OpenSpec

- [ ] 读取并记录 Issue #7、现有对话契约、DemoEntryMode、SceneFlow、进度契约和调用方基线
- [ ] 完成 proposal、spec、plan、design、tasks 和 delta spec
- [ ] 执行 `openspec validate dialogue-demo-flow` 并修复校验问题
- [ ] 用户确认本 Change 文档后开始代码实现

## 纯数据和对话契约

- [ ] 定义稳定 `ContentId`、`DialogueRequest`、`DialogueResult`、`StoryFlag` 和 `EndingId.BadEnding`
- [ ] 将 `IDialogueService` 迁移到显式请求、Skip、Complete、播放状态和移动锁定状态
- [ ] 实现 `RecordingDialogueService`，无内容时返回明确 `Unavailable`
- [ ] 迁移 `UnavailableDialogueService`、SessionServices、烟测入口和现有测试调用方

## Demo 流程契约

- [ ] 定义统一 `IDemoFlow` 和 `DemoRunResult`
- [ ] 复用 `DemoEntryMode` 表达 Continue/Replay，不创建第二套场景流
- [ ] 实现 `RecordingDemoFlow`，记录 Enter、Retry、Exit 和三种明确结果
- [ ] 确认普通 `Failed` 不产生、不映射为 `BadEnding`
- [ ] 确认不创建 `EndingReachedEvent`

## 验证

- [ ] 添加 ContentId 校验和对话 Start/Skip/Complete 行为测试
- [ ] 添加移动锁定和对话完成结果测试
- [ ] 添加 Demo Succeeded/Failed/Abandoned 测试
- [ ] 添加 Retry、Exit、Continue、Replay 测试
- [ ] 添加剧情标记纯数据和坏结局隔离测试
- [ ] 执行相关程序集编译和 Unity EditMode 测试
- [ ] 更新本 Change 任务状态和真实验证记录

## 明确未覆盖

- [ ] JSON 导入、ScriptableObject 剧情资产和具体台词
- [ ] NPC、UI、失败弹窗、暂停菜单和实际 Demo 玩法
- [ ] 正式场景加载、剧情播放表现和 `EndingReachedEvent`
