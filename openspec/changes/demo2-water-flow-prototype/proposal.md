# Demo2 自动水源与地形水道提案

## 背景

Demo2 场景中的两套水体都在 `WaterInteriorTilemap` 的旧 Inspector 矩形之外铺设了手工源水，导致控制器扫描不到源格并禁用。水道的真实横向边界来自 `GroundTilemap` 的地面覆盖列，而不是预先填写的原点、宽度和高度。现有 `TerrainWaterArea` 的 `1×1` Trigger 也不足以收集整段候选水道内的刚体。

## 目标

1. 让 `WaterInteriorTilemap` 中每个手工 Tile 自动成为开局水和持续水源，不依赖 Inspector 矩形。
2. 由 `GroundTilemap` 中可支撑候选水格的列定义有限水道，使水只向右扩张到有地面覆盖的列。
3. 保持 SideWallTilemap 与指定移动 `Collider2D` 的阻挡、蓄积、越过有限障碍和移开后续流动。
4. 自动将 `WaterDampingArea` 的 Box Trigger 覆盖推导出的水道域；阻尼仍只作用于实际占水格。
5. 保持既有 `Wave.asset`、同一 Tilemap 的右向动画相位、无推力和无浮力合同。

## 范围

- 删除控制器上的手填水道原点、宽度和高度，改为扫描全部手工源格并采样 `GroundTilemap`。
- 更新运行时水道域、显示、阻尼 Trigger 和场景实例引用。
- 增加自动源发现 EditMode 测试，以及地形覆盖与运行时阻尼 PlayMode 测试。
- 更新现有纯模型测试，继续证明右扩、阻挡、蓄积、越障和移除阻挡行为。

## 非目标

- 不创建 `SourceTilemap`、第二个水 Tilemap、运行时水资产、自动场景装配器或按对象名称查找。
- 不实现无界扩张、向下落水、瀑布、完整流体模拟、浮力、推力、游泳或实体水碰撞。
- 不修改 `Wave.asset`、`Waves.png`、`SceneCatalog`、`SceneLoader`、`SessionRoot`、Build Settings 或引入新包。
- 不让动态 `GroundTilemap` 重建水道域；动态箱子仍只影响格阻挡。

## 主要决策

- 扫描 `WaterInteriorTilemap.cellBounds` 内所有非空 Tile；扫描结果的最小/最大格坐标形成源格 BoundsInt，并一次保存原始 Tile。
- `GroundTilemap` 中只要存在严格位于候选水格下方同列的 Tile，该候选水格即可用。每个源格都必须受此规则支撑，空地面或悬空源格会明确报错并禁用。
- 水道横向从最低源列到最高有地面覆盖列；下界为最低源行，上界为最高源行与最高地面占据行的较大值。域内但无地面覆盖的候选格被标为阻挡。
- 每个物理步重新采样地面覆盖、SideWallTilemap 和移动 Collider2D，因此移动箱子移开后可继续流动；超出有限地形域的格永远不可达。
- Box Trigger 通过 Tilemap 单元角点变换到自身根节点的局部坐标设置 offset 与 size，不假定父子 Transform 原点或缩放。

## 风险

- 地面覆盖按“严格位于水格下方的同列 Tile”定义，不要求相邻。若关卡改为必须贴地，需要重铺源水或单独修改合同。
- 地面 Tilemap 被视为静态地形。未来运行时铺地需要独立 Change，定义域重建和已流出水的去向。
- Collider2D 仍按水格中心采样，细小或斜向箱体边缘受网格分辨率影响。
