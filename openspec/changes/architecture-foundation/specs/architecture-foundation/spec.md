# 架构基础能力

## ADDED Requirements

### Requirement: 生产代码目录必须位于 Assets/Scripts

生产代码 MUST 位于 `Assets/Scripts/` 的嵌套目录中，测试代码 MUST 位于 `Assets/Tests/` 的嵌套目录中。项目根目录不得出现 `AssetsScripts*`、`AssetsData*` 或 `AssetsTests*` 形式的拼接目录。

#### Scenario: 检查生产代码目录

- **WHEN** 检查项目代码目录
- **THEN** 架构代码位于 `Assets/Scripts/Architecture/`
- **AND** Feature 代码位于 `Assets/Scripts/Features/`
- **AND** 测试代码位于 `Assets/Tests/EditMode/` 或 `Assets/Tests/PlayMode/`

### Requirement: 核心程序集必须具有明确边界

项目 MUST 建立 `Spotlight.Domain`、`Spotlight.Application`、`Spotlight.Adapters`、`Spotlight.Features.*`、`Spotlight.Tests.EditMode` 和 `Spotlight.Tests.PlayMode` 程序集定义。每个程序集定义 MUST 有对应 `.meta` 文件。

#### Scenario: 检查核心程序集

- **WHEN** Unity 导入程序集定义
- **THEN** 每个目标程序集都能被识别
- **AND** 程序集名称与项目约定一致
- **AND** 生产程序集不引用测试程序集

### Requirement: Domain 和 Application 必须保持依赖方向

`Spotlight.Domain` MUST 不引用具体 Unity 场景、UI、音频、场景管理或存档实现。`Spotlight.Application` MUST 不引用 `Spotlight.Adapters`。

#### Scenario: 检查底层程序集引用

- **WHEN** 查看 Domain 和 Application 的 asmdef 引用
- **THEN** Domain 不包含具体 Unity 场景相关程序集引用
- **AND** Application 只引用允许的 Domain 和基础运行库
- **AND** Application 不引用 Adapters

### Requirement: Adapters 和 Features 必须遵守上层边界

`Spotlight.Adapters` MAY 引用 `Spotlight.Application` 和 `Spotlight.Domain`。每个 `Spotlight.Features.*` 程序集 MUST 只依赖公开契约所在程序集、`Spotlight.Domain` 和 `Spotlight.Application`，不得依赖其他 Feature 的具体实现。

#### Scenario: 检查上层程序集引用

- **WHEN** 查看 Adapters 和 Feature 的 asmdef 引用
- **THEN** Adapters 可以引用 Application 和 Domain
- **AND** Feature 不引用其他 Feature 的具体程序集
- **AND** 不存在反向的 Domain/Application 对 Adapters 引用

### Requirement: 现有 Unity 资源必须保持不变

本次架构基础变更 MUST 不修改 `SampleScene`、现有 URP 2D Renderer、Build Settings 或 Packages 配置。

#### Scenario: 检查现有资源保护

- **WHEN** 完成程序集边界建立后比较现有项目资源
- **THEN** `Assets/Scenes/SampleScene.unity` 内容和引用保持不变
- **AND** 现有 URP 2D Renderer 资源保持不变
- **AND** Build Settings 与 Packages 配置保持不变
