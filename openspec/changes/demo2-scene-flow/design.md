# 精简场景加载技术设计

## Context

见 `proposal.md` 的动机和 `specs/scene-flow/spec.md` 的可观察合同。当前 `SessionRoot` 是唯一组合根，`SceneLoader` 是当前唯一 Unity 场景加载适配器；当前实际场景只有 `SampleScene` 与 `demo2`，没有真实关卡控制器需要加载后服务注入。

## Goals / Non-Goals

**Goals:**

- 以三个脚本表达当前真实需要：`SessionRoot` 组装、`SceneCatalog` 映射、`SceneLoader` 加载。
- 为当前及后续 Demo 保留稳定 ID、集中映射、唯一 `SceneManager` 边界和可观察结果。

**Non-Goals:**

- 不创建或修改场景资产、Build Settings、玩家 Prefab、输入、视图转场、暂停、水路、交互、战斗、对话或任何 Demo 路由状态机。
- 不提供 `EnterDemo`、`RestartDemo`、`LoadHub` 等高层流程 API；调用方在自身规则确定稳定 ID 后调用加载器。
- 不创建通用入口、自动场景扫描、服务定位器、全局业务单例、备用场景或自动重试。

## Decisions

### 1. `SceneCatalog` 在一个脚本中定义 ID 和映射

`SceneCatalog` 提供 `Sample`、`Demo2` 稳定 ID 常量，并在同一类中解析 `sample` → `SampleScene` 与 `demo2` → `demo2`。`SessionRoot` 创建一个 `SceneCatalog` 后传给 `SceneLoader`。

当前两个条目不需要独立 `SceneId` 值对象、`SceneCatalogEntry` 数据类型或 ScriptableObject。未来新增真实场景时，由其 Change 更新此映射；场景数量或内容配置增长到需要资产化时再评估。

### 2. 场景模块只执行目标请求

`SceneLoader.LoadScene(string sceneId)` 先通过目录解析 Unity 场景名，再区分当前已激活、未登记 ID、开始加载和无法开始请求。它不根据来源场景决定下一场景，也不加载备用场景。

### 3. 推迟没有消费者的场景入口注入

当前没有任何正式场景控制器需要 `SceneLoader`。因此加载器不订阅 `sceneLoaded`，不扫描组件，也不初始化目标。某个具体控制器确实需要跨场景服务时，由该 Feature Change 为该控制器建立最小注入路径；不得提前恢复通用入口框架。

## Risks / Trade-offs

- [新 Demo 尚未存在] → 目录只登记两个当前实际场景；未登记 ID 返回明确结果，不预置空 Demo API。
- [未来关卡需要服务注入] → 届时增加由真实控制器驱动的最小接线，而不是复活全局通用入口。
- [代码映射新增条目需修改 `SceneCatalog`] → 在当前两条真实映射下，这比额外配置资产更清晰。

## Migration Plan

1. 合并 ID、映射条目和目录实现为单个 `SceneCatalog`。
2. 收缩 `SceneLoader` 与 `SessionRoot`，删除加载后入口初始化。
3. 删除无调用方的入口类型，重写稳定 ID 与加载结果 PlayMode 测试。