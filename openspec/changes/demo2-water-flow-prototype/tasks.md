# 自动水源与地形水道任务

## OpenSpec

- [x] 读取现有场景、水流资产和运行时代码现状
- [x] 确认水只阻尼刚体，不推送或托起刚体
- [x] 获得用户对自动水源、GroundTilemap 水道和自动 Trigger 合同的确认
- [x] 更新提案、设计、规格、计划与任务
- [x] 执行 `openspec validate demo2-water-flow-prototype --strict`

## 实现

- [x] 扫描 WaterInteriorTilemap 的所有手工源格并返回 BoundsInt
- [x] 由 GroundTilemap 覆盖推导有限水道域并拒绝悬空源格
- [x] 每个物理步重采样地面、SideWallTilemap 和移动 Collider2D 阻挡
- [x] 按推导域保留源格、渲染扩张格并保持右向动画相位
- [x] 使用推导域自动配置 BoxCollider2D Trigger
- [ ] 通过 Unity Editor 为两个现有水根接入 GroundTilemap

## 验证

- [ ] 保留并验证 TerrainWaterModel 的右扩、阻挡、蓄积和移除阻挡测试
- [ ] 新增并验证 WaterTilemapRenderer 的自动源发现 EditMode 测试
- [ ] 新增并验证 TerrainWaterController 的地形和阻尼 PlayMode 测试
- [ ] 在 Unity 批处理执行三个目标测试类
- [ ] 在 Demo2 Side View Play Mode 验证两个水根、水道边界和格级阻尼

## 当前验证记录

- 自动源扫描、地形水道、自动 Trigger 与测试合同已由用户确认；本任务在实现后补充实际验证记录。
