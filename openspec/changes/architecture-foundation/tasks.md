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
