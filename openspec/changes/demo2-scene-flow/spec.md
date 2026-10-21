# 精简场景加载总规格

本 Change 建立所有 Demo 共用的稳定场景 ID、集中映射与执行型场景加载边界。可观察行为以 `specs/scene-flow/spec.md` 为准。

## 架构不变量

- `SessionRoot` 是唯一组合根；`SceneLoader` 是唯一运行时 `SceneManager` 边界。
- 调用方通过 `SceneCatalog` 定义的稳定 ID 字符串请求目标；`SceneCatalog` 在一个脚本中维护 ID 和 Unity 场景名映射。
- 场景模块只执行调用方请求，不决定 Hub、Demo、重试、完成或退出的路由。
- 在出现真实场景控制器服务注入需求前，不创建通用入口、自动场景扫描或加载后注入链。
- 本 Change 不创建或修改正式场景、Build Settings、玩家 Prefab、输入、视图转场、水路、战斗、敌人、对话或 Act 完成条件。