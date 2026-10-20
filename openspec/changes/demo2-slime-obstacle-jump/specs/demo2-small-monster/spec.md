## ADDED Requirements

### Requirement: 移动中的史莱姆自动跳过前方实体障碍

处于 `Moving` 的史莱姆在身体已接地、落地重试间隔已经结束且朝树方向的前方检测首次命中实体非Trigger Collider时 MUST 自动起跳。地面接触 MUST NOT被当作前方障碍；史莱姆自身和当前目标树 MUST NOT触发跳跃。除这些例外外，箱子、玩家、其他怪物、SideWall及其他前方实体Collider均 MUST 允许触发跳跃尝试。

#### Scenario: 推动箱挡住去路

- **WHEN** 接地且可重试的Moving史莱姆朝树移动，前方检测命中推动箱
- **THEN** 史莱姆立即获得一次向上起跳速度并继续朝树移动

#### Scenario: 目标树位于前方

- **WHEN** 前方检测命中的Collider属于当前目标树
- **THEN** 史莱姆不得因目标树起跳，既有接近和啃食规则继续决定行为

#### Scenario: 地面接触

- **WHEN** 史莱姆站立在Ground、箱体顶部或其他实体表面
- **THEN** 支撑接触只用于确认接地，不触发起跳

### Requirement: 跳跃高度和空中水平移动保持确定

史莱姆默认跳跃高度 MUST 为3.5米，并允许通过怪物配置资产调整为有限正数。系统 MUST 根据当前Physics2D重力和史莱姆Rigidbody2D gravityScale计算向上起跳速度，不得每帧直接改写竖直位置。腾空期间若状态仍为 `Moving`，史莱姆 MUST 继续以既有MoveSpeed朝树保持水平速度；竖直速度 MUST 继续由Physics2D重力更新。

#### Scenario: 越过1米宽推动箱

- **WHEN** 史莱姆以默认3.5米高度起跳越过当前1米宽、1米高推动箱
- **THEN** 空中水平速度继续指向树，竖直速度先上升后受重力下降

#### Scenario: 空中受到伤害

- **WHEN** 腾空中的Moving史莱姆进入HitStun
- **THEN** 水平速度立即归零，现有竖直速度和重力保留，且硬直期间不得再次起跳

### Requirement: 落地后执行0.3秒重试间隔

一次跳跃后的首次落地 MUST 开始0.3秒缩放时间重试间隔；间隔可通过怪物配置资产调整为有限非负数。间隔未结束时，即使前方仍存在实体Collider也 MUST NOT再次起跳。间隔结束后仍处于Moving、接地且受阻时 MUST 允许再次尝试。首次生成后正常落地不视为跳跃重试，不应无条件延迟首次障碍跳跃。

#### Scenario: 无法越过高墙

- **WHEN** 史莱姆一次跳跃后仍落回SideWall前方
- **THEN** 落地后等待0.3秒，再允许对同一SideWall发起下一次跳跃尝试

#### Scenario: 落地冷却期间受阻

- **WHEN** 史莱姆落地不足0.3秒且前方仍有障碍
- **THEN** 保持Moving水平规则但不施加新的向上速度

### Requirement: 非移动状态和冻结期间禁止起跳

`Biting`、`HitStun`、`Dead` 状态、全局玩法冻结、GameObject停用或Rigidbody2D停止模拟期间 MUST 不执行障碍查询产生的新跳跃。跳跃不得新增独立Animator状态；起跳、腾空和落地期间继续使用当前 `Moving` 表现。切换俯视停用SideView时 MUST 保留现有生命和状态规则，返回侧视后由物理接触重新确认接地。

#### Scenario: 啃食期间前方有Collider

- **WHEN** 史莱姆处于Biting且前方检测范围内存在实体Collider
- **THEN** 不起跳并继续执行既有啃食登记

#### Scenario: 玩法冻结

- **WHEN** 史莱姆移动或腾空期间全局玩法被冻结
- **THEN** 不产生新跳跃，Unity缩放时间冻结现有物理轨迹，解除后继续
