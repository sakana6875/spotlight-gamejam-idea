# Demo2 玩家动画状态机实施计划

## 实施顺序

1. 在 `Assets/Scripts/Demos/Demo2/Player/` 增加纯 C# `Demo2PlayerStateMachine` 与显式状态枚举，先完成 Idle/Moving、两段攻击、冷却、受击和取消动作规则。
2. 在 `Assets/Tests/EditMode/` 增加后半段排队边界、每段0.8秒间隔、空中攻击、重复受击和受击中断测试。
3. 让 `PlayerMoveController` 推进状态模型、读取 Attack 输入、锁定不合法移动，并只通过 `Animator.SetInteger("State", value)` 驱动动画。
4. 让 `Demo2PushController` 拒绝攻击或受击期间的推动；让 `Demo2ViewModeController` 在开始转场前取消当前动作。
5. 在 Unity Editor 中为 `Demo2Player.controller` 创建 `State` 参数和显式 Transition，关闭攻击与受击 Clip 循环并保留 Idle/Run 循环。
6. 执行 EditMode 目标测试和生产程序集编译。
7. 在 Demo2 Play Mode 实测侧视与俯视的待机、跑动、单击、后半段二击、过早二击、空中攻击、攻击移动锁定、推动拒绝和视角切换取消。

## 目录与程序集

- `Assets/Scripts/Demos/Demo2/Player/`：玩家状态模型和现有 Unity 适配，属于 `Spotlight.Game`。
- `Assets/Scripts/Demos/Demo2/Push/`：推动许可接入，仍属于 `Spotlight.Game`。
- `Assets/Scripts/Demos/Demo2/Flow/`：视角切换取消动作，仍属于 `Spotlight.Game`。
- `Assets/Animations/Demo2/Player/`：Animator Controller 和现有 AnimationClip，不新增 Controller。
- `Assets/Tests/EditMode/`：纯状态规则行为测试，属于 `Spotlight.Tests.EditMode`。
- `Assets/Tests/PlayMode/`：只有运行时集成存在不能由实际烟测稳定覆盖的边界时才增加测试。

## 依赖方向

- 状态模型不引用 Animator、Input System、Rigidbody2D 或场景对象。
- `PlayerMoveController` 将 Input System 和 Unity 物理事实转换为状态模型输入。
- Animator、推动和视角适配器只消费状态或许可，不修改状态模型内部字段。
- 不新增程序集、接口、静态单例、EventBus 或第三方依赖。

## Animator 连线

```text
Entry -> PlayerIdle
PlayerIdle <-> PlayerRun
PlayerIdle/PlayerRun -> PlayerAttack1       条件 State == 2
PlayerAttack1 -> PlayerAttack2              条件 State == 3
PlayerAttack1/PlayerAttack2 -> Idle或Run    条件 State == 0或1
Any State -> PlayerHitted                   条件 State == 4
PlayerHitted -> Idle或Run                   条件 State == 0或1
```

所有条件 Transition 使用：

```text
Has Exit Time = false
Transition Duration = 0
Can Transition To Self = false
```

## 验证路径

- 状态规则：EditMode 测试覆盖窗口下边界、窗口外点击、两段冷却、受击中断、重复受击和动作取消。
- Animator 资源：检查参数值、Transition 条件、Motion 引用和循环设置。
- 物理：空中攻击时水平输入锁定但玩家继续下落；攻击结束后恢复控制。
- 互斥：攻击/受击时推动失败；切换视角取消当前动作并不在转场后恢复。
- 暂停：对话冻结期间状态计时和 Animator 同时停止，解除后继续。
- Unity Editor 未实际运行前，Play Mode 项目保持标记“Editor 验证未完成”。
