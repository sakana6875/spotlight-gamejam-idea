# Demo1 集成实施计划

## 实施顺序

### 1. 建立合并基线

- 在独立 worktree 中从 PR #18 head 工作，不覆盖 `feature/demo2` 的未提交改动。
- 合并 `origin/develop` 并记录全部冲突。
- 保留 `develop` 的 `Spotlight.Game` 与测试程序集身份，删除旧 Features、Demo1 和 Contracts 程序集。

### 2. 恢复程序集与代码依赖

- 将 Demo1 运行时代码编入 `Spotlight.Game`，为 Input System 和 TMP 增加明确程序集引用。
- 让 Demo1 Editor 程序集只引用 `Spotlight.Game`。
- 将输入、音乐和事件契约收敛到 Demo1 目录，迁移所有旧命名空间调用方。
- 将 EditMode 测试迁移到 `Spotlight.Tests.EditMode` 当前引用模型。

### 3. 接入组合根与场景流程

- 为现有 `SceneLoader` 增加仅包含真实 `sample`、`demo1` 的稳定场景映射和明确结果。
- 由 `SessionRoot` 创建并持有 Save、Scene 和音频服务，并向 Demo1 场景入口注入。
- 删除无真实行为的 checkpoint 控制器；接通结算返回 `SampleScene` 的实际调用。
- 确保 Demo1 入口、重试、完成和主动返回保持不同状态，不引入坏结局映射。

### 4. 接入输入与音频

- 使用统一 Input Actions 资产定义 `Pulse`，绑定 Space 与鼠标左键。
- 删除 `Demo1InpitActions.inputactions` 及损坏绑定。
- 使用实际 DSP 调度结果创建歌曲时钟；缺少 BGM 时阻止单局启动并报告原因。
- 删除未实现的 `PlaySfx` 空方法或为其提供真实资源，不保留 no-op。

### 5. 迁移内容与 Unity 资源

- 使用 Unity Editor 更新 Demo1 场景、Prefab、InputActionReference、Build Settings 和服务引用。
- 保留 JSON 源文件、生成资产及 `.meta`；验证导入失败不覆盖有效资产。
- 删除 TMP Documentation/Examples、SceneTemplateSettings、无关 `.vsconfig` 和非行为性 Renderer2D 序列化变化。
- PlayerSettings 只保留 Input System 所需改动，不提交默认公司标识变化。

### 6. 验证

- 执行 OpenSpec 校验和静态旧命名空间/程序集检查。
- 使用 Unity 2022.3.62f3c1 重新导入并编译目标程序集。
- 运行现有及 Demo1 EditMode 行为测试。
- 在 Editor/PlayMode 验证 Demo1 进入、两种 Pulse 输入、BGM 时钟、判定结算和返回 SampleScene。
- 检查场景/Prefab 无 Missing Script、资源 GUID 完整，并回写 `tasks.md` 验证记录。

## 目录与程序集

```text
Assets/Scripts/Spotlight.Game.asmdef
Assets/Scripts/Domain/Spotlight.Domain.asmdef
Assets/Scripts/Bootstrap/
Assets/Scripts/Scene/
Assets/Scripts/Save/
Assets/Scripts/Audio/
Assets/Scripts/Demos/Demo1/
Assets/Tests/EditMode/
Assets/Tests/PlayMode/
```

生产代码不新增 Demo1 独立程序集。只有 Demo1 Editor 导入代码保留 Editor-only 程序集并引用 `Spotlight.Game`。

## 依赖方向

```text
Bootstrap → Save / Scene / Audio / Demo1 场景入口
Demo1     → Domain / 注入的 Save、Scene、Audio 能力 / Unity 组件
Save      → Domain
Scene     → Unity SceneManagement
Domain    → 无 Unity 依赖
```

## 验证路径

- `openspec validate demo1-develop-integration --strict`
- Unity EditMode：现有架构测试及五组 Demo1 测试
- Unity PlayMode/Editor：Demo1 场景真实运行路径
- 静态检查：旧架构命名空间、独立程序集、无关资源和缺失 `.meta`
