# Demo2 玩家近战攻击规格

本Change把既有 `Attack1`、`Attack2` 状态与怪物伤害入口连接为可观察的近战闭环。完整新增行为见 `specs/demo2-player-melee-attack/spec.md`，对既有玩家状态合同的修改见 `specs/demo2-player-animation-state/spec.md`。

## 状态不变量

- 每段攻击只在代码状态时间首次跨过0.25秒时产生一次命中事实。
- Animator、Animation Event、Sprite帧和物理Trigger不决定命中时刻。
- 攻击框不改变玩家身体Collider，也不作为持续物理对象存在。
- 同一攻击段对同一怪物最多造成一次10点伤害；范围内多个合法怪物全部结算。
- SideWall只阻挡墙后的目标，不取消同段其他目标。
- 暂停不推进命中计时；命中前取消动作不会留下延迟伤害。

## 已确认参数

```text
Attack1命中时刻  0.25秒
Attack2命中时刻  0.25秒
每段伤害          10
攻击框默认尺寸    1.5m × 0.8m（Inspector可调）
AttackPoint默认位置 (0.9, 0, 0)，随Player翻面镜像
目标层            Monster
遮挡层            SideWall
多目标规则        全部目标各一次
```
