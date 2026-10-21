## Purpose

定义 Demo2 两条手工起始水道只在原行内逐格向右扩展，禁止跨行蓄积形成额外水道。

## MODIFIED Requirements

### Requirement: 手工源格行限定水道高度

系统 MUST 将手工水格包围范围的行数作为固定模拟高度。GroundTilemap MUST 只限定横向范围，不得增加模型高度。

#### Scenario: 场景存在两行手工源水

- **WHEN** Demo2 进入 PlayMode
- **THEN** 水只能占据这两行，不得生成第三行

### Requirement: 水只在当前行向右扩展

每个流动步 MUST 最多向右扩展一格。右侧受阻时当前行 MUST 停止，系统 MUST NOT 向上蓄积或越过障碍顶端。阻挡移开后，水 MAY 继续沿原行向右扩展。

#### Scenario: 有限高度障碍

- **WHEN** 水道右侧存在障碍
- **THEN** 水停在原行且不向其他行生成水格

### Requirement: 水纹和阻尼

系统 MUST 使用既有 Wave AnimatedTile 显示源格和水平扩展格，并只对实际占水格内刚体施加速度阻尼。

#### Scenario: 干格中的刚体

- **WHEN** 刚体位于 Trigger 内但所在格尚未被水占据
- **THEN** 刚体速度不受水影响

## REMOVED Requirements

### Requirement: 障碍前向上蓄积和越障

删除所有 `row + 1` 蓄积、抬高水位和越过障碍顶部的要求。
