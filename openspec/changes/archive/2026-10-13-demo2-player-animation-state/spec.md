# Demo2 玩家动画状态机总规格

本 Change 建立 Demo2 玩家移动、二段攻击、受击硬直与 Animator 表现的统一合同。完整可观察行为以 `specs/demo2-player-animation-state/spec.md` 为准。

## 状态不变量

- 玩家同一时刻只处于 `Idle`、`Moving`、`Attack1`、`Attack2` 或 `HitStun` 之一。
- 代码状态模型决定合法转换、动作计时、攻击间隔和输入许可；Animator 只消费 `State` 参数。
- Animator 保留可视化 Transition，不使用代码直接播放 Clip 绕过状态图。
- 攻击和受击不改变玩家身体碰撞体，也不包含剑命中或伤害结算。
- 空中攻击保留竖直物理，攻击和受击期间禁止水平控制、跳跃起跳和推动。
- 视角切换取消当前攻击或受击；普通全局冻结只暂停并在解除后继续。
