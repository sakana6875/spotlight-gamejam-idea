## Why

PR #18 的 `demo-1` 分支基于已被 `develop` 收敛的旧分层架构，导致程序集重命名冲突，并继续引用已经删除的 `Application`、`Contracts`、`ISceneFlow` 和 `ISaveService`。如果只接受任一侧冲突，Demo1 会丢失代码或产生无法编译的程序集依赖，因此需要按当前简化架构完成一次完整集成。

## What Changes

- **BREAKING**：删除 `Spotlight.Demos.Demo1` 和 `Spotlight.Contracts` 生产程序集，Demo1 运行时代码并入 `Spotlight.Game`。
- 将 Demo1 的输入、音乐和局部事件边界收敛为 Feature 内部类型，不恢复已删除的全局 `Architecture/Contracts` 与 `Application` 层。
- 将 Demo1 场景入口迁移到当前 `SceneLoader`、音频服务和 `SessionRoot` 组装路径；没有真实 checkpoint 行为时不注入 `SaveService`，结算返回目标明确为现有 `SampleScene`。
- 把 `Pulse` 接入统一 Input System 动作资产，默认绑定 Space 和鼠标左键，删除拼写错误且绑定损坏的 Demo1 私有动作资产。
- 保留 Demo1 实际使用的场景、音频、Prefab、配置和内容资产，删除 TMP 示例、文档及无关 Unity 生成设置。
- 保留并迁移五组 Demo1 EditMode 行为测试，补齐程序集、内容导入和场景运行验证。

## Capabilities

### New Capabilities

- `demo1-integration`：定义 Demo1 在当前程序集、输入、音频、场景返回和内容导入边界下的可观察行为。

### Modified Capabilities

- 无。

## Impact

- 代码：`Assets/Scripts/Demos/Demo1`、`Assets/Scripts/Bootstrap`、`Assets/Scripts/Scene`、`Assets/Scripts/Spotlight.Game.asmdef`。
- 测试：`Assets/Tests/EditMode`，必要时增加最小 PlayMode 场景验证。
- 资源：Demo1 场景、Input Actions、音频、Prefab、JSON/ScriptableObject 内容以及对应 `.meta`。
- 配置：Input System 包引用、Player Input Backend、Build Settings。
- 风险：Unity YAML/GUID 迁移、旧命名空间遗漏、输入动作引用丢失、场景未进入 Build Settings、音频或 TMP 资源清理过度。
