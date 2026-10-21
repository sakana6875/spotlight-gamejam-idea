## Purpose

定义Demo2玩家两段近战攻击从状态计时到空间命中和怪物伤害的可观察合同，使攻击范围、单段次数、多目标与墙体遮挡在侧视和俯视规则下保持确定一致。

## ADDED Requirements

### Requirement: 每段攻击在确定时刻结算一次

`Attack1` 与 `Attack2` MUST 各自在进入该状态后经过0.25秒时产生一次且仅一次命中结算。命中时刻 MUST 由玩家代码状态计时决定，不得由Animator状态、Animation Event、Sprite帧、`OnTrigger`或`OnCollision`回调决定。单帧时间增量跨过0.25秒时仍 MUST 结算；同段后续帧 MUST NOT重复结算。

#### Scenario: 第一段跨过命中时刻

- **WHEN** `Attack1` 上一帧进度小于0.25秒且本帧推进后达到或超过0.25秒
- **THEN** 系统在本帧产生一次第一段命中结算

#### Scenario: 第二段只结算一次

- **WHEN** `Attack2` 已经产生过命中结算且状态继续推进
- **THEN** 系统不得再次产生该段命中结算

### Requirement: 攻击区域使用可调AttackPoint和尺寸

每次命中结算 MUST 查询由场景配置给出的矩形区域。Player MUST 提供子节点 `AttackPoint` 作为区域中心；其默认局部位置 MUST 为 `(0.9, 0, 0)`，并随Player左右翻面自动镜像。区域默认宽1.5米、高0.8米，宽高 MUST 允许在Inspector调整且必须为有限正数。俯视模式 MUST 继续使用玩家最近一次左右朝向，因此只做上下移动不得改变Player水平翻面或AttackPoint所在侧。攻击查询不得改变玩家身体碰撞体。

#### Scenario: 默认向右攻击

- **WHEN** Player未翻面、AttackPoint保持默认局部位置且一段攻击到达命中时刻
- **THEN** 系统查询以Player右侧0.9米AttackPoint为中心的1.5米×0.8米区域

#### Scenario: 翻面后AttackPoint镜像

- **WHEN** 玩家向左翻面后攻击
- **THEN** AttackPoint随Player层级移动到左侧，攻击区域以镜像后的世界位置为中心且宽高不变

#### Scenario: Inspector调整尺寸

- **WHEN** 场景将攻击区域配置为合法的其他宽高
- **THEN** 运行时查询和Scene Gizmo都使用该配置尺寸

#### Scenario: 俯视上下移动后攻击

- **WHEN** 玩家最后稳定水平朝向为左，随后在俯视模式只做上下移动并攻击
- **THEN** AttackPoint和攻击区域仍位于玩家左侧

### Requirement: 范围内所有怪物各受到10点伤害

系统 MUST 只把专用 `Monster` Layer中的存活怪物作为攻击目标。命中区域内每只未被遮挡的存活怪物 MUST 接收10点伤害；同一怪物即使被多个Collider命中，在同一攻击段内也 MUST 最多接收一次伤害。一次查询中的多只合法怪物 MUST 全部独立结算。已死亡目标和不属于怪物合同的Collider MUST NOT接收伤害。

#### Scenario: 一段命中两只怪物

- **WHEN** 两只存活怪物同时位于攻击区域内且均未被墙遮挡
- **THEN** 两只怪物各减少10点生命

#### Scenario: 怪物具有多个Collider

- **WHEN** 同一怪物的两个Collider同时落入同一次攻击区域
- **THEN** 该怪物只减少10点生命

#### Scenario: 三次单段命中击杀史莱姆

- **WHEN** 30点满生命史莱姆先后受到三次合法攻击段命中
- **THEN** 前两次分别进入受击硬直且剩余20点、10点生命，第三次进入死亡状态

### Requirement: SideWall阻挡剑击

对每个范围内候选怪物，系统 MUST 检查玩家攻击起点到怪物中心之间的 `SideWall` Layer。路径被SideWall命中时，该怪物本段 MUST NOT受到伤害；墙体不得阻止同次攻击中路径未被遮挡的其他怪物结算。

#### Scenario: 怪物位于侧墙后

- **WHEN** 怪物身体位于攻击区域内，但玩家与怪物中心之间存在SideWall Collider
- **THEN** 怪物生命不变且不进入受击状态

#### Scenario: 同段包含遮挡与未遮挡目标

- **WHEN** 同次攻击区域内一只怪物被SideWall遮挡，另一只路径无遮挡
- **THEN** 只对无遮挡怪物造成10点伤害

### Requirement: 暂停和动作取消不得产生错误命中

全局玩法冻结期间 MUST 不推进0.25秒命中计时，也不得执行攻击查询；解除冻结后 MUST 从原攻击进度继续。玩家在命中时刻前因受击或合法视角转场离开攻击状态时，该段 MUST 被取消且不得产生延迟命中。已经完成的命中不得因随后取消动作而回滚或重复。

#### Scenario: 命中前视角切换

- **WHEN** 玩家在攻击开始0.25秒前切换视角并取消动作
- **THEN** 被取消的攻击不查询目标、不造成伤害

#### Scenario: 冻结跨越真实时间

- **WHEN** 玩家在攻击开始0.2秒时进入全局冻结并在真实时间1秒后解除
- **THEN** 冻结期间不命中，解除后仍需推进0.05秒状态时间才结算
