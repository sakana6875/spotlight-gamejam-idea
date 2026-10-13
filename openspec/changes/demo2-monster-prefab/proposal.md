## Why

Demo2 已有树苗的攻击者登记边界和玩家受击状态，但缺少可跨场景复用的小怪实现；现有史莱姆图集也尚未切帧。需要先建立单只侧视小怪从移动、啃食、受击到死亡的完整合同，再在后续 Change 中接入玩家攻击、接触伤害和8只怪物波次。

## What Changes

- 新增可用于组装 Demo2 侧视史莱姆 Prefab 的运行时组件；自身组件、树苗和暂停依赖全部通过 Inspector 明确接线，不自行查找、创建或加载对象。
- 新增怪物 ScriptableObject 配置类型，首个史莱姆资产应配置30点生命、1米/秒移动速度、0.5米啃食距离、0.5秒受击硬直和0.5秒死亡表现；0.9米 × 1.5米身体碰撞体由 Prefab 在 Editor 中配置。
- 新增窄 `IMonster` 战斗接口，只表达存活状态、当前生命、伤害请求和死亡事实；Demo2 的移动、树目标和视角规则不进入通用接口。
- 新增纯 C# 有限状态机，管理 `Moving`、`Biting`、`HitStun` 和 `Dead` 的互斥转换。
- 小怪只放置在场景 `SideView` 根节点下；切到俯视时由既有根节点停用整只怪物，返回侧视后保留原生命、位置和状态剩余进度。
- 小怪到达树苗后停止移动并登记啃食；离开啃食状态、受击、死亡、禁用或销毁时必须解除登记。
- 伤害会使存活小怪原地硬直0.5秒，不产生击退；生命归零时立即进入死亡状态、解除登记、关闭身体碰撞并发布一次死亡事实，死亡动画播放0.5秒后销毁对象。
- 定义 Animator `State` 参数合同：移动、啃食、受击和死亡状态均由代码状态映射；啃食可在 Editor 中复用待机动画，受击由代码闪白，死亡表现时长为0.5秒。
- 新增状态规则 EditMode 测试和运行时组件生命周期 PlayMode 验证；Prefab、动画和场景视觉由项目负责人在 Editor 中验证。

## Capabilities

### New Capabilities

- `demo2-small-monster`: 定义可复用侧视小怪的配置、状态、生命、树苗啃食、启停暂停、动画和死亡生命周期。

### Modified Capabilities

无。

## Impact

- 新增 `Assets/Scripts/Demos/Demo2/Monster/` 下的怪物接口、配置、纯状态规则和 Unity 适配器。
- 新增 C# 脚本及其 `.meta`；不创建 Prefab、Animator Controller、AnimationClip、ScriptableObject `.asset` 或场景资源。
- 复用现有 `Demo2TreeController.RegisterAttacker/UnregisterAttacker` 和 `Demo2SimulationController`，不改变其公开行为。
- 新增 EditMode 与 PlayMode 目标测试；不新增程序集、Unity 包或第三方依赖。
- 本 Change 不修改 `Slime.png.meta` 或 `Assets/Scenes/demo2.unity`，不放置正式场景实例。

## 非目标

- 不实现玩家剑击、攻击范围、10点玩家攻击伤害或玩家攻击到怪物接口的接线。
- 不实现小怪对玩家的10点接触伤害、1秒攻击间隔、玩家100点生命或玩家死亡。
- 不实现8只怪物波次、出生点、全部死亡统计、结算对话或树最终成长编排。
- 不实现俯视小 Boss、横扫、冲撞、撞树眩晕或永久眩晕。
- 不让小怪独立出现在俯视根节点，也不由怪物代码订阅视角事件或手动切换视觉与物理组件。
- 不创建通用 AI、寻路、对象池、通用角色基类或跨 Demo 怪物框架。
- 不通过 Editor 脚本、`Resources.Load`、场景扫描、静态单例或运行时自动创建来代替 Inspector 配置。
- 不创建或修改 Sprite 切片、动画状态机、配置资产、Prefab 或场景；这些 Editor 工作由项目负责人完成。