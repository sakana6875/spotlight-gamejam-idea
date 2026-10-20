# Demo2 玩家动画状态机任务

## 1. OpenSpec

- [x] 1.1 确认二段攻击后半段排队、攻击锁定移动、空中攻击、重复受击忽略和切视角取消动作规则
- [x] 1.2 执行 `openspec validate demo2-player-animation-state --strict` 并修复全部文档错误
- [x] 1.3 获得用户对 proposal、spec、design、plan 和 tasks 的实施确认

## 2. 纯状态规则

- [x] 2.1 在 `Assets/Scripts/Demos/Demo2/Player/` 实现五种互斥状态、动作计时、许可和明确取消入口
- [x] 2.2 实现 0.25 秒后半段二击排队、每段 0.8 秒间隔和禁止第三段规则
- [x] 2.3 实现 0.5 秒受击中断、取消排队和硬直期间拒绝重复受击
- [x] 2.4 用 EditMode 测试验证窗口边界、冷却、空中攻击、受击中断、重复受击和动作取消

## 3. 运行时接入

- [x] 3.1 让 `PlayerMoveController` 推进状态、读取 Attack、设置 Animator `State` 参数并暴露当前状态与受击入口
- [x] 3.2 在攻击和受击期间清零水平控制、拒绝跳跃，同时保持侧视竖直速度和重力
- [x] 3.3 让 `Demo2PushController` 在玩家状态不允许推动时返回失败
- [x] 3.4 让 `Demo2ViewModeController` 在合法转场开始前取消攻击、受击和二段排队
- [x] 3.5 验证全局冻结只暂停状态计时，解除后从原动作进度继续
- [x] 3.6 允许俯视模式复用现有攻击动画起手二段攻击，并在动作期间清零平面速度

## 4. Animator 资源

- [x] 4.1 在 Unity Editor 为 `Demo2Player.controller` 增加整数 `State` 参数和与合法转换一致的可视化连线
- [x] 4.2 将所有状态 Transition 配置为无 Exit Time、零过渡时长且禁止自转换
- [x] 4.3 保持 `PlayerIdle`、`PlayerRun` 循环并关闭 `PlayerAttack1`、`PlayerAttack2`、`PlayerHitted` 循环
- [x] 4.4 检查五个状态的 Motion、Player Animator Controller 引用和全部 `.meta` 完整

## 5. 验证

- [x] 5.1 编译 `Spotlight.Game`、`Spotlight.Tests.EditMode` 和受影响的 PlayMode 测试程序集
- [x] 5.2 执行玩家状态机目标 EditMode 测试并记录实际结果
- [x] 5.3 在 Demo2 Play Mode 验证侧视与俯视的待机、跑动、单击、后半段二击、过早二击和每段冷却
- [x] 5.4 在 Demo2 Play Mode 验证空中攻击继续下落、动作期间移动/跳跃/推动受限以及切视角取消动作
- [x] 5.5 在 Demo2 Play Mode 验证对话冻结暂停动作与计时，解除后继续
- [x] 5.6 删除临时验证脚本并在本文记录命令、测试和 Editor 验证结果

## 当前验证记录

- 产品规则已由用户确认。
- `openspec validate demo2-player-animation-state --strict` 已通过。
- `dotnet build Spotlight.Game.csproj --no-restore`、`Spotlight.Tests.EditMode.csproj` 和 `Spotlight.Tests.PlayMode.csproj` 均为 0 个警告、0 个错误。
- Unity 2022.3.62f3c1 EditMode 定向执行 `Demo2PlayerStateMachineTests`：5/5 通过。
- Unity 2022.3.62f3c1 PlayMode 定向执行推动、转场和冻结测试：7/7 通过；覆盖空中动作保留重力、受击禁止推动、转场取消动作和冻结恢复。
- Animator 资源烟测通过：`State` 参数存在，12 条 Transition 均为无 Exit Time、零时长、禁止自转换；五个 Clip 循环设置符合合同。
- 临时 Animator 配置器、独立 Unity 验证项目和状态烟测项目已删除。
- 用户追加确认俯视复用现有侧身攻击动画；侧视限定已移除，三套受影响程序集重新编译为 0 个警告、0 个错误。
- 项目负责人已在当前 Unity Editor 确认俯视鼠标左键可播放 Attack1、后半段输入可进入 Attack2，攻击期间停止平面移动；Editor 视觉验证完成。
