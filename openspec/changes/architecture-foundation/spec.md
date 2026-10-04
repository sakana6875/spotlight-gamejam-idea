# 架构基础规格

## 1. 目录边界

### 要求：生产代码必须位于 Assets/Scripts

项目生产代码 MUST 位于 `Assets/Scripts/` 的嵌套目录中，不得在项目根目录创建 `AssetsScripts`、`AssetsData`、`AssetsTests` 等拼接名称目录。

`Assets/Scripts/Architecture/` MUST 至少包含：

- `Contracts/`
- `Domain/`
- `Application/`
- `Adapters/`
- `Composition/`

`Assets/Scripts/Features/` MUST 作为后续 Feature 程序集的父目录。

测试代码 MUST 位于 `Assets/Tests/EditMode/` 或 `Assets/Tests/PlayMode/`。

### 要求：程序集定义必须与目录边界一致

每个生产程序集 MUST 有对应的 `.asmdef` 和 `.meta` 文件。程序集定义不得通过无关目录或隐式默认程序集绕过边界。

## 2. 程序集与依赖

### 要求：Domain 保持 Unity 无关

`Spotlight.Domain` MUST 不引用 `UnityEngine`、Unity 场景、UI、AudioSource、SceneManager 或具体存档实现程序集。

Domain 代码只能依赖 .NET 基础类型和本程序集内的领域代码。若当前没有领域规则需要实现，不得为了填充目录创建假服务或空业务接口。

### 要求：Application 依赖方向

`Spotlight.Application` MUST 只依赖 `Spotlight.Domain` 及必要的基础运行库，不得引用 `Spotlight.Adapters` 或具体 Unity 适配器。

### 要求：Adapters 依赖方向

`Spotlight.Adapters` MAY 依赖 `Spotlight.Application` 和 `Spotlight.Domain`，用于承载 Unity 生命周期和外部系统适配。生产代码不得反向让 Domain 或 Application 引用 Adapters。

### 要求：Feature 依赖方向

每个 `Spotlight.Features.*` 程序集 MUST 只依赖公开契约所在程序集、`Spotlight.Domain` 和 `Spotlight.Application`，不得引用其他 Feature 的具体程序集或具体 MonoBehaviour。

### 要求：测试依赖方向

`Spotlight.Tests.EditMode` 和 `Spotlight.Tests.PlayMode` MUST 只引用被测试生产程序集以及 Unity Test Framework 所需程序集。生产程序集 MUST NOT 引用测试程序集。

## 3. 当前交付内容

### 要求：本次只交付结构基础

本次变更 MUST 只实现 Issue #1 的目录、asmdef 和最小可编译入口。不得在本次变更中实现 SessionRoot、服务初始化、场景流程、存档、事件总线、音频、输入、对话或 Demo 玩法。

### 要求：入口类型不能是假实现

为了让程序集被 Unity 保留和编译，可以提供说明程序集职责的最小真实类型，但不得提供假成功、空方法、未实现业务返回值或误导性服务入口。仅为保留空目录创建的类型不应声明尚未确定的公共业务 API。

## 4. 资源与现有项目保护

### 要求：不改变现有 Unity 资源

本次 MUST 保持以下内容不变：

- `Assets/Scenes/SampleScene.unity`
- `Assets/Settings/Renderer2D.asset`
- `Assets/Settings/UniversalRP.asset`
- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`

不得创建正式场景或改变 Build Settings。

## 5. 验收

满足以下条件才算完成：

1. 正确的嵌套目录位于 `Assets/Scripts` 和 `Assets/Tests` 下，项目根目录不存在 `AssetsScripts*`、`AssetsData*` 或 `AssetsTests*` 形式的错误目录。
2. 所有目标 asmdef 和 `.meta` 文件存在，名称与程序集规范一致。
3. asmdef 引用方向满足 Domain → Application → Adapters 的反向禁止关系，以及 Feature 和测试边界。
4. Unity 能识别这些 asmdef；Domain/Application 不因 Unity 场景依赖而产生编译引用。
5. 现有 SampleScene、URP 2D 资源和包依赖未被修改。
6. 针对可执行环境完成程序集编译或 Editor 验证；无法由 Agent 执行的 Unity Editor 验证必须明确标注“Editor 验证未完成”。
