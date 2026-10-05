# 架构基础执行任务

## Issue #1：架构边界

- [x] 读取并记录 Unity 版本、Packages、现有场景、URP 2D Renderer 和当前根目录状态
- [x] 创建 `Assets/Scripts/Architecture`、`Assets/Scripts/Features` 和 `Assets/Tests` 的嵌套目录
- [x] 创建核心 asmdef 并确认依赖方向
- [x] 创建 EditMode 与 PlayMode 测试程序集
- [x] 为所有 asmdef 生成并保留对应 `.meta` 文件
- [x] 检查项目根目录不存在错误拼接目录
- [x] 执行 Unity 导入、编译和 EditMode 测试程序集验证
- [x] 检查 SampleScene、URP 资源、Build Settings 和 Packages 未被修改

## Issue #2：存档、永久进度与设置契约

- [x] 更新存档契约规格、设计、实施计划和执行任务
- [x] 验证 Issue #2 OpenSpec 变更：`openspec validate architecture-foundation` 通过
- [x] 实现纯 C# 存档、检查点、永久进度和设置数据契约
- [x] 实现 `ISaveService` 和 `IProgressService`
- [x] 实现内存存档与进度适配器
- [x] 添加空存档、覆盖、旧快照和永久进度保留行为测试
- [x] 添加未知快照、重置和设置数据边界测试
- [x] 执行 Unity 导入编译与 EditMode 测试：Unity 编译返回码 0；6 个 EditMode 测试全部通过
- [x] 同步任务状态并记录验证结果

## Issue #3：SessionRoot 与全局服务初始化骨架

- [x] 更新 Issue #3 的提案、规格、实施计划和执行任务
- [x] 验证 Issue #3 OpenSpec 变更：`openspec validate architecture-foundation` 通过
- [x] 确认七类全局服务契约的程序集归属并复用 Issue #2 存档接口
- [x] 实现七类服务的最小明确契约和可观察结果
- [x] 实现可验证的最小服务适配器，不使用假成功、空方法或静默降级
- [x] 实现 `SessionRoot` 唯一组合根和显式依赖组装
- [x] 实现重复初始化保护、失败报告和跨场景生命周期处理
- [x] 添加首次初始化、重复初始化和初始化失败行为测试
- [x] 检查无静态单例、Service Locator 和 Unity 全局查找服务路径
- [x] 执行 Unity 导入编译与相关 EditMode 测试：编译返回码 0；10 个测试全部通过
- [x] 执行 SampleScene/跨场景生命周期验证：Editor 验证未完成；按 Issue #3 非目标未修改 SampleScene，已通过 EditMode 初始化行为验证
- [x] 同步任务状态并记录验证结果

## Issue #4：场景流程契约与 Unity 场景适配器骨架

- [x] 更新 Issue #4 的提案、规格、实施计划和设计记录
- [x] 验证 Issue #4 OpenSpec 变更：`openspec validate architecture-foundation` 通过
- [x] 确认现有 `ISceneFlow` 和 `InMemorySceneFlow` 的迁移边界，不创建独立 Contracts 程序集
- [x] 定义稳定 `SceneId`、`DemoId`、`DemoEntryMode`、`SceneCatalog` 和 `SceneLoadResult`
- [x] 扩展场景流程端口：`LoadScene`、`LoadMenu`、`LoadHub`、`EnterDemo`、`RestartDemo`
- [x] 实现记录型场景适配器并覆盖有效、未知和未登记场景结果
- [x] 实现唯一 `UnitySceneFlowAdapter` 的 `SceneManager.LoadSceneAsync` 边界
- [x] 确认 Application、Gameplay、UI 和 Composition 不直接依赖 `SceneManager`
- [x] 添加场景登记、失败结果、Demo 入口模式和调用参数行为测试
- [x] 执行 Unity 导入编译与相关 EditMode 测试：编译成功；16 个 EditMode 测试全部通过
- [x] 执行 `SampleScene` 实际场景加载烟测：Editor 验证未完成；未修改 SampleScene，已通过场景流程纯 C# 行为验证
- [x] 同步任务状态、验证记录和 Issue #4 交付信息

### Issue #4 多关卡范围决策

- [x] 记录 Demo 内部多关卡默认由具体 Demo 状态机管理
- [x] 暂不创建通用 `IDemoLevelFlow`、`LoadDemoLevel` 或独立关卡场景适配器
- [x] 将独立关卡场景、关卡稳定 ID 和关卡存档字段延后到有真实需求的 Demo Issue
- [x] 保持 Issue #4 只验证顶层场景流程，不创建未来关卡占位资源
- [x] 在交付记录中说明复杂度收敛决策和后续扩展触发条件
## Issue #5：架构骨架集成与 SampleScene 烟测
- [x] 追加 Issue #5 的 proposal、spec、plan、design 和 tasks 范围
- [x] 执行 `openspec validate architecture-foundation` 并修复校验问题：返回 `Change 'architecture-foundation' is valid`
- [x] 添加无 Unity 依赖的 `SessionInitializedEvent`
- [x] 实现 `SessionSmokeEntry` 初始化、事件、服务调用、失败观察和解除订阅
- [x] 实现 Editor 幂等场景装配工具并生成所需 `.meta`
- [x] 更新 PlayMode asmdef，添加 SampleScene PlayMode 集成测试
- [x] 添加 EventBus 初始化事件订阅、发布和解除订阅 EditMode 测试
- [x] 执行 C# 程序集编译：`dotnet build Spotlight.Composition.csproj --no-restore` 成功；`dotnet build Spotlight.Tests.EditMode.csproj --no-restore` 成功，0 警告、0 错误
- [x] 执行 EditMode 批处理测试：`editmode-results.xml` 返回 Passed，17 个测试全部通过
- [x] 执行 PlayMode 批处理测试：`playmode-results.xml` 返回 Passed，1 个 `SessionSmokeTests` 全部通过；日志收到 `ArchitectureSmoke 收到 SessionInitializedEvent。`
- [x] 使用 Unity Editor 执行 `SessionSmokeSceneSetup.Apply` 并保存 SampleScene；场景包含 `ArchitectureSmoke`，随后 PlayMode SampleScene 烟测通过
- [x] 记录 Bootstrap、Menu、Hub、Demo、正式资源及未完成 Editor 验证限制：正式 UI、Input Actions、AudioMixer、文件存档和剧情资产仍未覆盖
***
