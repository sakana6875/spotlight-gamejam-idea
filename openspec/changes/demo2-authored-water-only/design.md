# Demo2 固定行水平水流技术设计

## 根因

旧 `TerrainWaterModel.Step()` 在右侧受阻时调用 `RaiseWaterBeforeBarrier(column, row)`，把连续水体复制到 `row + 1`。同时 `TerrainWaterController.CreateWaterBounds` 使用最高 GroundTilemap 行扩大模型高度。这两个条件共同允许场景两行水源生成第三行水。

## 决策

### 1. 恢复水平逐格状态

恢复 `TerrainWaterModel` 的源格、阻挡格、当前水格和下一步工作区。`Step()` 保留源格补水和向右一格扩展，但彻底删除 `RaiseWaterBeforeBarrier`。

### 2. 固定模拟行数

模型宽度仍从最低源列延伸到最高地面覆盖列。模型高度只使用 `sourceBounds.size.y`，模型下界仍是 `sourceBounds.yMin`。GroundTilemap 的最高行不参与高度计算。

### 3. 水平阻挡

控制器继续在每个物理步采样当前模型范围内的地面支撑、SideWallTilemap 和移动 Collider2D。阻挡只设置当前格不可占据，不触发任何跨行求解。

### 4. 同 Tilemap 渲染

渲染器在启动时缓存手工源 Tile。模型扩展格使用既有 `Wave.asset`，未占据格保持为空。写回范围严格限制在固定行模型 BoundsInt 中，因此不可能写入第三行。

### 5. 阻尼

阻尼 Trigger 覆盖固定行水道域，精确阻尼仍查询模型当前占据状态。

## 不变量

- 水格的世界行坐标始终位于 `sourceBounds.yMin..sourceBounds.yMax-1`。
- `Step()` 不读取或写入其他行。
- GroundTilemap 只能影响模型宽度和格支撑，不能改变高度。
