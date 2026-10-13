# Demo2 树苗生命周期技术设计

## 组件划分

### `Demo2TreeStateMachine`

纯 C# 状态和计时规则：

- 当前 `Demo2TreePhase`；
- 累计啃食秒数；
- 20秒阈值是否已经触发；
- BeginSprouting、CompleteSprouting、BeginGrowth、CompleteGrowth；
- AdvanceBiteTime(deltaSeconds, isUnderAttack)。

它不引用 Unity 对象、Animator、对话或怪物类型。

### `Demo2TreeController`

MonoBehaviour 外部适配：

- 持有状态机；
- 使用集合登记活跃攻击者；
- 在 Update 中以 `Time.deltaTime` 推进啃食计时；
- 使用 SpriteRenderer 和帧配置播放占位动画；
- Sprouting/Growing 动画使用 `Time.unscaledDeltaTime`；
- 发布 `SproutingCompleted`、`AttackLimitReached`、`GrowthCompleted` 事实事件。

公开命令返回 bool，拒绝非法状态，不用异常表达正常业务分支。

### `Demo2TreeWaterTrigger`

- 显式引用 TerrainWaterController、中心检测点和 Demo2TreeController；
- FixedUpdate 查询中心点是否被水占据；
- 成功触发一次后停止继续请求；
- 不控制相机、对话或怪物。

## 攻击者登记

树控制器提供 RegisterAttacker/UnregisterAttacker，以 Unity Object 实例作为集合键。重复登记同一攻击者不增加集合数量；攻击者销毁时由怪物攻击适配器解除。树禁用时清空集合。

## 占位动画

Prefab 使用现有 `Assets/Resources/Sprites/Square.png`：

- Dormant：显示初始占位 Sprite，使用较小缩放，Collider2D 关闭；
- Sprouting：从初始缩放播放到树苗缩放，并启用 Collider2D；
- AwaitingGrowth：保持树苗占位外观；
- Growing：按4个配置帧间隔播放；
- Mature：保持最终占位外观。

正式资源到位后，将视觉配置替换为 Sprite 数组或 Animator 资产，状态机无需迁移。

## 后续 Act0 接入

Act0Controller 订阅 SproutingCompleted 后播放怪物介绍对话；对话完成后启动8只怪物波次。全部怪物死亡时先打开结算对话，再调用 BeginGrowth。AttackLimitReached 只作为强制过关分支输入。
