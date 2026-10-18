# Demo1 集成规格

## 1. 程序集和依赖

Demo1 运行时代码 MUST 编译进 `Spotlight.Game`，编辑器导入代码 MUST 使用 Editor-only 程序集并只引用 `Spotlight.Game`。最终生产代码 MUST NOT 依赖旧 `Spotlight.Application`、`Spotlight.Adapters`、`Spotlight.Composition`、`Spotlight.Contracts` 或独立 `Spotlight.Demos.Demo1` 程序集。

## 2. 场景流程

稳定场景 ID `demo1` MUST 映射到实际 Demo1 场景并进入 Build Settings。所有 Unity 场景加载 MUST 经过 Scene 目录的统一边界。结算返回 MUST 请求加载 `SampleScene`；未知场景 ID MUST 返回明确失败且不回退。

## 3. 输入

统一 Input System 动作 `Pulse` MUST 默认绑定 Space 和鼠标左键。Demo1 MUST 通过动作引用读取 performed 状态，一次输入在当前帧最多产生一次 Pulse。旧 Input API、拼写错误的私有动作资产和损坏绑定 MUST 被移除。

## 4. 音频与歌曲时间

Demo1 BGM MUST 通过组合根持有的音频边界按 DSP 时间调度，歌曲时钟 MUST 使用实际调度起点。缺少 AudioClip 或调度失败 MUST 阻止单局启动并报告原因；不得返回假成功或保留空音效实现。

## 5. 玩法和结算

现有谱面顺序、Perfect/Good/Miss 窗口、得分、最终高度和结算事实 MUST 保持。一次 Pulse 最多消费一个音符；一次 Tick MAY 处理多个已过期音符；普通 Miss MUST NOT 自动触发坏结局。

## 6. 内容导入

Demo1 JSON 导入 MUST 校验必填字段、稳定 ID、重复项、数值边界和引用。失败 MUST 报告源文件与字段，并且 MUST NOT 覆盖已有有效运行时资产。运行时只依赖生成资产。

## 7. 资源完整性

场景、Prefab、音频、配置、JSON、生成资产和 `.meta` MUST 完整提交。TMP 示例、文档、IDE 配置、场景模板和无关 Renderer2D/PlayerSettings 变化 MUST NOT 进入最终变更集。

## 8. 验收

- Unity 2022.3.62f3c1 导入后无程序集或脚本编译错误。
- 现有测试及五组 Demo1 EditMode 测试通过。
- Demo1 可进入，Space/鼠标左键可触发 Pulse，BGM 时钟、判定和结算可观察。
- 返回操作实际加载 `SampleScene`。
- 场景和 Prefab 无 Missing Script/Missing Reference。
- 未执行的 Editor 或 PlayMode 验证必须明确标记“Editor 验证未完成”。
