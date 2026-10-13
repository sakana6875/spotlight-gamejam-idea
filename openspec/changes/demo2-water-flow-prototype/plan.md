# Demo2 固定行水平水流计划

1. 恢复 TerrainWaterModel 的源格补水和同一行向右扩展。
2. 删除 RaiseWaterBeforeBarrier 和跨行蓄积。
3. 恢复 WaterTilemapRenderer 模型写回和 Wave 动画。
4. 恢复 TerrainWaterController 流动步、地面、墙体和移动阻挡采样。
5. 将 BoundsInt 高度固定为手工水源行数。
6. 通过 Unity Editor 恢复场景引用。
7. 编译生产程序集；由项目负责人确认两条水道逐格右流且不生成第三行。
