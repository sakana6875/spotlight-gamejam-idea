## Why

仓库已有可跨场景持久化的 `SessionRoot` 与 `SceneLoader`，但只有 `SampleScene` 中会写烟测数据的 `SessionSmokeEntry`，没有正式启动入口。用户已经创建 `BootStrap.unity` 并将其置于Build Settings首位，需要一个不会执行烟测副作用、只负责进入初始玩法场景的入口组件。

## What Changes

- 新增 `BootstrapEntry`，要求与 `SessionRoot` 位于同一GameObject。
- 在全部Awake完成后的Start阶段确认组合根初始化，并通过 `SessionRoot.SceneLoader` 加载序列化的稳定场景ID。
- 初始稳定ID默认使用现有 `SceneCatalog.Demo2`，对应Build Settings中的 `demo2.unity`。
- `Started`和`AlreadyActive`视为成功；空ID、组合根初始化失败、未知ID或Unity拒绝加载时输出明确错误。
- 不修改用户已经创建的 `BootStrap.unity`、Build Settings或玩法场景；项目负责人负责把组件挂到现有SessionRoot对象。

## Capabilities

### New Capabilities

- `production-bootstrap-entry`: 定义正式组合根启动、稳定场景ID加载、成功结果和失败可观察性。

### Modified Capabilities

- 无。

## Impact

- 新增 `Assets/Scripts/Bootstrap/BootstrapEntry.cs` 及meta，仍属于 `Spotlight.Game`。
- 复用现有 `SessionRoot`、`SceneLoader`、`SceneCatalog.Demo2`和 `SceneLoadResult`，不新增服务接口。
- `SampleScene/ArchitectureSmoke` 与 `SessionSmokeEntry` 保持测试用途，不进入正式启动场景。

## 非目标

- 不引入 `SessionRoot.Instance`、Service Locator、Player单例或第二个场景加载入口。
- 不实现Loading UI、进度条、重试、菜单、存档恢复或第一幕场景注入。
- 不由Agent修改场景、Prefab或Build Settings。
