# Demo2 玩家动画状态机提案

## Why

Demo2 玩家已经具备 `PlayerIdle`、`PlayerRun`、`PlayerAttack1`、`PlayerAttack2` 和 `PlayerHitted` 动画，但当前 Animator 没有参数和转换，玩法输入也没有统一状态约束。现在需要先确定代码状态、Animator 表现、二段攻击时序和移动锁定之间的合同，避免后续攻击判定、生命和怪物系统反向控制动画或重复实现状态规则。

## What Changes

- 增加 Demo2 玩家状态模型，当前只覆盖 `Idle`、`Moving`、`Attack1`、`Attack2` 和 `HitStun`。
- 使用代码状态模型维护合法转换、计时、二段攻击排队、0.8 秒攻击间隔和移动许可。
- 使用 Animator 的 `State` 整数参数和可视化 Transition 将状态映射到现有五个动画；Animator 不决定玩法转换。
- 第一次攻击后只在 0.25 秒至动作结束的后半段接受第二次点击，并在第一段结束后立即进入第二段。
- 侧视和俯视均允许二段攻击；俯视暂时复用现有侧身攻击动画，攻击期间停止平面移动并保留最近一次左右朝向。
- `Attack1`、`Attack2` 和 `HitStun` 期间锁定玩家水平移动和跳跃输入；侧视重力继续生效。
- `PlayerAttack1`、`PlayerAttack2` 和 `PlayerHitted` 改为非循环动画；`PlayerIdle` 和 `PlayerRun` 保持循环。
- 提供明确的受击入口，使后续生命或敌人攻击系统可以请求 0.5 秒硬直；本次不实现伤害来源。

## 目标

- 让移动、攻击和受击动画由唯一玩家状态事实驱动。
- 保留 Animator 中可检查的参数、状态和连线。
- 确保二段攻击、冷却、受击中断和移动锁定可独立测试。
- 为后续剑攻击判定提供稳定的攻击状态，但不提前实现命中逻辑。

## 范围

- `Assets/Scripts/Demos/Demo2/Player` 中的玩家状态规则和现有移动适配。
- `Assets/Animations/Demo2/Player/Demo2Player.controller` 及五个现有动画片段。
- 对应 EditMode 状态规则测试和 Demo2 Play Mode 烟测。

## 非目标

- 不实现剑碰撞体、攻击命中、伤害、生命值、死亡或怪物逻辑。
- 不新增跳跃、下落、推动、搬运、死亡或俯视专用动画。
- 不让 Animator、Animation Event 或动画帧率决定伤害和冷却。
- 不按 Sprite 轮廓逐帧修改玩家身体碰撞体。
- 不实现第三段攻击、随机攻击、蓄力攻击或复杂连击树。

## Capabilities

### New Capabilities

- `demo2-player-animation-state`: 定义 Demo2 玩家移动、二段攻击、受击硬直和 Animator 表现之间的可观察行为合同。

### Modified Capabilities

无。

## 主要决策

- 代码状态模型是玩法状态的唯一来源；Animator 只消费整数状态参数并播放对应 Clip。
- Animator 使用可视化 Transition，不使用代码直接 `Animator.Play` 绕过状态图。
- 二段攻击采用后半段输入排队：`Attack1` 开始后 0.25 秒至结束之间再次点击才排队 `Attack2`。
- 每段攻击从自身开始重新计算 0.8 秒攻击间隔；未排队第二段时，第一段结束后仍需等待剩余间隔才能再次起手。
- 受击优先于攻击；进入 `HitStun` 时取消尚未开始的第二段。
- 当前没有专用跳跃和俯视动画，因此这些动作继续使用现有 Idle/Run 表现，不新增虚假动画状态。

## Impact

- 生产代码：`PlayerMoveController` 将读取攻击输入并服从玩家状态给出的移动许可；新增纯 C# 玩家状态模型。
- Animator：新增 `State` 参数和五个可视化状态转换；现有 Clip 引用保持不变。
- 公共边界：玩家移动组件将暴露当前状态及受击请求入口，供后续伤害系统调用。
- 测试：新增二段输入窗口、冷却、移动锁定和受击中断的行为测试。
- 依赖：不新增程序集、Unity 包或第三方依赖。

## 风险

- Animator 参数值与代码枚举不一致会播放错误动画，因此必须集中映射并在运行时验证参数存在。
- 攻击动作时长与 Clip 时长不一致会产生视觉停顿或提前切换，因此本次合同固定当前约 0.52 秒动作时长，并通过实际播放验证。
- 当前俯视和空中缺少专用素材，视觉仍有限；本次不会用错误素材伪装为已完成状态。
