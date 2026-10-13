# Demo2 固定行水平水流设计

## 保留

- TerrainWaterModel 的源格、阻挡格和逐步状态。
- 每个流动步最多向右扩展一格。
- GroundTilemap 横向范围与支撑检查。
- SideWallTilemap、移动 Collider2D 阻挡和移开后继续流动。
- 同一 Tilemap 写回、Wave 动画和格级阻尼。

## 删除

- `RaiseWaterBeforeBarrier`。
- `row + 1` 跨行写入。
- 使用最高 GroundTilemap 行扩大模型高度。
- 障碍前抬高水位和越障。

## 不变量

模型高度固定为 `sourceBounds.size.y`。当前场景手工源水为两行，因此运行时只能存在两条水道。
