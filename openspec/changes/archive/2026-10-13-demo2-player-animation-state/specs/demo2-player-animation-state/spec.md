## Purpose

定义 Demo2 玩家移动、二段攻击、受击硬直与 Animator 表现之间的稳定行为合同，使输入许可、动作计时和动画播放保持一致，并为后续伤害与命中系统提供明确状态边界。

## ADDED Requirements

### Requirement: 玩家状态是动画表现的唯一事实来源

系统 MUST 维护 `Idle`、`Moving`、`Attack1`、`Attack2` 和 `HitStun` 五种互斥玩家状态。Animator MUST 通过一个整数 `State` 参数消费当前状态，并使用可见 Transition 播放对应动画；Animator、Animation Event 和 Clip 播放进度 MUST NOT 反向决定玩法状态、攻击间隔或输入许可。参数值 MUST 稳定映射为 `0=Idle`、`1=Moving`、`2=Attack1`、`3=Attack2`、`4=HitStun`。

#### Scenario: 移动事实驱动动画

- **WHEN** 玩家处于可移动状态且侧视存在水平输入，或俯视存在合法轴向输入
- **THEN** 玩家状态为 `Moving`，Animator 的 `State` 为 `1` 并播放 `PlayerRun`

#### Scenario: 停止输入恢复待机

- **WHEN** 玩家处于可移动状态且当前没有合法移动输入
- **THEN** 玩家状态为 `Idle`，Animator 的 `State` 为 `0` 并播放 `PlayerIdle`

### Requirement: 第一段攻击只在合法起手条件下开始

系统 MUST 在侧视或俯视模式下，只要玩家处于 `Idle` 或 `Moving`、全局玩法未冻结且当前攻击间隔已经结束，就接受攻击输入并立即进入 `Attack1`。`Attack1`、`Attack2` 或 `HitStun` 中的普通起手输入 MUST NOT 开始新的第一段攻击。玩家在空中时仍 MUST 允许合法起手攻击。俯视模式 MUST 复用现有 `PlayerAttack1` 和 `PlayerAttack2`，攻击期间平面速度 MUST 为零；在俯视上下移动后攻击时，视觉朝向暂时保持最近一次左右朝向。

#### Scenario: 空中侧视攻击

- **WHEN** 玩家在侧视空中、攻击间隔已结束且处于 `Idle` 或 `Moving` 时按下攻击
- **THEN** 玩家立即进入 `Attack1`，同时保留现有竖直速度和重力

#### Scenario: 俯视攻击复用现有动画

- **WHEN** 玩家在俯视模式、攻击间隔已结束且处于 `Idle` 或 `Moving` 时按下攻击
- **THEN** 玩家立即进入 `Attack1`、停止俯视平面移动，并播放现有侧身攻击动画

### Requirement: 第二段攻击只接受后半段排队输入

`Attack1` 动作时长 MUST 与当前 Clip 一致为 `31/60` 秒。系统 MUST 仅在 `Attack1` 已经过 `0.25` 秒且尚未结束时接受一次第二次攻击输入作为排队；排队成功后 MUST 在 `Attack1` 结束时立即进入 `Attack2`。窗口开始前的点击 MUST NOT 排队，`Attack2` 中的点击 MUST NOT产生第三段攻击。

#### Scenario: 后半段点击进入第二段

- **WHEN** 玩家在 `Attack1` 开始后 `0.25` 秒至结束前再次按下攻击
- **THEN** 系统记录一次第二段排队，并在 `Attack1` 结束时立即进入 `Attack2`

#### Scenario: 过早点击不进入第二段

- **WHEN** 玩家只在 `Attack1` 开始后不足 `0.25` 秒时再次按下攻击
- **THEN** 系统不排队 `Attack2`，并在 `Attack1` 结束后回到当前移动事实对应的 `Idle` 或 `Moving`

### Requirement: 每段攻击分别执行0.8秒攻击间隔

