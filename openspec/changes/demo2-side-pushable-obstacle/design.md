# Demo2 SideView 可推动障碍物设计

## 设计目标

把 `Push` 收敛为一个可验证的 SideView 物理动作，而不是提前建立通用交互框架。障碍物继续作为普通 Unity 2D 物理对象；如果进入手工铺设的水格，则按既有阻尼合同减速。

## 组件职责

### `Demo2PushableObstacle`

- 挂在可推动障碍物根节点。
- 要求同一对象拥有 Dynamic `Rigidbody2D` 和 `Collider2D`。
- 保存推动力和最大水平速度等场景调参。
- 只提供“沿指定水平朝向施加一次推动力”的局部行为。
- 不读取输入，不查找玩家，不查找水流控制器。

### `Demo2PushController`

- 挂在 Player，与已有 `PlayerMoveController` 和 Collider2D 组合。
- 持有自己的 `GameInputActions` 生命周期，仅读取 `Demo2/Push`。
- 在 `FixedUpdate` 中检查 SideView 和按住状态。
- 使用玩家 Collider2D 的短距离 Cast 识别前方的 `Demo2PushableObstacle`。
- 使用预分配命中数组，避免物理步临时分配。
- 只消费 `PlayerMoveController` 的只读朝向，不维护第二份朝向状态。

## 物理选择

采用 `Rigidbody2D.AddForce(..., ForceMode2D.Force)`，而不是直接设置 Transform、`MovePosition` 或关闭碰撞。这样可以保留墙体碰撞、质量和现有 `WaterDampingArea` 的阻尼效果。为了防止持续按键导致障碍物无限加速，组件限制水平速度；Y 速度不被推动逻辑修改。

## 场景接线

Demo2 场景将障碍物 Collider2D 显式配置为水道移动阻挡。障碍物只停止当前行水平流动，移开后原行继续推进，不生成额外水道行。

## 测试边界

推动行为依赖 Physics2D、InputAction 和 Rigidbody2D，因此使用 PlayMode 行为验证。验证观察刚体位置、速度和碰撞结果，不测试私有字段、组件调用次数或 Unity 序列化细节。
