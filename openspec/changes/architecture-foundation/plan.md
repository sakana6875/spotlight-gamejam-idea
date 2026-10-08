# 架构基础实施计划

## 实施顺序

### 1. 确认现状

- 检查 Unity 版本、现有 Packages、SampleScene、URP 2D Renderer 和项目根目录。
- 确认不修改已有未提交资源和 Unity 自动生成目录。
- 确认目标目录不存在错误的 `AssetsScripts*` 拼接目录。

### 2. 建立目录

创建以下嵌套目录：

```text
Assets/
├── Scripts/
│   ├── Architecture/
│   │   ├── Contracts/
│   │   ├── Domain/
│   │   ├── Application/
│   │   ├── Adapters/
│   │   └── Composition/
│   └── Features/
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

目录只作为程序集边界和后续代码落点，不提前创建未确认的业务子目录。

### 3. 建立核心 asmdef

按依赖顺序创建：

1. `Spotlight.Domain`
2. `Spotlight.Application`
3. `Spotlight.Adapters`
4. Feature 基础程序集模板或首个实际 Feature 程序集（仅在有真实代码归属时创建）
5. `Spotlight.Tests.EditMode`
6. `Spotlight.Tests.PlayMode`

asmdef 的引用保持最小化：

```text
Spotlight.Domain       → 无项目程序集引用
Spotlight.Application  → Spotlight.Domain
Spotlight.Adapters     → Spotlight.Application, Spotlight.Domain
Spotlight.Features.*   → 公开契约所在程序集, Spotlight.Domain, Spotlight.Application
Spotlight.Tests.*      → 被测试程序集, Unity Test Framework
```

不为 `Contracts` 目录单独创建程序集。若后续契约需要跨层使用，先根据实际依赖决定归属，不能提前添加万能 Contracts 程序集。

### 4. 添加最小可编译入口

只添加能够说明程序集职责、不会伪装业务完成度的最小类型。入口类型不得包含空业务方法、假返回值、静默 catch 或未确认的 API。

如果 Unity Editor 能够保留空目录对应的 asmdef，则不添加无必要的代码；否则使用纯 C# 的职责标记或程序集说明类型，并提供中文 XML 文档。

### 5. 添加测试程序集边界

为 EditMode 和 PlayMode 测试创建 asmdef，引用 Unity Test Framework 和对应生产程序集。当前不为了填充测试程序集创建没有行为断言的测试；后续规则实现时再添加行为测试。

### 6. 生成和检查 .meta

通过 Unity Editor 或可重复的 Unity 资源生成方式生成并保留 `.asmdef.meta` 和目录需要的 `.meta` 文件。不得手工修改复杂 Unity YAML。

## 依赖与保护边界

- 只有后续 SessionRoot 所在的 Composition/Adapters 层可以组装具体实现。
- Domain/Application 不得查找、创建或缓存 Unity 场景对象。
- 不创建 GameManager、静态单例或 Service Locator。
- 不将 SampleScene 改为正式 Bootstrap。
- 不把后续 Issue 的接口提前塞入本次空壳程序集。

## 验证路径

### 静态验证

- 检查目录路径是否为 `Assets/Scripts/...` 和 `Assets/Tests/...`。
- 检查 asmdef 名称、GUID、引用和 `autoReferenced` 配置。
- 检查项目根目录没有本次错误拼接目录。
- 检查本次变更未修改 SampleScene、URP 资源和 Packages。

### Unity 验证

- 打开 Unity 2022.3.62f3c1 项目并等待 Asset Database 完成导入。
- 确认 asmdef 被识别且无程序集循环引用。
- 执行 EditMode 编译/测试入口，确认 Domain/Application 可编译。
- 若当前 Agent 无法实际运行 Unity Editor，必须在任务和交付结果中标记“Editor 验证未完成”，不得声称验证通过。
## Issue 2：存档、永久进度与设置契约

1. 在 `Spotlight.Domain` 内实现纯数据模型和 `ISaveService`、`IProgressService`，先固定稳定 ID、结果类型和永久/临时状态分离规则。
2. 在 `Spotlight.Adapters` 内实现内存服务；使用 `Dictionary<string, SaveSnapshot>` 实现唯一检查点覆盖，不引入文件 IO、Unity 序列化或场景依赖。
3. 在 `Spotlight.Tests.EditMode` 内添加行为测试，覆盖空存档、覆盖、永久进度保留、未知 ID、重置和设置数据。
4. 执行 JSON/依赖静态检查、Unity 导入编译和 EditMode Test Runner；失败后继续定位修复，不放宽断言。

Issue 2 的实现不创建正式场景，不修改 Build Settings、SampleScene、URP 资源或 Packages。

## Issue 3：SessionRoot 与全局服务初始化骨架

### 实施步骤

1. 读取当前 asmdef、Issue #2 的存档接口和 `SampleScene`/Build Settings 状态，确认不修改现有场景资源。
2. 在 `Spotlight.Domain` 或 `Spotlight.Application` 的已有程序集边界内放置最小服务契约；复用 `ISaveService` 和 `IProgressService`，按职责归属其余接口，不创建独立 Contracts 程序集。
3. 在 `Spotlight.Adapters` 或 `Spotlight.Application` 内提供可验证的最小服务实现。实现必须返回明确结果或状态，不得使用空方法、假成功或静默降级。
4. 在 `Assets/Scripts/Architecture/Composition/SessionRoot.cs` 创建唯一组合根。它负责创建服务、组装依赖、执行一次性初始化和对外提供接口集合，不承载具体业务规则。
5. 为 `SessionRoot` 定义幂等初始化和失败报告行为；使用 `DontDestroyOnLoad` 保持跨场景生命周期，但不修改 `SampleScene` 或创建正式 Bootstrap 场景。
6. 为可注入启动入口提供显式依赖对象或初始化参数，禁止服务使用方通过静态字段和 Unity 全局查找获取服务。
7. 添加 EditMode 行为测试，覆盖首次初始化、重复初始化、必需服务失败和接口依赖可见性；不通过私有字段或调用次数断言实现细节。
8. 运行 OpenSpec 验证、静态依赖检查、Unity 导入编译和相关 EditMode 测试；场景启动与跨场景生命周期若未在 Unity Editor/PlayMode 中实际执行，记录“Editor 验证未完成”。

### 目录与依赖

```text
Assets/Scripts/Architecture/
├── Domain/ 或 Application/       # 服务 Ports 与结果契约
├── Adapters/                     # 最小可验证服务实现
└── Composition/
    └── SessionRoot.cs            # 唯一组合根
