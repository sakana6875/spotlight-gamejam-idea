# 通用场景加载与入口实施计划

## 实施顺序

1. 在 `Scene` 目录新增纯 C# `SceneId`、`SceneCatalog` 与明确结果模型；`SessionRoot` 显式登记 `sample` → `SampleScene`、`demo2` → `demo2`。
2. 将 `SceneLoader` 收敛为 `LoadScene(SceneId)`，保持其作为唯一 `SceneManager` 边界并只执行调用方给定目标。
3. 用通用 `SceneEntry` 替换 Demo2 专用入口；让 `SessionRoot` 初始化首场景，让 `SceneLoader` 在后续场景加载后沿同一路径初始化入口。
4. 用 PlayMode 验证当前场景中的稳定 ID 请求、未知 ID、当前场景和入口单次初始化。

## 目录与程序集

- `Assets/Scripts/Scene/`：`SceneId`、`SceneCatalog`、`SceneLoadResult`、`SceneLoader`、入口协议与 `SceneEntry`，属于 `Spotlight.Game`。
- `Assets/Scripts/Bootstrap/`：`SessionRoot` 中当前真实映射的唯一组装。
- `Assets/Tests/PlayMode/`：通用加载结果和入口注入验证。

## 验证路径

- `sample` 与 `demo2` 映射到当前真实 Unity 场景名；未登记 ID 返回明确结果且不回退。
- `SceneLoader` 不判断来源/目标 Demo 路由，只执行调用方给定的 `SceneId`。
- 当前活动场景中的 `SceneEntry` 只接收一个 `SceneLoader` 实例，重复初始化不重复执行。
- 缺少加载器或引用缺失时，入口不启用其配置目标，并输出包含场景与对象的配置错误。