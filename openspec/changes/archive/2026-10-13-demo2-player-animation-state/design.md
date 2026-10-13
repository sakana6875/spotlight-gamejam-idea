# Demo2 玩家动画状态机设计

## Context

参见 `proposal.md` 的 Why。当前 `PlayerMoveController` 已统一读取 Demo2 的移动和跳跃输入，并负责侧视/俯视 Rigidbody2D 规则；`Demo2PushController` 和 `Demo2ViewModeController` 分别处理推动与视角转场。`Demo2Player.controller` 已引用五个 Clip，但没有参数或 Transition。当前 Clip 仅覆盖待机、跑动、两段攻击和受击。

## Goals / Non-Goals

**Goals:**

- 用可独立测试的纯 C# 状态模型维护状态、计时、二段排队和动作许可。
- 让现有 Unity 适配器共享同一状态事实，避免攻击时移动、推动或视角转场发生冲突。
- 在 Animator 中保留明确参数和可视化连线，同时让玩法规则不依赖 Animator 播放进度。

**Non-Goals:**

- 不建立通用角色状态机框架、接口层或可配置连击树。
- 不把剑命中、生命、死亡和敌人状态加入当前状态模型。
- 不为缺失的跳跃、下落或俯视动画创建占位 Clip。

## Decisions

### 1. 状态规则使用纯 C# 模型

在 `Assets/Scripts/Demos/Demo2/Player/` 新增 `Demo2PlayerStateMachine`，位于 `Spotlight.Game` 程序集。模型只接收缩放时间增量、当前移动事实、攻击按下事实和外部许可，输出当前状态及 `CanMove`、`CanPush` 等明确许可。

选择纯模型而不是 `StateMachineBehaviour`：攻击窗口、冷却和受击中断可以在 EditMode 中确定性验证，并且不会受 Animator 更新模式影响。当前只有一个玩家调用方，不引入接口或通用基类。

### 2. PlayerMoveController 作为现有输入与物理适配器接入状态模型

`PlayerMoveController` 已持有 `GameInputActions`、Rigidbody2D、视角模式和移动事实，因此由它推进状态模型并设置 Animator 参数，避免再创建一套 Input Actions 和跨组件循环依赖。

攻击或受击期间，侧视 FixedUpdate 只把水平目标速度设为零并忽略跳跃请求，不覆盖竖直速度；俯视则把平面速度清零。全局冻结继续使用现有 `Demo2SimulationController`，冻结时不推进状态模型。

侧视与俯视都将 Attack 输入交给同一状态模型。俯视进入攻击状态后由既有许可把平面速度清零，并继续使用同一 `PlayerAttack1/2` Motion；由于当前没有上下攻击素材，竖直移动不会改变最近一次左右视觉朝向。

### 3. Animator 使用单一整数参数和显式连线

Animator 增加整数参数 `State`：

```text
0 Idle
1 Moving
2 Attack1
3 Attack2
4 HitStun
```

状态图使用条件为 `State == value`、`Has Exit Time = false`、`Transition Duration = 0` 的可视化 Transition。Idle 与 Moving 双向连接；合法动作路径连接到 Attack1、Attack2、HitStun，再连接回 Idle/Moving。受击使用 Any State 入口以表达中断优先级。代码只调用 `Animator.SetInteger`，不直接调用 `Play` 或 `CrossFade`。

选择单一整数而不是多个 Bool/Trigger：整数与互斥状态一一对应，不会遗留触发器或产生同时为真的状态组合。Animator 连线仍可在 Editor 中直接检查。

### 4. 动作时序由代码常量固定

当前两段攻击 Clip 的停止时间均约为 `31/60` 秒，状态模型使用同一动作时长；第二段排队窗口从 `0.25` 秒开始，到第一段结束前关闭。进入每段攻击时分别重置 `0.8` 秒攻击间隔。受击硬直固定为 `0.5` 秒，因此 `PlayerHitted` 播放结束后可保持末帧直到状态离开。

不使用 Animation Event：替换帧、采样率或 Clip 后不会静默改变玩法规则；如果未来更改动作时长，必须同时更新规格、状态常量和验证。

### 5. 推动和视角转场消费玩家许可

`Demo2PushController.TryPush` 在状态不允许推动时直接返回失败。`Demo2ViewModeController` 接受切换输入后先请求玩家取消当前动作，再沿用现有转场和全局冻结流程。取消会清空第二段排队并把 Animator 参数恢复为 Idle；普通对话冻结不取消动作。

### 6. Animator 资源通过 Unity Editor 配置

参数、Transition、Clip 循环设置优先在 Unity Editor 中修改并保存 `.controller`/`.anim`，不手工维护复杂 YAML。实现完成后检查对应 `.meta`、状态 Motion 引用和场景 Player 的 Controller 引用保持完整。

## Risks / Trade-offs

- [整数值与 Animator 条件漂移] → 在代码中显式赋值枚举，并在 Play Mode 启动时验证 `State` 参数与五个状态存在。
- [代码时长与 Clip 时长漂移] → 将当前 Clip 时长写入规格和测试，资源变更必须同步更新合同。
- [攻击中切视角成为逃逸硬直手段] → 这是本次确认行为；后续若改变必须更新规格，而不是只改转场代码。
- [空中攻击缺少专用动画] → 允许现有攻击 Clip 播放，但不新增虚假空中状态；重力和竖直速度保持正确。
- [PlayerMoveController 职责扩大] → 本次只增加对一个纯状态模型和 Animator 参数的适配；出现第二个消费者前不拆分无调用方组件。

## Migration Plan

1. 先增加并验证纯状态模型，不改变场景资源。
2. 将现有移动、推动和视角适配器接到状态许可。
3. 在 Unity Editor 中配置 Animator 参数、连线和 Clip 循环。
4. 运行 EditMode 规则测试，再在 Demo2 Play Mode 验证实际输入与动画。
5. 若运行验证失败，回退适配器改动和 Animator 资源；现有 Clip 本身不迁移、不重切片。
