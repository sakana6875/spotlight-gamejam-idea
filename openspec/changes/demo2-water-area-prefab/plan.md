# Demo2 空白水域 Prefab 实施计划

1. 严格校验 OpenSpec，并确认 Edit Mode/Play Mode 所有权边界。
2. 通过 Unity Editor 创建内部引用完整、Tilemap 为空的 TerrainWaterArea.prefab。
3. 捕获 demo2 现有两行水 Tile 的 TileBase、矩阵、颜色和 flags。
4. 用 Prefab 实例替换普通 TerrainWaterArea，保持父节点和世界位置。
5. 将捕获 Tile 写入实例 WaterInteriorTilemap，不写入 Prefab 资产。
6. 恢复 GroundTilemap、SideWallTilemap、移动障碍、流动步和 Wave 引用。
7. 保存并重新加载场景，确认实例水格存在、Prefab 资产水格为0。
8. 编译 Spotlight.Game；不启动 Unity Test Runner。
9. 由项目负责人在 Edit Mode 验证实例可以绘制和擦除，并在 Play Mode 验证现有水流不回归。

## 文件范围

- `openspec/changes/demo2-water-area-prefab/`
- `Assets/Resources/Demo2/Water/TerrainWaterArea.prefab`
- `Assets/Scenes/demo2.unity`

## 不修改

水流状态机、树生命周期、玩家、推动、输入、场景加载、存档和 Packages。
