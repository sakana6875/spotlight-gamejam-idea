## Purpose

定义 Demo1 在当前简化架构中的可运行集成边界，使场景进入、节拍输入、音乐时钟、判定结算、内容导入和返回流程能够被一致验证，且不恢复已经删除的旧架构层级。

## ADDED Requirements

### Requirement: Demo1 必须作为当前游戏程序集的一部分可用

Demo1 运行时代码 MUST 编译进当前游戏程序集，并且 MUST NOT 依赖已删除的旧 Application、Adapters、Composition、Contracts 或独立 Demo1 生产程序集。编辑器导入代码 MUST 与运行时代码隔离，并只引用当前游戏程序集。

#### Scenario: Unity 导入 Demo1

- **WHEN** Unity 重新导入合并后的项目
- **THEN** Demo1 运行时代码和编辑器导入代码完成编译，且不存在缺失程序集引用

### Requirement: Demo1 场景必须通过统一场景边界进入和退出

运行时 MUST 使用稳定场景 ID `demo1` 加载 Demo1，并且 MUST 只通过项目的统一场景加载边界调用 Unity 场景 API。Demo1 结算后的返回操作 MUST 请求加载现有 `SampleScene`，不得只记录日志或静默成功。

#### Scenario: 进入 Demo1

- **WHEN** 调用方请求稳定场景 ID `demo1`
- **THEN** 场景加载边界请求加载已登记且位于 Build Settings 的 Demo1 场景

#### Scenario: 从结算界面返回

- **WHEN** 玩家在 Demo1 结算界面触发返回
- **THEN** 场景加载边界请求加载 `SampleScene`，并返回可观察的请求结果

#### Scenario: 请求未知场景

- **WHEN** 调用方提交未登记的稳定场景 ID
- **THEN** 场景加载边界返回明确失败结果且不加载其他场景

### Requirement: Pulse 必须来自统一输入动作

Demo1 MUST 从统一 Input System 动作 `Pulse` 读取节拍输入。`Pulse` MUST 默认绑定 Space 和鼠标左键；一次 performed 输入 MUST 在当前帧最多产生一次 Pulse，不得依赖旧 Input API 或损坏的本地动作资产。

#### Scenario: 键盘触发 Pulse

- **WHEN** 玩家按下 Space
- **THEN** 当前帧向 Demo1 单局控制器提供一次 Pulse

#### Scenario: 鼠标触发 Pulse

- **WHEN** 玩家点击鼠标左键
- **THEN** 当前帧向 Demo1 单局控制器提供一次 Pulse

### Requirement: Demo1 音乐时钟必须使用实际 DSP 起点

Demo1 MUST 通过统一音频边界调度 BGM，并以音频系统返回的实际 DSP 开始时间作为歌曲时钟基准。音频资源缺失或调度失败 MUST 可观察，不得返回假成功。

#### Scenario: 正常调度 BGM

- **WHEN** Demo1 具有有效 BGM 并以未来 DSP 时间请求播放
- **THEN** 音频边界调度播放并返回实际 DSP 起点，歌曲时钟以该值推进

#### Scenario: BGM 缺失

- **WHEN** Demo1 未配置 BGM
- **THEN** 启动失败原因被明确报告，单局不得以伪造时间继续运行

### Requirement: Demo1 必须保持现有节拍判定与结算行为

Demo1 MUST 保持现有谱面顺序、Perfect/Good/Miss 窗口、一次输入最多消费一个音符、连续过期音符处理、得分累计和最终高度结算行为。普通 Miss MUST NOT 自动映射为坏结局。

#### Scenario: 一次输入只消费一个音符

- **WHEN** 同一帧有多个可判定音符且只发生一次 Pulse
- **THEN** 单局控制器最多推进一个音符

#### Scenario: 长时间未输入

- **WHEN** 歌曲时间越过连续多个音符的 Miss 边界
- **THEN** 所有已过期音符依次记为 Miss，状态推进到第一个未过期音符

#### Scenario: 全部音符处理完成

- **WHEN** 全部音符已处理且歌曲时间越过收尾缓冲
- **THEN** 单局进入完成状态并发布一次结算事实

### Requirement: 内容导入失败不得覆盖有效资产

Demo1 外部 JSON 内容 MUST 校验必填字段、稳定 ID、重复项、数值边界和标记引用。校验失败 MUST 报告源文件与字段，并且 MUST NOT 创建或覆盖对应的有效运行时资产。

#### Scenario: 导入合法谱面

- **WHEN** JSON 包含完整且合法的 Demo1 谱面数据
- **THEN** 导入器生成或更新对应运行时资产

#### Scenario: 导入非法谱面

- **WHEN** JSON 缺少必填字段或包含非法数据
- **THEN** 导入失败原因包含源文件与字段，已有运行时资产保持不变

### Requirement: 集成资源必须保持最小且完整

Demo1 MUST 提交实际引用的场景、Prefab、音频、配置、生成资产和对应 `.meta`。Demo1 MUST NOT 提交未被玩法引用的 TMP 示例、文档、场景模板或 IDE 配置。

#### Scenario: 检查合并内容

- **WHEN** 审查 Demo1 的最终变更集
- **THEN** 所有运行时引用可解析，且不存在 TMP 示例、文档和无关生成配置
