## Purpose

定义正式Build从唯一组合根初始化并通过稳定场景ID进入首个玩法场景的行为，避免复用会写测试数据的烟测入口或绕过SceneLoader直接加载场景。

## ADDED Requirements

### Requirement: 正式入口依赖同对象SessionRoot

正式Bootstrap入口 MUST 与 `SessionRoot` 位于同一GameObject，并在启动时确认组合根已成功初始化。缺少组合根或初始化失败时不得发起场景加载。

#### Scenario: 组合根初始化成功

- **WHEN** BootStrap场景启动且同对象SessionRoot成功初始化
- **THEN** 正式入口可以使用该SessionRoot持有的SceneLoader

#### Scenario: 组合根不可用

- **WHEN** 正式入口无法获得有效SessionRoot或初始化失败
- **THEN** 系统输出明确错误且不发起场景加载

### Requirement: 初始玩法场景通过稳定ID加载

正式入口 MUST 在Start阶段调用组合根持有的SceneLoader加载配置的稳定场景ID，不得直接调用SceneManager或硬编码Unity场景名。默认稳定ID MUST 为 `demo2`。

#### Scenario: 默认进入Demo2

- **WHEN** BootstrapEntry使用默认配置启动
- **THEN** SceneLoader收到 `demo2` 稳定ID并开始加载Build Settings中的 `demo2.unity`

### Requirement: 加载结果可观察

`Started`与`AlreadyActive` MUST 视为成功。空稳定ID、未知稳定ID和加载启动失败 MUST 输出包含ID和结果的明确错误，不得静默停留在Bootstrap场景。

#### Scenario: Unity接受加载请求

- **WHEN** SceneLoader返回Started
- **THEN** BootstrapEntry不输出失败错误，等待Unity完成场景切换

#### Scenario: 初始ID无效

- **WHEN** 初始场景ID为空或SceneLoader返回失败结果
- **THEN** BootstrapEntry输出明确错误并且不尝试其他场景作为兜底

### Requirement: 正式入口不执行烟测副作用

正式Bootstrap入口 MUST NOT 保存烟测检查点、解锁Demo或执行 `SessionSmokeEntry` 的验证逻辑。

#### Scenario: 正式Build启动

- **WHEN** BootStrap场景通过BootstrapEntry进入玩法场景
- **THEN** 入口只初始化组合根并发起场景加载，不写入任何烟测进度
