# Demo2 树苗生命周期实施计划

## 实施顺序

1. 确认成长时机、多个攻击者计时语义、占位图许可和中心检测点位置。
2. 实现纯 C# Demo2TreeStateMachine 和明确状态/结果。
3. 实现 Demo2TreeController 的攻击者集合、20秒阈值和事实事件。
4. 实现使用非缩放时间的 Sprouting 与4帧 Growing 占位动画。
5. 实现 Demo2TreeWaterTrigger，只在水首次到达中心时触发发芽。
6. 使用 Square Sprite 创建 Demo2Tree Prefab，并通过 Unity Editor 接入 demo2 场景。
7. 验证状态转换、多个攻击者不倍增、20秒事件一次、暂停停止计时和非缩放成长动画。
8. 不在本 Change 中接入正式对话、刷怪、玩家攻击或通关路由。

## 文件范围

- `openspec/changes/demo2-tree-lifecycle/`
- `Assets/Scripts/Demos/Demo2/`：树状态、控制器和水触发器。
- `Assets/Resources/Demo2/`：树苗占位 Prefab。
- `Assets/Tests/EditMode/`：纯状态与计时边界。
- `Assets/Tests/PlayMode/`：水触发和非缩放动画行为。
- `Assets/Scenes/demo2.unity`：中心树与检测点场景接线。

## 不修改

水流规则、玩家推动、玩家攻击、怪物、对话、音频、场景加载、存档和 Build Settings。
