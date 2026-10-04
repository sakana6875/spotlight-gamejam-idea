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
