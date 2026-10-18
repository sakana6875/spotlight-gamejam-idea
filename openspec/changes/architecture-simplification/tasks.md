# 架构简化执行任务

## OpenSpec

- [x] 读取并记录 Issue #15、当前架构调用图、asmdef 引用和未提交工作区边界
- [x] 完成 proposal、spec、plan、design、tasks 和 delta spec
- [x] 执行 `openspec validate architecture-simplification` 并修复校验问题
- [x] 用户确认本 Change 文档后开始代码、测试、资源或配置实现

## 组合根收敛

- [x] 统计 SessionRoot、SessionBootstrapper、ISessionServiceFactory、DefaultSessionServiceFactory 和 SessionServices 的生产与测试调用方
- [x] 将固定服务组装折叠到 SessionRoot 的明确路径
- [x] 删除或延后无替换职责的 Bootstrapper/Factory/Services 多层转发
- [x] 保留初始化幂等、失败可观察和跨场景生命周期
- [x] 移除烟测入口的跨场景存活风险

## 服务和烟测脚手架

- [x] 统计 IEventBus、SessionInitializedEvent、音频/输入/对话端口及替身的真实消费者
- [x] 删除或延后默认运行时无真实消费者的全局端口和替身
- [x] 保留实际存档/进度行为边界，不创建万能 SessionServices 或 Service Locator
- [x] 收窄或删除 SessionSmokeEntry，并迁移仍有价值的初始化失败验证

## 场景流程

- [x] 统计 ISceneFlow、SceneCatalog、SceneLoadResult、RecordingSceneFlow 和 UnitySceneFlowAdapter 的生产调用方
- [x] 选择唯一真实场景加载边界并固定成功语义
- [x] 删除或延后无调用方的场景操作、未来场景预登记和无生产来源的结果码
- [x] 确认 Feature、Application 和 Domain 不直接引用 SceneManager

## Demo、对话和剧情

- [x] 统计 IDemoFlow、DemoRunResult、RecordingDemoFlow 及相关测试调用方
- [x] 删除或延后第二套通用 Demo 流程，保留首个真实 Demo 所需的本地状态边界
- [x] 统计 ContentId、StoryFlag、EndingId、DialogueRequest、DialogueResult 和对话适配器的真实消费者
- [x] 删除或延后无真实消费者的对话/剧情/坏结局预留，不把普通失败映射为坏结局

## 存档和进度

- [x] 保留 checkpointId 覆盖、永久进度保留、空存档和失败结果行为
- [x] 逐项确认 ListSnapshots、SettingsData、ResetSave 和版本字段是否有生产调用方
- [x] 删除或延后无调用方的存档契约，并迁移或删除对应测试
- [x] 确认内存实现不被描述为文件持久化

## 测试、程序集和文档

- [x] 删除或改写只验证 LastRequest/LastOperation 等替身内部字段的测试
- [x] 将生产代码程序集收敛为 Spotlight.Domain 与 Spotlight.Game
- [x] 将现有 Application/Adapters 代码迁移到 Spotlight.Game，保持目录职责但合并编译边界
- [x] 删除或延后空 Spotlight.Features 程序集及其 .meta
- [x] 更新 Domain、Game、测试 asmdef、.meta、测试引用和生产调用方
- [x] 选择 canonical OpenSpec Change：`architecture-simplification` 作为当前收敛规范，`architecture-foundation` 保留为历史实施记录

## 物理目录简化实施

- [x] 将 SessionRoot 和最小烟测迁移到 `Assets/Scripts/Bootstrap`
- [x] 将内存存档合并为 `Assets/Scripts/Save/SaveService.cs`，删除存档接口转发层
- [x] 将场景加载合并为 `Assets/Scripts/Scene/SceneLoader.cs`，删除场景目录预留和 Recording fake
- [x] 删除 `Assets/Scripts/Architecture/Application`、`Adapters`、`Composition`、`Contracts` 及空 `Features` 目录
- [x] 更新 Unity Editor 脚本、场景引用、测试命名空间和 asmdef 引用
- [x] 保留存档边界测试与 SampleScene PlayMode 烟测
## 验证

- [x] 执行目标程序集编译：Unity EditMode 批处理重新导入并编译通过
- [x] 执行相关 EditMode 测试：Unity 2022.3.62f3c1 批处理执行 6/6 通过（临时 XML 结果已清理）
- [x] 执行静态依赖检查：旧 `Architecture`/`Features` 目录无残留；旧架构命名空间和接口无生产/测试/编辑器引用；`SceneManager` 仅在 `SceneLoader`、PlayMode 测试和 Editor 装配工具中使用
- [x] 执行可用 Unity 验证：Unity 批处理执行 PlayMode 1/1 通过（临时 XML 结果已清理）
- [x] 回写任务验证记录：记录目录迁移、EditMode、PlayMode 和静态结构检查结果

### 当前验证记录

- `openspec validate architecture-simplification`：通过。
- Unity EditMode：6/6 通过；`SaveServiceTests` 保留存档行为边界。
- Unity PlayMode：1/1 通过；`SampleScene_InitializesRequiredSessionBoundaries`。
- 静态结构检查：`Assets/Scripts/Architecture*`、`Assets/Scripts/Features*` 和 `Assets/Editor/Architecture*` 无残留；旧架构类型引用无残留。
- Unity Editor 已使用 `D:/Unity/2022.3.62f3c1/Editor/Unity.exe` 执行验证。