进入 `Attack1` 或 `Attack2` 时，系统 MUST 从该段开始重新计算 `0.8` 秒攻击间隔。单独完成 `Attack1` 后，系统 MUST 在第一段开始满 `0.8` 秒前拒绝新的起手；完成 `Attack2` 后，系统 MUST 在第二段开始满 `0.8` 秒前拒绝新的起手。

#### Scenario: 第一段结束但间隔未完成

- **WHEN** 未排队第二段的 `Attack1` 已结束，但距离第一段开始不足 `0.8` 秒
- **THEN** 玩家恢复 `Idle` 或 `Moving`，期间新的攻击输入被拒绝

#### Scenario: 第二段重置攻击间隔

- **WHEN** 玩家从 `Attack1` 进入 `Attack2`
- **THEN** 攻击间隔从 `Attack2` 开始重新计算 `0.8` 秒

### Requirement: 攻击和受击锁定互斥操作但保留侧视重力

`Attack1`、`Attack2` 和 `HitStun` 期间，系统 MUST 禁止水平移动、跳跃起跳和推动石头。侧视 Rigidbody2D 已有的竖直速度和重力 MUST 继续生效，系统 MUST NOT 将空中玩家悬停。俯视速度在这些状态中 MUST 为零。

#### Scenario: 空中攻击继续下落

- **WHEN** 玩家带有竖直速度进入 `Attack1` 或 `Attack2`
- **THEN** 水平控制被清零，新的跳跃输入被忽略，而竖直速度继续受重力更新

#### Scenario: 攻击期间尝试推动

- **WHEN** 玩家处于 `Attack1`、`Attack2` 或 `HitStun` 并按住推动输入
- **THEN** 推动请求失败且石头不接收玩家推力

### Requirement: 受击中断当前动作并持续0.5秒

外部系统成功请求受击时，系统 MUST 立即进入 `HitStun`、取消未开始的第二段攻击并播放 `PlayerHitted`。`HitStun` MUST 持续 `0.5` 秒；期间重复受击请求 MUST 被拒绝且不得重播动画或刷新时长。硬直结束后 MUST 根据当前移动输入进入 `Idle` 或 `Moving`。

#### Scenario: 受击中断已排队连击

- **WHEN** 玩家在已排队 `Attack2` 的 `Attack1` 中收到首个受击请求
- **THEN** 玩家立即进入 `HitStun`，排队的 `Attack2` 被取消

#### Scenario: 硬直期间重复受击

- **WHEN** 玩家已处于 `HitStun` 时再次收到受击请求
- **THEN** 请求失败，既有硬直结束时间和动画播放不变

### Requirement: 全局冻结暂停计时而视角切换取消动作

全局玩法冻结期间，玩家状态计时、攻击间隔和 Animator 播放 MUST 同时暂停。玩家在攻击或受击期间请求切换视角时，系统 MUST 允许既有视角转场开始，但 MUST 在转场开始前取消当前动作和第二段排队并恢复 `Idle`；转场结束后不得恢复已取消动作。

#### Scenario: 对话暂停攻击

- **WHEN** 对话在 `Attack1` 中冻结玩法并随后解除冻结
- **THEN** `Attack1` 从冻结前进度继续，冻结时长不计入动作时长或攻击间隔

#### Scenario: 攻击期间切换视角

- **WHEN** 玩家在 `Attack1`、`Attack2` 或 `HitStun` 中触发合法视角切换
- **THEN** 当前动作立即取消为 `Idle`，排队清除，之后执行既有视角转场

### Requirement: 循环设置符合动作语义

`PlayerIdle` 和 `PlayerRun` MUST 循环播放。`PlayerAttack1`、`PlayerAttack2` 和 `PlayerHitted` MUST NOT 循环；非循环 Clip 播放结束后在代码状态改变前可以保持末帧，但 MUST NOT自行触发玩法转换。

#### Scenario: 攻击片段不重复播放

- **WHEN** `Attack1`、`Attack2` 或 `HitStun` 状态持续到对应 Clip 的末帧
- **THEN** 对应 Clip 不从首帧重新循环，后续状态仍由代码计时决定
