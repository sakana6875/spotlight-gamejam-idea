# Demo2 固定行水平水流任务

## OpenSpec

- [x] 确认第三条水道来自向上蓄积和模型高度扩大
- [x] 确认保留原两行逐格向右流动
- [x] 更新并严格校验 proposal、spec、design、plan 和 tasks
- [x] 获得用户对修正规则的确认

## 实现

- [x] 恢复 TerrainWaterModel 的水平逐格状态
- [x] 删除 RaiseWaterBeforeBarrier 和所有跨行蓄积
- [x] 恢复 WaterTilemapRenderer 模型写回与 Wave 动画
- [x] 恢复 TerrainWaterController 流动步和水平阻挡采样
- [x] 将模型高度固定为手工水源行数
- [x] 通过 Unity Editor 恢复 demo2 水组件引用
- [x] 更新旧水流和推动障碍物 OpenSpec 合同

## 验证

- [x] 严格校验三个相关 OpenSpec
- [x] 编译 Spotlight.Game 生产程序集
- [x] 项目负责人确认两条水道逐格向右流动且不生成第三行

## 当前验证记录

- 已恢复两条手工水源行的逐格向右推进；模型高度固定为 `sourceBounds.size.y`，且代码中不存在 `RaiseWaterBeforeBarrier`、`row + 1` 或最高地面行扩高逻辑。
- Unity Editor 已恢复 GroundTilemap、SideWallTilemap、移动阻挡、0.15 秒流动步和 Wave Tile 引用。
- 三个相关 OpenSpec 均通过严格校验；`Spotlight.Game` 编译通过，0 个警告、0 个错误。
- 项目负责人已在 PlayMode 确认两条水道逐格向右流动，且不再生成第三行。