```

依赖保持：

```text
SessionRoot/Adapters → Application/Domain → 纯数据与接口
```

`Domain` 和 `Application` 不得依赖 `SessionRoot`、具体 Adapter 或 Unity 场景对象；`SessionRoot` 可以依赖接口和具体实现，以履行组合根职责。

### Issue 3 验证路径

- 检查七类服务接口均有唯一归属且没有重复的存档/进度接口；
- 检查 `SessionRoot` 是唯一创建和组装入口，没有静态服务单例和 Service Locator；
- 检查重复初始化不会产生第二组服务，失败结果包含可定位原因；
- 执行 Unity 批处理导入/编译与 EditMode 测试；
- 仅在实际打开场景并执行生命周期流程后标记场景和 `DontDestroyOnLoad` 验证，否则明确记录“Editor 验证未完成”。

## Issue 4：场景流程契约与 Unity 场景适配器骨架

### 实施步骤

1. 读取当前 `ISceneFlow`、`InMemorySceneFlow`、Composition 组装关系、`SampleScene` 和 Build Settings 状态；确认不修改现有场景资源。
2. 将现有最小场景流程契约迁移到 `Application/Services/Scene/`，保持 `Spotlight.Application` 程序集归属；移除旧的重复定义，确保 SessionRoot 和现有适配器的调用方全部迁移。
3. 定义稳定 `SceneId`、`DemoId`、`DemoEntryMode`、`SceneCatalog`、`SceneLoadResult` 及必要的结果码。目录只描述当前允许的稳定 ID，不把未创建场景伪装为可用。
4. 扩展 `ISceneFlow`，实现 `LoadScene`、`LoadMenu`、`LoadHub`、`EnterDemo` 和 `RestartDemo`；记录型适配器用于 EditMode 验证参数与失败结果。
5. 在 `Adapters/Scene/UnitySceneFlowAdapter.cs` 实现唯一的 `SceneManager.LoadSceneAsync` 边界。适配器先查 `SceneCatalog`，只对已登记且可用的场景发起加载，并把无效输入转换为明确结果。
6. 保持 `SessionRoot` 通过接口组装场景服务，不让 Composition、Gameplay 或 UI 直接依赖 Unity SceneManager；不创建正式场景或修改 Build Settings。
7. 添加 EditMode 行为测试，覆盖 SampleScene 登记、未知场景、未创建场景、Demo 入口模式、重复/当前场景结果和调用参数；不测试私有字段或 Unity 序列化细节。
8. 执行 OpenSpec 校验、静态依赖检查、Unity 导入编译和相关 EditMode 测试；只有实际执行 SampleScene 加载烟测后才能标记 Editor 验证完成，否则明确记录“Editor 验证未完成”。

### 目录与依赖

```text
Assets/Scripts/Architecture/
├── Application/Services/Scene/   # 属于 Spotlight.Application 的场景 Ports 与纯数据
├── Adapters/Scene/               # UnitySceneFlowAdapter 与记录型验证适配器
└── Composition/                  # 只负责组装，不持有场景加载细节
```

依赖保持：

```text
UnitySceneFlowAdapter → Application/Domain
Composition           → Application/Adapters/Domain
Application/Domain    → 不依赖 Unity SceneManager
```

Issue #4 不创建独立 `Spotlight.Contracts` 程序集。原 Issue 描述中的 `Contracts/Scene/` 是职责概念，实际文件必须落在已有程序集目录，避免默认程序集和无意义程序集拆分。

### Issue 4 验证路径

- 检查稳定 ID、目录映射和结果码没有重复或隐式回退；
- 检查只有 Unity 适配器引用 `UnityEngine.SceneManagement`；
- 检查 `SessionRoot`、现有内存适配器和调用方完成接口迁移；
- 执行 Unity 批处理导入/编译与 EditMode 测试；
- 未实际加载 `SampleScene` 时记录“Editor 验证未完成”。

### 多关卡与复杂度边界

1. Demo 内部可以有多个关卡，但当前不创建通用 `IDemoLevelFlow`、`LoadDemoLevel` 或独立关卡场景系统。
2. 具体 Demo 优先使用自己的 Application/Domain 状态机管理内部关卡；只在真实需求确认需要独立 Unity 场景时，新增对应 Demo 的专用扩展。
3. 关卡恢复字段、独立关卡稳定 ID 和场景加载操作延后到产生真实检查点恢复需求的 Demo Issue，不提前修改通用存档契约。
4. Issue #4 只验证顶层场景流程，不为未来多关卡场景创建占位资源、通用适配器或预留接口。
5. 保留真实需要的 `SessionRoot`、存档/进度端口和 `ISceneFlow`；暂不扩展通用 EventBus、场景栈、Additive 场景编排或万能 Demo 管理器。
6. 所有新增抽象必须有已确认调用方和独立行为验收，不能仅因为未来可能复用而加入。
## Issue #5：架构骨架集成实施顺序

1. 追加并校验本 Change 的 Issue #5 提案、规格、设计和任务。
2. 增加纯 C# `SessionInitializedEvent`，保持初始化协调器不持有 EventBus。
3. 实现 `SessionSmokeEntry`：复用同一对象上的 `SessionRoot`，发布同步事件并调用所有可验证服务端口。
4. 实现 Editor 场景工具，通过 Unity API 幂等装配 `ArchitectureSmoke`，不手工修改 YAML。
5. 更新 PlayMode asmdef，添加 PlayMode SampleScene 测试和 EventBus 解除订阅 EditMode 测试。
6. 运行 OpenSpec 校验、Unity EditMode/PlayMode 批处理验证，并在 Unity Editor 中运行 SampleScene（若工具可用）。
7. 将实际命令、测试数量、场景运行结果和未覆盖范围写回本 Change。

实现不得创建正式场景、修改 Build Settings 或引入静态服务入口。
实现注意：`SessionSmokeEntry` 实际落在 Composition 程序集的 `Composition/Smoke/`，因为 Adapters 已被 Composition 依赖，将入口放入 Adapters 会违反程序集无环约束。
***
## Issue #7：对话生命周期与 Demo 流程实施顺序

1. 读取现有 `IDialogueService`、组合根默认工厂、不可用适配器和调用方，固定请求对象与结果码迁移边界。
2. 在 Domain Story 增加稳定 `ContentId`、`StoryFlag`、`EndingId` 和 `DialogueRequest`；在 Application Dialogue 增加 `DialogueResult`，再更新 `IDialogueService`。
3. 实现 `RecordingDialogueService`，只记录请求和生命周期状态，明确处理移动锁定、非法内容和未播放操作。
4. 定义 `IDemoFlow` 与 `DemoRunResult`，实现 `RecordingDemoFlow` 的进入、重试、退出和一次性结果注入。
5. 迁移组合根和现有适配器调用方，保持 `UnavailableDialogueService` 对合法内容的不可用结果。
6. 添加 EditMode 行为测试，覆盖稳定值、对话生命周期、移动锁定、Demo 入口/重试/退出及成功/失败/主动退出边界；不测试私有字段或调用次数。
7. 执行 OpenSpec 校验、目标程序集编译、Unity EditMode 测试和既有 PlayMode 烟测；将实际测试数量与限制写回本 Change。

Issue #7 不创建剧情资产、导入器、正式 UI、场景流程、存档字段、坏结局判定或通用 Demo 关卡系统。
***

