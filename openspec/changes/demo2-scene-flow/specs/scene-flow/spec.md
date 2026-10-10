## Purpose

定义所有 Demo 共用的稳定场景 ID、集中映射和执行型加载边界，使 Demo 之间的场景切换不依赖散落 Unity 场景名、静态服务或隐式路由。

## ADDED Requirements

### Requirement: 当前真实场景使用集中稳定 ID

系统 MUST 通过 `SceneCatalog` 将稳定 ID `sample` 映射到 `SampleScene`，将 `demo2` 映射到 `demo2`。`SceneCatalog` MUST 在同一个脚本中声明这些 ID 并解析映射；未登记的 ID MUST NOT 被当作 Unity 场景名直接尝试加载。

#### Scenario: 请求已登记 Demo2

- **WHEN** 调用方请求稳定 ID `demo2`
- **THEN** 系统查询到 Unity 场景名 `demo2` 并只请求该场景

### Requirement: 场景请求结果明确且不决定路由

场景请求 MUST 区分已开始、当前已激活、未知 ID 与未能开始四种结果；请求失败 MUST 保留所请求的稳定 ID，不得加载备用场景或报告假成功。场景模块 MUST NOT 根据来源场景决定下一个 Demo、Hub、重试或退出目标。

#### Scenario: 请求未知稳定 ID

- **WHEN** 调用方请求未在映射表登记的稳定 ID
- **THEN** 系统返回未知 ID 结果，当前场景保持不变

### Requirement: 推迟通用场景入口

在出现真实场景控制器服务注入需求前，系统 MUST NOT 创建通用入口协议、自动扫描场景组件、订阅场景加载回调或自动启用配置目标。

#### Scenario: 加载当前场景

- **WHEN** 调用方请求当前活动场景对应的稳定 ID
- **THEN** 系统返回当前已激活结果，且不扫描或初始化任何场景组件