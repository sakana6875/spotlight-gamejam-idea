## Context

参见 `proposal.md`。`develop` 已把生产程序集收敛为 `Spotlight.Domain` 和 `Spotlight.Game`，而 PR #18 仍从旧 `Spotlight.Features` 分支演进出独立 Demo1/Contracts 程序集，并引用已删除的全局服务接口。Demo1 同时带入真实场景、输入、音频和内容导入，因此本次不能只解决文本冲突。

## Goals / Non-Goals

**Goals:**

- 保留 `develop` 的程序集 GUID 和简化架构，迁移全部 Demo1 调用方。
- 让 Demo1 通过组合根提供的真实服务完成输入、音乐时钟、场景返回和内容导入。
- 保持现有纯规则与行为测试，不以放宽断言掩盖迁移问题。
- 将 PR 变更集收敛为 Demo1 真正使用的代码、资源和配置。

**Non-Goals:**

- 不创建正式 Hub、菜单、剧情或新的失败结局。
- 不增加武器、技能、敌人或额外 Demo1 玩法。
- 不实现文件持久化或新的存档格式。
- 不升级 Unity Editor、URP、TMP 或无关包。

## Decisions

### 1. Demo1 并入 Spotlight.Game

保留 `develop` 的 `Assets/Scripts/Spotlight.Game.asmdef` 及其 `.meta` GUID，并增加 Demo1 实际需要的 `Unity.InputSystem` 与 `Unity.TextMeshPro` 引用。删除旧 Features、独立 Demo1 和全局 Contracts 程序集。

未选择保留独立 Demo1 程序集，因为当前没有独立编译、复用或发布边界；它只会迫使 Bootstrap/Save/Scene 形成额外引用关系。

### 2. 只保留 Feature 内真实端口

输入读取和音乐调度是 Unity/Input System/AudioSource 外部边界，可以保留最小 Demo1 局部契约；契约与实现放在 Demo1 目录并编入 `Spotlight.Game`。局部事件总线只发布已发生的判定与完成事实，使用 Demo1 专用类型，不恢复全局 Application EventBus。

所有契约只包含真实调用方法。当前没有音效资源时不保留空 `PlaySfx`。

### 3. 场景边界恢复为真实最小目录

现在已有真实 Demo1 场景调用方，场景加载边界增加稳定 ID 到场景名的最小映射，仅登记 `sample` 和 `demo1`。加载返回明确结果，未知 ID 不回退。`SceneManager` 仍只存在于 Scene 目录。

Demo1 结算返回 `SampleScene`，不创建尚不存在的 Hub。

### 4. SessionRoot 继续负责跨场景服务和注入

`SessionRoot` 创建并持有 Save、Scene 和音频服务。组合根在场景加载后向明确的 Demo1 场景入口注入依赖；Demo1 不使用静态单例、Service Locator 或自行查找全局服务。没有真实 checkpoint 行为的控制器删除，不保留只存字段的空壳。

### 5. 统一 Pulse 动作资产

将损坏的 `Demo1InpitActions.inputactions` 替换为项目级 `Assets/Input_Actions.inputactions`，动作名固定为 `Pulse`，默认绑定 `<Keyboard>/space` 与 `<Mouse>/leftButton`。场景通过 `InputActionReference` 读取，不硬编码设备轮询。

### 6. 音乐使用组合根音频边界

组合根持有实际音频播放实现和 AudioSource 生命周期；Demo1 只提交 BGM 配置并请求 DSP 调度。歌曲时钟只接受真实调度结果。缺少 AudioClip 时报告错误并停止单局初始化。

### 7. Unity 资源通过 Editor 修改

场景、Input Actions、Build Settings 和序列化引用优先通过 Unity Editor 或可重复 Editor 工具保存。文本工具只处理 C#、asmdef、OpenSpec 和简单配置；不手工拼接复杂场景 YAML。

## Risks / Trade-offs

- [程序集 GUID 选择错误导致 Missing Script] → 固定保留 `develop` 的 Spotlight.Game `.meta`，迁移后用 Unity 重新导入检查。
- [删除 TMP 资源导致字体引用丢失] → 先检查场景 GUID 引用，只删除 Documentation/Examples；Essential Resources 按真实引用保留。
- [场景入口没有被组合根注入] → 对进入、结算返回和缺失入口分别做 PlayMode/Editor 烟测。
- [Input System 切换要求 Unity 重启] → 包、lockfile 与 Player Input Backend 作为一个变更验证。
- [PR 与正在开发的 Demo2 共享输入/场景文件] → 只加入稳定 `Pulse`、`sample`、`demo1`，不携带 Demo2 内容；后续合并按稳定 ID 合并。

## Migration Plan

1. 在独立分支合并 `origin/develop`，保留当前工作区不变。
2. 解决 asmdef 重命名和测试程序集冲突，先恢复可编译边界。
3. 迁移 Demo1 局部契约、旧服务引用及组合根注入。
4. 通过 Unity Editor 更新输入、场景、Build Settings 和序列化引用。
5. 清理无关生成资源，执行静态检查、EditMode 和实际场景烟测。
6. 若迁移失败，回退本解决分支；不改写原 `demo-1` 历史或当前 Demo2 工作区。
