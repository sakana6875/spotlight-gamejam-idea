## Context

参见 `proposal.md` 的 Why。现有 `Demo2TreeController` 已提供攻击者登记/解除边界，`Demo2ViewModeController` 已通过 `SideView` 与 `TopView` 根节点表达互斥视图，`Demo2SimulationController` 已表达全局玩法冻结。仓库尚无怪物代码、怪物配置类型或可消费的怪物接口；`Slime.png` 仍是未切片的 Editor 资源。

用户明确要求本 Change 只负责代码实现。Prefab、Sprite 切片、Animator Controller、AnimationClip、ScriptableObject 配置资产和场景接线均由项目负责人在 Unity Editor 中完成，代码不得用自动初始化掩盖这些工作。

## Goals / Non-Goals

**Goals:**

- 提供可挂到侧视小怪 Prefab 的明确运行时组件和窄战斗接口。
- 用可独立测试的纯状态模型维护生命、受击、啃食和死亡计时。
- 复用既有树苗、侧视根节点启停和暂停边界，不增加场景扫描或全局服务。
- 让缺失 Inspector 引用、非法配置和 Animator 合同错误立即可观察。

**Non-Goals:**

- 不由代码创建、切片或配置任何 Unity 美术资源、Prefab、配置资产或场景对象。
- 不实现玩家攻击、小怪接触伤害、波次、小 Boss、寻路、对象池或跨 Demo AI 框架。
- 不引入玩家与怪物共用的角色基类，也不扩大 `IMonster` 到 Demo2 场景规则。

## Decisions

### 1. 使用窄 `IMonster`，不使用通用角色继承树

`IMonster` 只暴露 `IsAlive`、`CurrentHealth`、伤害结果和 `Died` 事实。伤害返回枚举，区分成功、致死、非法伤害和目标已死亡；死亡事件携带 `IMonster` 发布者并且只发布一次。

选择接口是因为后续玩家剑击和波次统计存在真实替换边界；移动目标、视角和 Animator 是 Demo2 小怪实现细节。通用 `Character` 基类会迫使玩家和小怪共享并不存在的移动、攻击和死亡规则，因此不采用。

### 2. 纯状态模型维护规则，MonoBehaviour 只适配 Unity

`Demo2MonsterStateMachine` 持有当前状态、当前生命、受击剩余时间和死亡剩余时间，并提供：

- `Advance(deltaTime, isWithinBiteRange)`；
- `TryTakeDamage(damage)`；
- 当前状态、生命、是否应消失等只读结果。

状态固定为 `Moving=0`、`Biting=1`、`HitStun=2`、`Dead=3`，数值同时作为 Animator `State` 参数合同。状态模型不引用 Unity 对象，也不发布 Unity 事件。控制器根据伤害结果在致死转换时发布一次 `Died`。

不使用 `StateMachineBehaviour` 或 Animation Event，因为0.5秒受击/死亡时长和登记清理必须在无 Animator 的 EditMode 测试中确定验证。

### 3. ScriptableObject 类型只承载怪物调参

`Demo2MonsterConfig` 包含最大生命、移动速度、啃食距离、受击硬直秒数和死亡表现秒数，并通过只读属性公开。类型提供明确的配置校验结果，但不在 `OnValidate` 中静默夹紧非法值。首个配置资产由项目负责人创建并填写30、1、0.5、0.5、0.5。

玩家攻击力、小怪未来接触伤害和Collider尺寸不写入该配置：前两者属于尚未实施的攻击方合同，Collider由 Prefab Editor 配置决定。

### 4. Inspector 显式接线，Awake 只校验

`Demo2MonsterController` 使用 `[SerializeField] private` 接收配置、Rigidbody2D、Collider2D、SpriteRenderer、Animator、视觉根节点、`Demo2TreeController` 和 `Demo2SimulationController`。

`Awake` 只执行引用、配置值和 Animator `State` 整数参数校验。失败时输出包含对象与字段原因的错误并禁用组件。不会调用 `FindObjectOfType`、`GetComponentInChildren`、`Resources.Load`，也不会自动创建缺失组件或配置资产。

