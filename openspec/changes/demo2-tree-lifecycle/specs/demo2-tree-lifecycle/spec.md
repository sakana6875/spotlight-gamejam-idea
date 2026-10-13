## Purpose

定义 Demo2 树苗从引水触发、待成长、受攻击计时到结算成长的状态和可观察行为。

## ADDED Requirements

### Requirement: 树生命周期转换明确

树 MUST 依次使用 Dormant、Sprouting、AwaitingGrowth、Growing、Mature 五种状态。只允许 Dormant → Sprouting → AwaitingGrowth → Growing → Mature。非法、重复或逆向转换 MUST 返回明确失败且不得重复发布事实事件。

水到达前，Dormant 树 MUST 显示初始视觉，但其 Collider2D MUST 保持关闭。水首次触发 Sprouting 时才启用碰撞体并播放发芽动画。

#### Scenario: 正常成长路径

- **WHEN** 水触发发芽、嫩芽动画完成、外部开始成长、成长动画完成
- **THEN** 树依次进入 Sprouting、AwaitingGrowth、Growing、Mature

#### Scenario: 刷怪期间请求自动长大

- **WHEN** 树处于 AwaitingGrowth 且外部未调用 BeginGrowth
- **THEN** 树保持待长大状态，不因时间经过或怪物生成自动进入 Growing

### Requirement: 水到中心只触发一次发芽

水触发组件 MUST 查询指定中心检测点是否被 `TerrainWaterController` 当前水格占据。首次到水时 MUST 请求树进入 Sprouting；后续持续有水或重复检测 MUST NOT 重播发芽。

#### Scenario: 水持续覆盖中心

- **WHEN** 水首次到达中心并持续停留
- **THEN** 发芽流程只开始一次

### Requirement: 嫩芽结束后进入待长大状态

Sprouting 占位动画完成后，树 MUST 进入 AwaitingGrowth 并发布一次 `SproutingCompleted`。该状态允许怪物攻击计时，但 MUST NOT播放最终成长动画。

#### Scenario: 刷怪波次进行中

- **WHEN** 树处于 AwaitingGrowth 且怪物尚未全部死亡
- **THEN** 树保持树苗外观和 AwaitingGrowth 状态

### Requirement: 多个攻击者只累计一份啃食时间

树 MUST 维护当前活跃攻击者集合。集合非空且树处于 AwaitingGrowth 时，啃食时间按每秒一秒累计；两个或更多攻击者同时攻击不得加速累计。集合为空、玩法暂停或树处于其他状态时不得累计。

#### Scenario: 多只怪物同时攻击

- **WHEN** 三只怪物同时攻击树5秒
- **THEN** 累计啃食时间增加5秒，而不是15秒

#### Scenario: 攻击中断

- **WHEN** 所有怪物停止攻击3秒
- **THEN** 这3秒不增加累计啃食时间

### Requirement: 20秒阈值只通知一次

累计啃食达到20秒时，树 MUST 发布一次 `AttackLimitReached`。继续攻击、攻击者切换或重复登记不得再次发布。树本身 MUST NOT 打开对话、指责玩家或执行通关。

#### Scenario: 超过阈值继续攻击

- **WHEN** 累计啃食从20秒继续增加到25秒
- **THEN** `AttackLimitReached` 总共只发布一次

### Requirement: 最终成长由外部结算命令触发

只有 AwaitingGrowth 状态允许 `BeginGrowth()`。外部关卡控制器在怪物全部死亡并打开结算对话时调用该命令。Growing 动画 MUST 使用非缩放时间播放4帧；完成后进入 Mature 并发布一次 `GrowthCompleted`。

#### Scenario: 对话冻结玩法期间成长

- **WHEN** Time.timeScale 为0且树处于 Growing
- **THEN** 4帧成长动画仍继续播放并最终进入 Mature

### Requirement: 占位视觉可替换

占位 Prefab MAY 使用现有 Square Sprite 表达状态，但状态控制器 MUST 通过明确 Sprite/帧配置驱动视觉，不得依赖资源名称查找。替换正式嫩芽和4帧成长 Sprite 时不得修改树状态规则。

#### Scenario: 替换正式美术

- **WHEN** 开发者在 Prefab 中替换占位 Sprite 配置
- **THEN** 水触发、啃食计时和成长状态行为保持不变
