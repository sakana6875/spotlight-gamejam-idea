# 精简场景加载执行任务

## 1. OpenSpec

- [x] 1.1 确认稳定 ID、集中映射和执行型加载边界
- [x] 1.2 依据用户确认移除通用入口与值对象设计，更新提案、规格、设计、delta spec 和任务
- [x] 1.3 执行 `openspec validate demo2-scene-flow --strict` 并修复所有校验问题

## 2. 三脚本场景模块

- [x] 2.1 在单个 `SceneCatalog` 中合并稳定 ID 常量与 `sample`、`demo2` 映射
- [x] 2.2 将 `SceneLoader` 收缩为 `LoadScene(string sceneId)`，保留当前、未知和启动失败的明确结果
- [x] 2.3 更新 `SessionRoot` 组装，并删除无调用方的入口、值对象和映射条目类型

## 3. 验证

- [x] 3.1 重写稳定 ID、未知 ID 和当前场景的 PlayMode 行为验证
- [x] 3.2 执行目标 Unity PlayMode 测试并删除临时 XML 结果

## 当前验证记录

- `openspec validate demo2-scene-flow --strict` 已于 2026-10-10 通过。
- 已收缩为 `SessionRoot`、`SceneCatalog`、`SceneLoader` 三个运行时脚本；已删除通用入口、自动场景扫描、`SceneId` 值对象和映射条目类型。静态检查确认 `SceneManager` 仅存在于 `SceneLoader`，已删除类型无 Assets 调用方。
- `SceneFlowTests` 已使用 Unity `2022.3.62f3c1` 批处理 PlayMode 运行：筛选 `Spotlight.Tests.PlayMode.SceneFlowTests`，进程返回码为 0。日志显示已加载 `SampleScene`、执行未知 ID 路径，并在退出前保存结果。
- `Temp/scene-flow-playmode.xml` 在批处理退出后不存在，无临时 XML 残留。