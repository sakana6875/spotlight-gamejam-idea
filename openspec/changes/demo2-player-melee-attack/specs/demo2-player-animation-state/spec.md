## MODIFIED Requirements

### Requirement: 玩家状态是动画表现的唯一事实来源

系统 MUST 维护 `Idle`、`Moving`、`Attack1`、`Attack2` 和 `HitStun` 五种互斥玩家状态。Animator MUST 通过一个整数 `State` 参数消费当前状态，并使用可见Transition播放对应动画；Animator、Animation Event和Clip播放进度 MUST NOT反向决定玩法状态、攻击间隔、输入许可或伤害结算。参数值 MUST 稳定映射为 `0=Idle`、`1=Moving`、`2=Attack1`、`3=Attack2`、`4=HitStun`。`Attack1` 与 `Attack2` 的命中事实 MUST 由同一状态模型在各段开始后0.25秒产生一次。

#### Scenario: 移动事实驱动动画

- **WHEN** 玩家处于可移动状态且侧视存在水平输入，或俯视存在合法轴向输入
- **THEN** 玩家状态为 `Moving`，Animator的 `State` 为1并播放 `PlayerRun`

#### Scenario: 停止输入恢复待机

- **WHEN** 玩家处于可移动状态且当前没有合法移动输入
- **THEN** 玩家状态为 `Idle`，Animator的 `State` 为0并播放 `PlayerIdle`

#### Scenario: 状态计时驱动攻击命中

- **WHEN** `Attack1` 或 `Attack2` 的代码状态计时首次跨过0.25秒
- **THEN** 状态推进结果报告该段一次性命中事实，Animator和当前Sprite帧不参与判定