自身引用同样由 Editor 明确配置，便于 Prefab 结构变化时保持可检查；场景引用由每个 Prefab 实例覆盖。

### 5. Rigidbody2D 只接受水平控制

侧视活动时，控制器在 `FixedUpdate` 中只修改 Rigidbody2D 的水平速度：

- `Moving`：朝树苗X方向设为配置速度；
- `Biting`、`HitStun`、`Dead`：水平速度为零；
- 所有存活状态保留现有竖直速度和重力。

啃食距离按怪物与树苗目标 Transform 的水平距离判断。进入 `Biting` 时登记，任何离开路径均通过一个幂等方法解除，防止受击、父节点停用、禁用和销毁留下攻击者。

不引入 NavMesh 或自定义寻路；首个小怪是侧视直线守树单位。

### 6. 俯视复用场景根节点生命周期，不建立怪物视角适配器

项目负责人将怪物实例放置在 `SideView` 根节点下。切换俯视时，既有 `Demo2ViewModeController` 停用整个 `SideView`；Unity 自动停止子级怪物的 Update、FixedUpdate、Animator、渲染与物理，并触发 `OnDisable` 解除啃食登记。切回侧视时重新启用同一对象，控制器在 `OnEnable` 以零时间同步树苗距离和 Animator 参数，保留 Transform、生命和状态剩余时间。

不订阅 `PlayerMoveController.ViewModeChanged`，也不手动关闭视觉、Collider2D 或 Rigidbody2D。场景层级已经完整表达“小怪不在俯视世界存在”，重复的代码视角状态只会增加失配风险。

全局冻结不会停用根节点，因此控制器仍显式检查 `IsGameplayFrozen` 并停止物理控制与状态推进。

### 7. 死亡事实与视觉结束解耦

致死伤害立即进入 `Dead`、解除登记、关闭身体碰撞并发布一次 `Died`。状态模型随后按缩放时间累计死亡表现；达到配置时长后控制器销毁 GameObject。这样后续波次可以立即统计死亡事实，而0.5秒动画仍完整播放。

若死亡表现期间 GameObject 被侧视根节点停用或发生全局冻结，死亡计时暂停；重新启用或解除冻结后继续。死亡后伤害不会重播动画、重复事件或重置计时。

### 8. Animator 资源由 Editor 配置

代码只写整数 `State`：`0=Moving`、`1=Biting`、`2=HitStun`、`3=Dead`。啃食可在 Animator 中复用待机 Motion；受击可保持当前 Motion，控制器把SpriteRenderer从原色切为浅红色提供反馈并在硬直结束恢复；死亡状态播放项目负责人切出的3帧 Clip。

控制器校验参数类型但不校验状态名或创建 Transition。状态图、Clip循环与Sprite切片属于 Editor 验收，不用 Editor 脚本生成。

## Risks / Trade-offs

- [Prefab 场景引用不能保存在 Prefab 资产中] → 每个场景实例必须在 Inspector 覆盖树苗和暂停引用，并放在 `SideView` 根节点下；缺失时立即报错并禁用，不自动查找。
- [受击与啃食暂时缺少专用美术] → 保持稳定状态参数，暂用浅红色反馈与待机 Motion；替换 Clip 不改玩法代码。
- [水平距离不处理复杂地形] → 当前只支持侧视直线守树小怪；需要平台跨越或绕障时建立独立 Change。
- [死亡事件早于 GameObject 销毁] → 事件明确表示生命已归零，不表示视觉对象已消失；后续波次必须按该事实统计。
- [代码交付无法完成最终视觉验证] → 自动测试验证状态和运行时生命周期；Prefab、动画、Collider尺寸与场景表现明确标记为 Editor 验证未完成。

## Migration Plan

1. 先合入代码、测试和 OpenSpec，不修改现有场景及资源。
2. 项目负责人切分 `Slime.png`，创建配置资产、Animator 和 Prefab，将实例放到 `SideView` 下并接入树苗与暂停引用。
3. 完成 Editor 烟测后，后续 Change 再接玩家攻击、小怪接触伤害和8只波次。
4. 回滚时删除新增 Monster 脚本及测试即可；现有场景、树苗和玩家代码没有格式或数据迁移。
