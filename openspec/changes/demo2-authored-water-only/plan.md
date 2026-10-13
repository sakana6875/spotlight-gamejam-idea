# Demo2 固定行水平水流实施计划

## 实施顺序

1. 更新并严格校验 OpenSpec，固定“保留水平逐格推进、删除跨行蓄积”的合同。
2. 恢复 `TerrainWaterModel`，删除 `RaiseWaterBeforeBarrier`，让每行只向右扩展。
3. 恢复 `WaterTilemapRenderer` 的手工源缓存、模型写回和水纹动画。
4. 恢复 `TerrainWaterController` 的流动步、地面横向范围、墙体与移动阻挡采样。
5. 将水道 BoundsInt 高度固定为 `sourceBounds.size.y`，不再使用最高地面行。
6. 通过 Unity Editor 恢复 GroundTilemap、SideWallTilemap、移动阻挡、流动步长和 Wave Tile 引用。
7. 更新旧水流与推动障碍物 OpenSpec，使其只允许原行水平扩展。
8. 不启动 Unity Test Runner；执行 OpenSpec 严格校验和生产程序集编译，由项目负责人手动确认只有两条水道且逐格右流。

## 文件范围

- `Assets/Scripts/Demos/Demo2/TerrainWaterModel.cs`
- `Assets/Scripts/Demos/Demo2/TerrainWaterController.cs`
- `Assets/Scripts/Demos/Demo2/WaterTilemapRenderer.cs`
- `Assets/Scenes/demo2.unity`
- 相关 OpenSpec 和既有测试源文件

## 不修改

玩家推动规则、水纹图片、Wave AnimatedTile 内容、场景中现有两行手工源水、视图转场、存档和场景加载。
