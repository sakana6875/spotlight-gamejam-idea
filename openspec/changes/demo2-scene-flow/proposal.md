# 精简场景加载提案

## Why

当前实际只有两个场景，并且没有任何真实玩法控制器需要在场景加载后接收跨场景服务。先前的值对象、映射条目、通用入口协议和自动注入链没有消费者，增加了理解与验证成本。

## What Changes

- `SessionRoot` 继续是唯一组合根，仅创建 `SaveService` 与 `SceneLoader`。
- `SceneCatalog` 在同一脚本维护稳定 ID 常量与当前 `sample` → `SampleScene`、`demo2` → `demo2` 映射；不拆分 `SceneId` 或 `SceneCatalogEntry`。
- `SceneLoader` 是唯一运行时 `SceneManager` 边界，接收稳定 ID 字符串并返回明确结果；它不订阅场景回调、不扫描入口、不决定路由。
- 删除没有真实调用方的通用 `SceneEntry`、`ISceneEntryPoint` 与 Demo2 专用入口。

## Capabilities

### New Capabilities

- `scene-flow`：稳定场景 ID、集中映射和明确的执行型场景请求。

### Modified Capabilities

- 无。当前主规格目录没有可修改的通用场景流转规格；`architecture-simplification` 继续是架构来源。

## Impact

- `Assets/Scripts/Bootstrap/SessionRoot.cs`、`Assets/Scripts/Scene/` 下的映射、加载器与结果类型。
- `Assets/Tests/PlayMode/` 中的稳定 ID 和加载结果验证。
- 不创建或修改正式场景、Build Settings、玩家 Prefab、输入、视图转场、水路、敌人、对话或 Demo 路由状态机。