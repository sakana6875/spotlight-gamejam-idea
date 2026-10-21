## Purpose

定义可复用、初始空白、支持场景实例手工绘制水源的 Demo2 水域 Prefab 合同。

## ADDED Requirements

### Requirement: 水域 Prefab 资产必须保持空白

系统 MUST 提供 `TerrainWaterArea.prefab`。Prefab MUST 包含 Grid、WaterInteriorTilemap、TerrainWaterController、WaterTilemapRenderer、WaterDampingArea 和 BoxCollider2D Trigger。Prefab 资产中的 WaterInteriorTilemap MUST 不包含任何 Tile。

#### Scenario: 新建水域实例

- **WHEN** 开发者把 TerrainWaterArea.prefab 拖入场景
- **THEN** 实例包含完整运行组件
- **AND** WaterInteriorTilemap 初始为空

### Requirement: 场景实例可以绘制和擦除手工水格

开发者 MUST 能在 Edit Mode 选择 Prefab 实例内的 WaterInteriorTilemap，并使用 Tile Palette 绘制或擦除 Tile。修改后的 Tile 数据 MUST 保存在该场景实例中，不得要求修改 Prefab 资产。

#### Scenario: 两个关卡使用不同水道

- **WHEN** 两个场景实例分别绘制不同位置和行数的水格
- **THEN** 两个实例各自保存自身水道布局
- **AND** Prefab 资产仍保持空白

### Requirement: 场景外部引用必须使用实例 Override

GroundTilemap、SideWallTilemap 和移动 Collider2D MUST 在场景实例上显式配置。Prefab 资产 MUST NOT 查找或引用具体场景对象，也不得按名称运行时扫描。

#### Scenario: 实例缺少地面引用

- **WHEN** 水域实例未配置 GroundTilemap 或 SideWallTilemap
- **THEN** 控制器明确报配置错误并禁用，不加载备用对象

### Requirement: Play Mode 后由水流模拟接管 Tilemap

进入 Play Mode 时，系统 MUST 扫描实例中已绘制的 Tile 并缓存为永久源格。之后运行时 Tilemap 写回 MUST 只表达该实例水流状态。Play Mode 中的手工 Tile Palette 修改不属于支持的制作流程。

#### Scenario: 空白实例直接运行

- **WHEN** 水域实例没有任何手工水格并进入 Play Mode
- **THEN** 控制器报告没有手工水源格并禁用
- **AND** 不自动生成水格

### Requirement: demo2 迁移保持现有水道

系统 MUST 使用 TerrainWaterArea Prefab 实例替换 demo2 当前普通场景对象，并保留当前两行手工 Wave Tile、0.15秒流动步、地面、墙体、移动障碍和阻尼配置。

#### Scenario: 迁移当前关卡

- **WHEN** demo2 场景完成 Prefab 迁移
- **THEN** Edit Mode 中仍存在原两行手工水格
- **AND** Prefab 资产本身不包含这些 Tile
