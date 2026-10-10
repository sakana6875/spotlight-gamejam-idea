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
- [ ] 3.2 执行目标 Unity PlayMode 测试并删除临时 XML 结果

## 当前验证记录

- `openspec validate demo2-scene-flow --strict` 已于 2026-10-10 通过。
- 已收缩为 `SessionRoot`、`SceneCatalog`、`SceneLoader` 三个运行时脚本；已删除通用入口、自动场景扫描、`SceneId` 值对象和映射条目类型。静态检查确认 `SceneManager` 仅存在于 `SceneLoader`，已删除类型无 Assets 调用方。
- `SceneFlowTests` 已覆盖集中映射、当前 `sample` 场景和未知 ID 结果；尚未在 Unity Test Runner 中执行。
- 当前 Unity Editor 占用项目，Harness 不能启动第二个批处理 PlayMode；需在已打开的 Editor Test Runner 中执行 `SceneFlowTests`，并确认此精简版本重新导入且编译通过。