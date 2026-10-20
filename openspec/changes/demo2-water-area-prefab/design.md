# Demo2 空白水域 Prefab 技术设计

## Prefab 结构

```text
TerrainWaterArea (Grid)
├── TerrainWaterController
├── WaterTilemapRenderer
├── WaterDampingArea
├── BoxCollider2D (isTrigger)
└── WaterInteriorTilemap
    ├── Tilemap
    └── TilemapRenderer
```

Prefab 内部引用：

- WaterTilemapRenderer → WaterInteriorTilemap；
- WaterTilemapRenderer → Wave.asset；
- TerrainWaterController → WaterTilemapRenderer；
- WaterDampingArea → TerrainWaterController。

场景 Override：

- TerrainWaterController → GroundTilemap；
- TerrainWaterController → SideWallTilemap；
- TerrainWaterController → movingBlockers。

## 迁移算法

1. 读取旧 WaterInteriorTilemap 的 `cellBounds`、TileBase、颜色、变换矩阵和 flags。
2. 创建内部引用完整且水 Tilemap 为空的 Prefab 资产。
3. 在旧对象原父节点和原 Transform 下实例化 Prefab。
4. 将捕获的 Tile 数据写入新实例 WaterInteriorTilemap，形成场景实例 Override。
5. 配置外部引用和流动参数。
6. 删除旧普通场景对象并保存 demo2。
7. 重新加载 Prefab 资产，确认其 WaterInteriorTilemap `GetUsedTilesCount()` 为0。

## 编辑规则

- 关卡制作者只在 Edit Mode 绘制实例 Tilemap。
- 不对关卡专用 Tile 数据执行 Apply All。
- Play Mode 后模拟拥有 Tilemap 写权限；退出 Play Mode 后 Unity 自动恢复场景手工布局。

## 坐标规则

Prefab 根节点自带 Grid，因此水格坐标不依赖外部 Demo2Map/Grid。Ground 和墙体通过世界坐标转换到水 Tilemap 坐标，现有控制器合同保持有效。
