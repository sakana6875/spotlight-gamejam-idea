## Purpose

定义 Demo2 手工起始水道保持原行并按流动步向右扩展，禁止向上或向下生成额外水道行。

## ADDED Requirements

### Requirement: 手工水格行定义全部允许水道行

系统 MUST 扫描 `WaterInteriorTilemap` 的手工水格包围范围。模拟高度 MUST 严格等于该包围范围的高度；GroundTilemap、墙体和移动障碍 MUST NOT 扩大模拟高度。当前场景两行手工水格必须只产生两条水道。

#### Scenario: 两行手工起始水道

- **WHEN** 场景只在两个相邻行中铺设手工水格
- **THEN** PlayMode 中水只能占据这两个行
- **AND** 上方与下方行始终不生成水格

### Requirement: 水在每条水道行内逐格向右流动

每个流动步 MUST 让当前已占水格最多向右扩展一格。每一行 MUST 独立推进；水不得因另一行的阻挡改变自身行坐标。

#### Scenario: 两条水道同时推进

- **WHEN** 两条手工水道右侧都有可用地面覆盖且没有阻挡
- **THEN** 两条水道在每个流动步分别向右新增一格

### Requirement: 阻挡只停止当前行水平流动

SideWallTilemap 或指定移动 Collider2D 覆盖右侧格时，水 MUST 在当前行停止。系统 MUST NOT 向上一行蓄积、绕过障碍顶部或创建第三条水道。阻挡移开后，水 MUST 在后续流动步继续沿原行向右扩展。

#### Scenario: 障碍物阻挡其中一条水道

- **WHEN** 障碍物覆盖一条水道的右侧候选格
- **THEN** 该行停止推进
- **AND** 不在其上方或下方生成水格

#### Scenario: 移开障碍物

- **WHEN** 障碍物离开原阻挡格
- **THEN** 水在后续流动步继续沿原水道行向右推进

### Requirement: 地面只限定横向长度

GroundTilemap MUST 用于确定水道可向右延伸到的最远列，并校验候选格下方存在地面支撑。GroundTilemap 的最高行 MUST NOT 用于增加水道模拟高度。

#### Scenario: 地面高于水道

- **WHEN** GroundTilemap 在水道上方存在 Tile
- **THEN** 该 Tile 不会使模拟区域增加新水道行

### Requirement: 水纹和阻尼跟随实际占据格

系统 MUST 使用既有 `Wave.asset` 显示手工源格和水平扩展格。`WaterDampingArea` MUST 只对当前实际占水格内的 Rigidbody2D 施加速度阻尼，不得施加水平推力或浮力。

#### Scenario: 候选范围内的水格与干格

- **WHEN** 两个刚体都在阻尼 Trigger 中，但只有一个位于当前实际占水格
- **THEN** 只有水格内刚体速度衰减
