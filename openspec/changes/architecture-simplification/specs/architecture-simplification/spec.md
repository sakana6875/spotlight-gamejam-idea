# 架构简化 Delta 规格

## MODIFIED Requirements

### Requirement: 组合根只表达真实组装职责

`SessionRoot` MUST 继续作为唯一组合根，但在只有一个默认组装方案时，固定生产运行时 MUST NOT 依赖无替换实现的 Bootstrapper、Factory 和万能服务聚合链。组装应能从组合根的直接路径追踪；初始化幂等、失败可观察和跨场景生命周期 MUST 保持。

#### Scenario: 默认组装路径

- **WHEN** 游戏启动并执行默认组合
- **THEN** 服务由 `SessionRoot` 的直接组装路径创建或持有，不经过仅为未来替换保留的多层转发

### Requirement: 共享服务必须有真实消费者

默认运行时 MUST NOT 创建没有真实生产调用方的通用 EventBus、对话、输入、音频和 Demo 记录型服务。Feature MUST 只接收实际使用的最小依赖，不得使用 Service Locator 或静态注册表。

#### Scenario: 没有真实消费者的服务

- **WHEN** 一个服务只有烟测或记录替身消费者而没有真实 Feature/外部资源消费者
- **THEN** 该服务不进入默认生产组装，相关验证改为边界测试或延后到出现真实消费者

### Requirement: 场景边界保持但 API 收窄

`SceneManager` MUST 只存在于唯一 Unity 场景加载边界。默认生产 API MUST 只暴露当前有调用方且成功语义明确的场景操作；未创建场景和未来 Demo 操作不得被当作可用能力。

#### Scenario: 无调用方的未来场景

- **WHEN** 调用方请求尚未创建或没有生产消费者的场景操作
- **THEN** 操作不作为默认生产流程能力存在，或返回明确的不可用结果，不回退到其他场景

### Requirement: 普通 Feature 可以直接组合 Unity 组件

普通 Feature MAY 由职责明确的 MonoBehaviour 组合实现，不必经过通用 Application Service、Adapter 和 Port 转发。Feature MUST NOT 依赖其他 Feature 的具体 MonoBehaviour、直接调用 `SceneManager` 或自行创建全局服务。

#### Scenario: 首个真实 Feature

- **WHEN** 首个 Feature 实现自身控制器、玩家和目标组件
- **THEN** 它可以直接协调当前场景组件和最小注入依赖，同时保持跨模块和场景加载边界

## REMOVED Requirements

### Requirement: 默认组装七类全局服务

删除“默认组合根必须创建 IEventBus、ISceneFlow、ISaveService、IProgressService、IAudioService、IInputService 和 IDialogueService 七类服务”的预建要求。保留有真实调用方的存档、进度和场景边界；其他能力按真实 Feature 需求恢复。

### Requirement: EventBus 初始化烟测

删除 `SessionInitializedEvent` 自发发布/接收作为默认架构连通性验收的要求。不得把烟测对替身的调用当作正式输入、音频、对话或场景能力证明。

### Requirement: 通用 Demo 流程端口

删除没有真实 Demo caller 时必须提供 `IDemoFlow`、`DemoRunResult`、Enter/Retry/Exit 通用编排的要求。首个 Demo 的流程由具体 Feature 控制器定义。

## RETAINED Requirements

### Requirement: 存档永久进度边界

保留 checkpointId 后写覆盖、临时状态与永久进度分离、旧快照读取不撤销永久进度、失败可观察和内存实现不伪装文件持久化。

### Requirement: 禁止 Service Locator 和跨模块静态业务单例

保留组合根唯一创建全局服务、模块不得自行查找/创建/替换全局服务，以及 Feature 不依赖其他 Feature 具体实现的约束。
## MODIFIED Requirement: 生产程序集边界

生产代码 MUST 收敛为 `Spotlight.Domain` 和 `Spotlight.Game` 两个程序集；测试继续使用 `Spotlight.Tests.EditMode` 与 `Spotlight.Tests.PlayMode`。`Spotlight.Domain` MUST 不引用 Unity，`Spotlight.Game` 承载 SessionRoot、Services、场景边界、Unity 适配、Features、UI 和 Data。

当前 MUST NOT 为 `Application`、`Adapters` 或没有真实 Feature 的 `Features.*` 单独保留生产程序集。目录可以表达职责，但不自动产生独立编译边界。只有明确独立编译隔离需求出现后，才新增 Feature 程序集。