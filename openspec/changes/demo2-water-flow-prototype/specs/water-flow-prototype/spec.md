## Purpose

定义 Demo2 `TerrainWaterArea` 的自动手工水源、地形限定水道和格级刚体阻尼合同。每个 `WaterInteriorTilemap` 的手工 Tile 是开局水和持续源；GroundTilemap 覆盖列界定有限水道；所有已占据水格只提供速度阻尼。

## ADDED Requirements

### Requirement: 水体由预制体实例手工组装

系统 MUST 提供 `TerrainWaterArea` 预制体并作为 `Demo2Map/Grid` 的子级放置。预制体 MUST 持有 WaterInteriorTilemap、运行组件和 Box Trigger 阻尼区；场景既有 SideWallTilemap 与 GroundTilemap MUST 与预制体实例平级。手工源水、SideWallTilemap、GroundTilemap 和移动 Collider2D 引用 MUST 由场景实例 Override 保存。系统 MUST NOT 按对象名称查找、自动创建运行时 Tilemap 或提供场景迁移逻辑。

#### Scenario: 配置现有水体实例

- **WHEN** 开发者在 Demo2 场景中选择已有 TerrainWaterArea 或 WaterChannelPrefab 实例
- **THEN** 可以在实例 Override 上指定同一 SideView 的 GroundTilemap，且不再需要填写源格原点、宽度或高度

### Requirement: 扫描全部手工水格为持续水源

系统 MUST 在首次显示前遍历 WaterInteriorTilemap 的完整 `cellBounds`，并将每个非空 Tile 作为永久源格和已占据水格。扫描 MUST 返回包围全部源格的 BoundsInt，且 MUST 保存每个源格的原始 Tile。系统 MUST NOT 限制扫描到预配置矩形、创建 SourceTilemap 或清除源格。

#### Scenario: 旧矩形外的不连续源格

- **WHEN** 开发者在旧 `(0,0), 12×4` 范围外铺设多个不连续 Tile
- **THEN** 每个 Tile 在首帧都是源格和水格，且扫描 BoundsInt 包围所有这些 Tile

#### Scenario: 空水 Tilemap

- **WHEN** WaterInteriorTilemap 不含任何 Tile
- **THEN** 控制器 MUST 记录 `地形边界水流配置无效：WaterInteriorTilemap 中没有手工水源格。` 并禁用

### Requirement: GroundTilemap 限定可用水道格

控制器 MUST 使用 GroundTilemap 的占据列创建有限模拟域。域下界 MUST 为最低源行；域上界 MUST 为最高源行与最高 GroundTilemap 占据行的较大值；横向 MUST 从最低源列延续到最高有地面覆盖列。候选水格所在列只有在存在严格位于该格下方的 GroundTilemap Tile 时才可用；其余候选格 MUST 通过模型阻挡。每个源格 MUST 受地面支撑。GroundTilemap 不含 Tile 时，控制器 MUST 记录 `地形边界水流配置无效：GroundTilemap 没有可支撑水道的地面格。` 并禁用。

#### Scenario: 右侧地面覆盖和缺口

- **WHEN** 源水右侧有一列下方地面 Tile，之后的列没有任何下方地面 Tile
- **THEN** 流动步可占据有地面覆盖列，且 MUST NOT 占据无地面覆盖列

#### Scenario: 悬空源格

- **WHEN** 任一手工源格在同列不存在严格位于其下方的 GroundTilemap Tile
- **THEN** 控制器 MUST 记录配置错误并禁用，而不是保留无法支撑的水源

### Requirement: 水从源格按流动步长扩张

系统 MUST 在首次写入 WaterInteriorTilemap 前读取手工源格、GroundTilemap、SideWallTilemap 和指定移动 Collider2D 的状态，但 MUST NOT 在 Awake 中预先填满所有可达水格。首次显示 MUST 只含源格；每个后续流动步 MUST 最多向右扩张一层，直到受阻、蓄积或越过有限高度障碍。控制器 MUST 每个物理步重新采样地面覆盖、墙体和移动 Collider2D；移开箱子后，水 MUST 在后续流动步继续右扩。

#### Scenario: 移开阻挡后继续流动

- **WHEN** 指定移动 Collider2D 不再覆盖曾阻挡的水格中心
- **THEN** 已蓄积水在后续流动步继续向新开放的右侧格扩张


### Requirement: 水只进行水平扩展和障碍前蓄积

水模拟器 MUST 使用预分配离散水格和工作区。水格只可向右扩展；右侧受阻时，水 MUST 在障碍左侧向上蓄积，并在超过障碍顶端后继续向右。水 MUST NOT 向下掉落、沿台阶下泄、越过推导水道域或模拟瀑布。

#### Scenario: 有限障碍上的蓄积

- **WHEN** 水流右侧存在有限高度的阻挡格
- **THEN** 水在障碍左侧向上蓄积，超过障碍顶端后继续向右，且不向下填充

### Requirement: 同一水 Tilemap 显示右向流动

系统 MUST 使用既有 `Wave.asset` 及其 AnimatedTile 在同一 WaterInteriorTilemap 中显示水纹。每个已占水格 MUST 按其相对于推导 BoundsInt 的列坐标取得稳定动画相位偏移，并随时间推进为右向视觉流动。视觉相位 MUST NOT 改变水格占据、模型规则、刚体阻尼或其他物理状态。

#### Scenario: 稳定水体持续可见流动

- **WHEN** 手工源格和扩张水格已达到稳定状态
- **THEN** 水纹仍持续向右移动，且水格位置、墙体和刚体不会因该视觉更新而移动

### Requirement: 自动范围的格级阻尼

WaterDampingArea MUST 要求同根 `BoxCollider2D` 且其 `isTrigger` 为真。控制器 MUST 在成功推导水道域后使用 WaterInteriorTilemap 单元四角在 Trigger 根节点的局部坐标配置该 Box 的 offset 和 size，不得假定 Transform 原点、父子关系或缩放。Box MUST 只用于收集候选 Rigidbody2D；每个物理步 MUST 仅对实际已占据水格中的刚体施加与当前速度反向的阻尼。系统 MUST NOT 施加右向推力、向上浮力、游泳状态或实体阻挡。

#### Scenario: 候选水道中的阻尼

- **WHEN** 两个刚体同时位于自动 Box 范围，一个位于已占水格，另一个位于尚未占水的候选格
- **THEN** 前者速度逐物理步衰减，后者速度不变，二者均不得获得水平或向上水力
