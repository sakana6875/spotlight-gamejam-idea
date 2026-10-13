# Demo2 SideView 可推动障碍物设计

## 设计目标

把 `Push` 收敛为严格授权的水平动作，同时保留箱子作为 Dynamic Rigidbody2D 的重力掉落、墙体碰撞和水体阻尼。

## 组件职责

### `Demo2PushableObstacle`

- 挂在可推动障碍物根节点。
- 要求同一对象拥有 Dynamic `Rigidbody2D` 和 `Collider2D`。
- 保存1米/秒推动速度、未授权约束和授权约束。
- 接收当前物理步的玩家推动方向，并在自身 FixedUpdate 消费一次。
- 不读取输入，不查找玩家、怪物或水流控制器。

### `Demo2PushController`

- 挂在 Player，与已有 `PlayerMoveController` 和 Collider2D 组合。
- 持有自己的 `GameInputActions` 生命周期，仅读取 `Demo2/Push`。
- 在 `FixedUpdate` 中检查 SideView、玩家状态和按住 `F`。
- 使用玩家 Collider2D 的短距离 Cast 识别前方的 `Demo2PushableObstacle`。
- 只有满足上述条件时才向障碍物登记当前物理步的推动方向。
- 只消费 `PlayerMoveController` 的只读朝向，不维护第二份朝向状态。

## 物理选择

障碍物保持 Dynamic Rigidbody2D，并使用晚于玩家控制器的 FixedUpdate 消费授权。无请求时应用 `原Inspector约束 | FreezePositionX` 并清零X速度；有请求时应用 `原Inspector约束 & ~FreezePositionX`，把X速度设为方向乘1米/秒，同时原样保留Y速度。每步消费后清除请求，因此松开F后的下一物理步会重新锁定。玩家和怪物普通碰撞没有请求，不能解除X锁定；重力和水体阻尼仍作用于Y速度及授权期间的动态刚体。

## 场景接线

Demo2 场景障碍物继续使用 Dynamic Rigidbody2D 和原有 Collider2D，不需要改 Body Type；项目负责人只需确认 Inspector 显示推动速度为1。

## 测试边界

推动行为依赖 Physics2D、InputAction 和 Rigidbody2D，因此使用 PlayMode 行为验证。验证必须覆盖未按F的玩家碰撞、怪物碰撞、Y方向重力掉落、持续按F的1米/秒目标速度、松开重新锁定和墙体阻挡。
