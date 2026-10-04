# 架构基础执行任务

- [x] 读取并记录 Unity 版本、Packages、现有场景、URP 2D Renderer 和当前根目录状态
- [x] 创建 `Assets/Scripts/Architecture`、`Assets/Scripts/Features` 和 `Assets/Tests` 的嵌套目录
- [x] 创建 `Spotlight.Domain` asmdef 并确认不引用 Unity 场景相关程序集
- [x] 创建 `Spotlight.Application` asmdef 并仅引用 `Spotlight.Domain`
- [x] 创建 `Spotlight.Adapters` asmdef 并引用 `Spotlight.Application` 与 `Spotlight.Domain`
- [x] 建立 Feature 程序集落点并确认不引用其他 Feature 的具体实现
- [x] 创建 `Spotlight.Tests.EditMode` 与 `Spotlight.Tests.PlayMode` asmdef
- [x] 为所有 asmdef 生成并保留对应 `.meta` 文件
- [x] 检查项目根目录不存在 `AssetsScripts*`、`AssetsData*` 和 `AssetsTests*` 错误目录
- [x] 执行 EditMode 测试程序集验证（已执行 Unity Test Runner 批处理命令并正常退出；当前没有生产脚本或行为测试，因此未生成测试结果 XML）
- [x] 记录 Unity Editor 验证结果（已完成批处理导入与编译验证；Editor 验证通过，空程序集仅产生“没有脚本可编译”警告）
- [x] 检查 SampleScene、URP 资源、Build Settings 和 Packages 未被修改
