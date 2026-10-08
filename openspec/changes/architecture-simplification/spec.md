# 架构简化规格

## 1. 组合根和初始化

### 要求：唯一组合根

`SessionRoot` MUST 继续是当前运行周期创建和组装跨场景服务的唯一组合根。其他生产模块 MUST NOT 使用静态业务单例、Service Locator、`FindObjectOfType` 或 `FindFirstObjectByType` 查找、创建或替换全局服务。

### 要求：固定组装链收敛

当前只有一个默认组装方案时，生产运行时 MUST NOT 为可替换性保留无替换实现的 `ISessionServiceFactory`、`DefaultSessionServiceFactory`、`SessionBootstrapper` 和完整 `SessionServices` 多层转发链。固定服务可以在 `SessionRoot` 内的明确方法中创建和保存。

### 要求：初始化行为保持

收敛后 MUST 保持：

- 首次初始化只创建一组服务；
- 重复初始化不会创建第二组服务；
- 初始化失败包含可定位原因并可观察；
- 成功初始化后的 `SessionRoot` 按当前约束跨场景存活；
- 临时烟测入口不得成为跨场景业务根或正式游戏流程。

## 2. 服务和依赖

### 要求：只保留真实边界

默认运行时 MUST NOT 创建没有真实生产调用方的 EventBus、对话、输入、音频和 Demo 记录型服务。出现真实 Feature 或外部资源调用方后，才按最小依赖建立对应服务或接口。

如果仍有组合对象，它 MUST 只在组合根/场景接线边界使用，并且只包含当前入口实际需要的依赖；不得扩展为万能上下文。

### 要求：Feature 最小依赖

Feature MUST 通过初始化参数、明确引用或场景入口接线接收实际需要的服务。Feature MUST NOT 自行创建全局服务、访问静态服务注册表或依赖其他 Feature 的具体 MonoBehaviour。

## 3. 场景加载

### 要求：单一 Unity 场景边界

`SceneManager` 和 `SceneManager.LoadSceneAsync` MUST 只出现在唯一的 Unity 场景加载适配器或工具中。Feature、Domain 和 Application MUST NOT 直接引用 `SceneManager`。

### 要求：只表达当前场景能力

场景 API MUST 只保留当前存在调用方和可验证语义的操作。未创建、未登记或没有调用方的 Menu、Hub、Demo 入口和重启操作 MUST NOT 作为默认生产流程的假能力。

场景加载结果 MUST 明确区分成功和已知失败；成功语义必须记录为“请求发起”或“加载完成”中的一种，不得让记录替身和 Unity 适配器使用不同语义。

## 4. Demo、对话和剧情

### 要求：不提前建立通用 Demo 编排

在没有真实 Demo Feature caller 时，`IDemoFlow`、`DemoRunResult`、`RecordingDemoFlow` 和同类通用 Demo 编排契约 MUST NOT 作为默认运行时依赖。首个 Demo 的进入、重试、退出、成功和失败由其自身控制器负责。

普通 Demo 失败 MUST NOT 自动产生或映射为坏结局。

### 要求：对话按真实内容建立

在没有真实对话 UI、内容资产或 Feature caller 时，通用对话生命周期服务、不可用生产占位服务和记录型对话适配器 MUST NOT 被默认运行时创建。稳定 `ContentId` 若已被真实内容契约使用，可以保留；完整剧情、结局和事件模型必须延后到有实际消费者时建立。

## 5. 存档和永久进度

### 要求：保留行为边界

架构收敛 MUST 保留以下可观察行为：

- 同一 `checkpointId` 的后写入快照覆盖旧快照；
- 临时检查点状态与永久进度分离；
- 读取旧快照不得撤销已解锁或已完成的永久进度；
- 空存档、未知检查点和写入失败返回明确结果；
- 内存实现不得声称完成文件持久化。

### 要求：延后未使用契约

没有真实调用方时，`ListSnapshots`、设置存档、复杂 Reset 流程、额外音量字段和版本迁移模型 SHOULD NOT 继续扩展。删除这些契约前 MUST 迁移或删除其生产调用方和对应行为测试，并确认没有已确认的存档兼容要求。

## 6. 测试、程序集和文档

### 要求：最小生产程序集

生产代码 MUST 收敛为以下程序集：

- `Spotlight.Domain`：纯 C# 领域规则和数据，不引用 Unity；
- `Spotlight.Game`：`SessionRoot`、真实服务、场景边界、Unity 适配、Feature、UI 和 Data；
- `Spotlight.Tests.EditMode`；
- `Spotlight.Tests.PlayMode`。

当前 MUST NOT 为 `Application`、`Adapters` 或没有真实 Feature 的 `Features.*` 单独保留生产程序集。目录可以继续表达职责，但不自动产生编译边界。只有出现明确的独立编译隔离需求或真实模块边界时，才新增 Feature 程序集。

生产代码物理目录 MUST 收敛为当前真实职责目录：`Bootstrap`、`Save`、`Scene`、`Domain` 和未来真实 Feature 目录。默认运行时 MUST NOT 保留无真实调用方的 `Architecture/Application`、`Architecture/Adapters`、`Architecture/Composition`、`Architecture/Contracts` 或空 `Features` 目录。

### 要求：生产程序集依赖

`Spotlight.Domain` MUST 不引用 Unity。`Spotlight.Game` MAY 引用 `Spotlight.Domain` 和 Unity，但不得让 Feature 依赖其他 Feature 的具体实现。测试程序集 MUST 只引用被测试生产程序集和 Unity Test Framework 所需程序集，生产程序集 MUST NOT 引用测试程序集。

### 要求：测试消费者行为

测试 MUST 优先验证调用方可观察的状态、结果和边界。测试 MUST NOT 以 `LastRequest`、`LastOperation` 等记录型替身内部字段作为主要产品验收依据；删除替身时应删除或改写对应测试。

### 要求：OpenSpec 状态唯一

本 Change 完成后 MUST 选择一个架构简化 Change 作为当前规范来源；旧 Change 的重复内容 MUST 归档、标记为历史或改为明确 delta，不得继续把已删除的未来能力当作当前交付要求。

## 7. 验收

完成本 Change 至少需要证明：

1. 默认组装不再通过无替换实现的多层工厂表达可替换性。
2. `SessionRoot` 仍是唯一组合根，且初始化幂等、失败可观察、生命周期行为未回归。
3. 默认运行时没有无真实消费者的通用 EventBus、对话、输入、音频和 Demo 替身。
4. Feature 不直接依赖 `SceneManager`，且场景加载成功语义一致。
5. 存档覆盖、永久进度保留和明确失败边界仍有行为测试。
6. 删除的类型没有生产调用方、程序集引用或未处理的测试依赖。
7. 未实际执行的 Unity Editor、场景、输入、音频和 PlayMode 验证明确标记为“Editor 验证未完成”。