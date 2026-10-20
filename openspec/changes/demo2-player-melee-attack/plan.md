## 实施顺序

1. 为 `Demo2PlayerStateMachine` 增加轻量Strike推进结果，在Attack1/Attack2各自首次跨过0.25秒时返回一次事实，并保持现有动作时长、二击排队和冷却不变。
2. 扩展状态机EditMode行为测试，覆盖两段精确边界、低帧率跨越、单段一次、命中前取消和受击中断。
3. 新增 `Demo2PlayerMeleeAttack`，以显式 `AttackPoint` 子节点为中心实现默认1.5×0.8的可复用OverlapBox查询、怪物去重、每只10点伤害和SideWall遮挡；尺寸可在Inspector调整。
4. 让 `PlayerMoveController` 消费Strike结果并调用近战组件；全局冻结继续通过不调用Advance自然暂停。
5. 在TagManager新增Monster Layer，将SlimeMonster Prefab根Collider迁移到该层，保持Physics2D碰撞矩阵不变。
6. 通过Unity Editor只给Demo2场景Player增加默认局部位置 `(0.9, 0, 0)` 的AttackPoint子节点、近战组件和引用，保留当前刷怪装置和其他场景接线。
7. 增加PlayMode行为验证，覆盖左右范围、全部目标、多Collider去重、墙体遮挡、范围外目标和三次10点伤害死亡。
8. 编译生产、EditMode和PlayMode程序集；按用户许可执行目标测试，并完成实际场景烟测。

## 文件范围

```text
Assets/Scripts/Demos/Demo2/Player/Demo2PlayerStateMachine.cs
Assets/Scripts/Demos/Demo2/Player/PlayerMoveController.cs
Assets/Scripts/Demos/Demo2/Player/Demo2PlayerMeleeAttack.cs              新增
Assets/Scripts/Demos/Demo2/Player/Demo2PlayerMeleeAttack.cs.meta         新增
Assets/Tests/EditMode/Demo2PlayerStateMachineTests.cs
Assets/Tests/PlayMode/Demo2PlayerMeleeAttackTests.cs                     新增
Assets/Tests/PlayMode/Demo2PlayerMeleeAttackTests.cs.meta                新增
Assets/Prefabs/Demo2/SlimeMonster.prefab
Assets/Scenes/demo2.unity
ProjectSettings/TagManager.asset
openspec/changes/demo2-player-melee-attack/**
```

不修改Animator Controller、攻击AnimationClip、Input Actions、怪物生命规则、刷怪装置或Physics2D碰撞矩阵。

## 验证路径

- EditMode：状态推进结果在0.25秒边界只报告一次，并在取消/受击前不报告。
- PlayMode：真实Physics2D查询命中左右与多目标，对同怪物多Collider去重，SideWall遮挡，范围外不命中，三次命中触发现有死亡流程。
- Editor：选中Player核对AttackPoint位置和Gizmo尺寸与剑视觉；实际触发刷怪后验证左右攻击、二段各10点、墙后不受伤和三次命中死亡。
- 如仍执行“暂不运行测试”的既有要求，则只完成程序集编译并把自动测试标记为未执行，不伪造通过结果。
