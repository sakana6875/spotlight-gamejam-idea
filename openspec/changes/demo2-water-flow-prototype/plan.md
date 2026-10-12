# Demo2 自动水源与阻尼 Trigger 实施计划

## 实施顺序

1. 修订 OpenSpec 提案、设计、规格、计划和任务，使 GroundTilemap 覆盖、自动源扫描和自动 Trigger 成为可观察合同。
2. 在 WaterTilemapRenderer 中扫描完整 `cellBounds` 的全部非空手工 Tile，返回源格 BoundsInt 并一次保存原始 Tile。
3. 在 TerrainWaterController 删除手填矩形，新增 GroundTilemap；由源格与地形范围建立有限 TerrainWaterModel，并验证源格有下方地面支撑。
4. 每个物理步将地面覆盖、SideWallTilemap 与移动 Collider2D 采样为模型阻挡；继续使用同一 Tilemap 进行渲染和动画相位。
5. 在 WaterDampingArea 将 Collider2D 约束为同根 BoxCollider2D Trigger，并根据推导 BoundsInt 自动配置其本地 offset 与 size。
6. 通过 Unity Editor 为 WaterChannelPrefab 与 TerrainWaterArea 的两个现有控制器接入同一 SideView GroundTilemap；不手改 `.unity` YAML。
7. 保留 TerrainWaterModel 的纯模型行为测试，新增 WaterTilemapRenderer EditMode 源发现测试和 TerrainWaterController PlayMode 地形/阻尼测试。
8. 严格验证 OpenSpec、目标 Unity 测试和 Demo2 Side View Play Mode。

## 文件范围

- `openspec/changes/demo2-water-flow-prototype/`：本变更的合同和执行任务。
- `Assets/Scripts/Demos/Demo2/`：控制器、Tilemap 显示与阻尼组件。
- `Assets/Tests/EditMode/`：水格规则和源发现行为测试。
- `Assets/Tests/PlayMode/`：地形水道与阻尼行为测试。
- `Assets/Scenes/demo2.unity`：仅由 Unity Editor 保存 GroundTilemap 实例引用。

## 不修改

`Wave.asset`、`Waves.png`、Build Settings、`SessionRoot`、`SceneCatalog`、`SceneLoader`、输入、玩家、所有 Act 控制器和水模型的离散规则。
