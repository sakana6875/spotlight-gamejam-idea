# 架构基础设计

## 设计目标

本变更只建立程序集和目录边界，为后续组合根、Ports、领域规则和 Unity 适配器提供稳定落点。它不实现任何完整业务流程。

## 目录设计

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

`Contracts` 是代码组织目录，不单独创建程序集。契约的程序集归属必须由实际依赖决定，避免建立没有明确职责的万能 Contracts 程序集。

## 程序集设计

```text
Spotlight.Domain       → 基础运行库
Spotlight.Application  → Spotlight.Domain
Spotlight.Adapters     → Spotlight.Application, Spotlight.Domain
Spotlight.Features.*   → 公开契约所在程序集, Spotlight.Domain, Spotlight.Application
Spotlight.Tests.EditMode → 被测试程序集, Unity Test Framework
Spotlight.Tests.PlayMode → 被测试程序集, Unity Test Framework
```

Domain 不引用 Unity 场景相关程序集。Application 不引用具体适配器。只有 Adapters 和后续 Composition 承担 Unity 生命周期与具体实现组装。

## 空目录处理

Unity 需要实际资源或脚本才能持续保留目录。asmdef 本身是实际资源，可以保留程序集边界。除非编译器或 Unity 导入流程明确需要，否则不添加只有空方法、假返回值或未确定业务 API 的占位类。

如果某个程序集必须有入口代码，入口只能是无业务副作用、明确表达程序集职责的真实类型，并使用中文 XML 文档解释其约束；不得借入口类型提前实现后续 Issue。

## 资源保护

本变更不触碰现有场景、URP 资产、Build Settings 和 Packages。SampleScene 继续作为当前项目已有场景，不在本变更中改名或升级为 Bootstrap。

## 验证策略

先通过静态检查确认路径、asmdef 名称和引用关系，再由 Unity 2022.3.62f3c1 导入并编译。若 Agent 无法运行 Unity Editor，必须保留静态验证结果，并在任务和交付中明确标记 Editor 验证未完成。
## 存档契约设计

Issue #2 的纯 C# 数据模型和 Ports 归属 `Spotlight.Domain`，具体内存验证适配器归属 `Spotlight.Adapters`。虽然目录保留 `Contracts/` 作为架构组织落点，但当前不创建独立 Contracts 程序集；可执行契约必须位于已有程序集边界内，避免落入默认程序集。

```text
Assets/Scripts/Architecture/Domain/Save/
├── SaveContainer.cs
├── SaveSnapshot.cs
├── PermanentProgress.cs
├── SettingsData.cs
├── CheckpointData.cs
└── ISaveService.cs / IProgressService.cs

Assets/Scripts/Architecture/Adapters/Save/
├── InMemorySaveService.cs
└── InMemoryProgressService.cs
```

`SaveContainer` 同时持有永久进度、设置和按 `checkpointId` 索引的快照；服务操作返回明确结果对象，适配器不把内存状态伪装成可靠文件存档。读取快照时只替换临时状态，永久进度保持独立。

## Issue 2 验证策略

EditMode 测试通过 Ports 验证空存档、覆盖、旧快照读取、永久进度保留、未知 ID、重置和设置边界。测试不得检查私有字段或实现调用次数。Unity 批处理导入和 EditMode Test Runner 用于确认 asmdef、适配器和行为测试可编译运行。
