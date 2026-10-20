## Why

Demo2 玩家已经有可测试的两段攻击状态和怪物伤害入口，但攻击动画目前不会产生命中，玩家无法阻止史莱姆啃食树苗。需要建立与代码状态计时一致的近战判定，使剑击范围、伤害、遮挡和单段命中次数可观察且不依赖动画事件或逐帧碰撞体。

## What Changes

- 为每段玩家攻击增加一次由状态计时产生的命中事实；`Attack1` 和 `Attack2` 均在开始后0.25秒结算。
- 新增独立的玩家近战适配组件，使用无分配 `Physics2D.OverlapBox` 查询朝向前方的怪物，不改变玩家身体碰撞体。
- 攻击区域默认宽1.5米、高0.8米，中心由Player子节点 `AttackPoint` 决定；默认局部位置为向右0.9米，玩家翻面时随层级镜像，尺寸可在Inspector调整。
- 每段攻击对范围内每只存活怪物最多造成一次10点伤害，多Collider归属同一怪物时去重。
- 使用专用 `Monster` Layer筛选目标；`SideWall` 位于攻击起点与怪物之间时阻挡伤害。
- 全局冻结暂停命中计时；受击或视角切换在命中时刻前取消攻击时不得产生延迟命中。
- 增加选中玩家时的攻击框 Gizmo，以及命中时序、朝向、范围、多目标、去重、遮挡和取消边界验证。

## Capabilities

### New Capabilities

- `demo2-player-melee-attack`: 定义Demo2玩家两段近战攻击的代码计时、空间查询、伤害、目标去重和墙体遮挡合同。

### Modified Capabilities

- `demo2-player-animation-state`: 明确既有 `Attack1`、`Attack2` 状态在0.25秒产生一次命中事实，并保持Animator不反向决定伤害。

## Impact

- 修改 `Demo2PlayerStateMachine`，让推进结果明确报告每段一次性命中时刻。
- 修改 `PlayerMoveController`，把状态推进结果交给近战组件；朝向由Player翻面后的 `AttackPoint` 世界位置表达。
- 新增 `Demo2PlayerMeleeAttack` 运行时组件及对应 `.meta`。
- 修改 `TagManager.asset` 增加 `Monster` Layer，并将 `SlimeMonster.prefab` 根对象设置到该层。
- 修改 Demo2 Player 场景实例，新增 `AttackPoint` 子节点并连接近战组件、怪物层和 `SideWall` 遮挡层。
- 复用现有 `IMonster.TryTakeDamage(int)`、史莱姆受击闪白、硬直和死亡流程；不新增程序集、Unity包或第三方依赖。

## 非目标

- 不给剑或玩家身体增加逐帧 `Collider2D`，不使用 Trigger 回调或 Animation Event 结算伤害。
- 不实现玩家生命、怪物接触伤害、击退、暴击、属性、武器切换、攻击音效、粒子、屏幕震动或掉落。
- 不修改现有二段输入窗口、0.8秒攻击间隔、攻击动画或移动锁定规则。
- 不实现俯视专用攻击方向或新动画；俯视继续使用最近一次左右朝向。
- 不建立通用战斗框架、通用伤害基类、对象池或可配置连击树。
