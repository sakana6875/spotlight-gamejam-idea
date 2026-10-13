## Purpose

定义可由项目负责人组装为 Prefab 的 Demo2 侧视小怪运行时合同，使生命、状态、树苗啃食、侧视根节点启停和死亡生命周期保持确定且可复用，同时不把场景接线或未来战斗系统隐藏在自动初始化中。

## ADDED Requirements

### Requirement: 小怪使用显式配置且配置失败可观察

小怪运行时组件 MUST 通过 Inspector 接收怪物配置、Rigidbody2D、身体 Collider2D、SpriteRenderer、Animator、视觉根节点、树苗和玩法暂停控制器。系统 MUST NOT 通过场景扫描、名称、标签、静态单例、`Resources.Load` 或运行时创建来补齐缺失依赖。配置缺失或数值非法时，组件 MUST 记录包含具体原因的错误并停止玩法推进，不得假装成功。

#### Scenario: Prefab 实例缺少树苗引用

- **WHEN** 小怪组件启用时未配置树苗引用
- **THEN** 组件报告缺少树苗引用并保持禁用，不移动也不登记啃食

#### Scenario: ScriptableObject 数值非法

- **WHEN** 配置中的生命、移动速度、啃食距离、受击时长或死亡时长不是有限正数
- **THEN** 组件报告具体非法字段并停止玩法推进，不静默改写资产值

### Requirement: 小怪数值由 ScriptableObject 配置

怪物配置 MUST 提供最大生命、移动速度、啃食距离、受击硬直时长和死亡表现时长的只读运行时值。首个史莱姆配置资产 MUST 使用30点最大生命、1米/秒移动速度、0.5米啃食距离、0.5秒受击硬直和0.5秒死亡表现。身体碰撞体 MUST 由项目负责人在 Prefab 中配置为0.9米 × 1.5米，运行时代码不得重建或逐帧修改碰撞轮廓。

#### Scenario: 首个史莱姆开始运行

- **WHEN** 使用约定的首个史莱姆配置创建状态模型
- **THEN** 当前生命为30且移动、啃食、受击和死亡计时使用配置资产中的对应值

### Requirement: 统一怪物接口保持窄战斗边界

系统 MUST 提供统一怪物接口，暴露存活状态、当前生命、带明确结果的伤害请求以及一次性死亡事实。该接口 MUST NOT 暴露树苗目标、视角切换、Rigidbody2D、Animator 或 Demo2 小怪状态。非正伤害、已死亡目标和成功受伤 MUST 返回不同结果。

#### Scenario: 外部伤害来源命中存活小怪

- **WHEN** 外部调用方向拥有30点生命的小怪请求10点正伤害
- **THEN** 请求成功且当前生命变为20

#### Scenario: 非法伤害请求

- **WHEN** 外部调用方请求0点或负数伤害
- **THEN** 请求返回非法伤害且生命和状态不变

### Requirement: 有限状态机是小怪玩法状态的唯一事实来源

系统 MUST 维护 `Moving`、`Biting`、`HitStun` 和 `Dead` 四种互斥状态。移动许可、啃食登记、受击计时、死亡计时和 Animator `State` 参数 MUST 从该状态产生；Animator、动画事件和 Clip 播放进度 MUST NOT 反向决定玩法转换。

#### Scenario: 小怪尚未到达树苗

- **WHEN** 存活小怪不在啃食距离内且不处于受击硬直
- **THEN** 唯一状态为 `Moving`

#### Scenario: Animator 播放进度变化

- **WHEN** Animator Clip 的长度或播放进度发生变化
- **THEN** 小怪的受击和死亡计时仍由状态模型与配置时长决定

### Requirement: 侧视小怪持续向树苗水平移动并登记啃食

处于 `Moving` 的小怪 MUST 以配置速度沿水平方向朝树苗移动，并保留 Rigidbody2D 的竖直速度和重力。进入配置的啃食距离后 MUST 停止水平移动、进入 `Biting` 并向树苗登记自身；离开 `Biting`、禁用或销毁时 MUST 成对解除登记。重复帧不得重复登记或解除。

#### Scenario: 到达树苗

- **WHEN** 移动中的小怪首次进入树苗0.5米啃食距离
- **THEN** 小怪停止水平移动、进入 `Biting` 且树苗只增加一次该攻击者登记

#### Scenario: 受击中断啃食

- **WHEN** 正在啃食的小怪受到未致死伤害
- **THEN** 小怪立即退出 `Biting` 并从树苗解除登记

#### Scenario: 组件禁用清理登记

- **WHEN** 正在啃食的小怪组件被禁用或对象被销毁
- **THEN** 树苗不再保留该小怪的攻击者登记

### Requirement: 侧视根节点启停完整暂停小怪

小怪 Prefab 实例 MUST 放置在场景 `SideView` 根节点下，不得独立放入俯视根节点。切换俯视时，既有视角控制器停用 `SideView`，Unity GameObject 生命周期 MUST 同时停止小怪渲染、物理、移动、啃食、受击和死亡计时；小怪 `OnDisable` MUST 解除树苗攻击者登记。返回侧视重新启用同一实例时，系统 MUST 保留原位置、生命和状态剩余进度，并根据当前树苗距离恢复移动或啃食。怪物代码 MUST NOT 订阅玩家视角事件或手动切换视觉、Collider2D 与 Rigidbody2D。

#### Scenario: 受击期间停用侧视根节点

- **WHEN** 小怪受击硬直已推进0.2秒后，`SideView` 被停用2秒再重新启用
- **THEN** 小怪仍剩0.3秒硬直，并从原位置和生命继续

#### Scenario: 啃食期间停用侧视根节点

- **WHEN** 正在啃食的小怪随 `SideView` 被停用
- **THEN** 小怪解除树苗攻击者登记，重新启用且仍在距离内时重新登记一次

### Requirement: 受击扣除生命并原地硬直0.5秒

存活小怪受到正伤害后 MUST 先扣除生命。生命仍大于零时 MUST 进入 `HitStun`，立即停止水平移动和啃食，不产生击退；硬直持续配置的0.5秒后，根据当前与树苗的距离返回 `Moving` 或 `Biting`。硬直期间再次受到正伤害 MUST 继续扣血并从最新受击重新计算0.5秒硬直。

#### Scenario: 未致死受击

- **WHEN** 拥有30点生命的小怪受到10点伤害
- **THEN** 生命变为20、小怪进入 `HitStun`、不移动且不啃食

#### Scenario: 硬直期间再次受击

- **WHEN** 小怪硬直已经经过0.3秒时再次受到10点伤害且仍存活
- **THEN** 生命再次减少且硬直剩余时间重置为0.5秒

### Requirement: 生命归零立即死亡并在0.5秒表现后消失

伤害使生命降至零时，生命 MUST 截断为零并立即进入 `Dead`。系统 MUST 立即停止移动、解除啃食登记、关闭身体碰撞、发布且只发布一次死亡事实；配置的0.5秒死亡表现结束后 MUST 销毁怪物 GameObject。死亡期间和死亡后伤害请求 MUST 被拒绝，且不得重复发布死亡事实或延长消失时间。

#### Scenario: 第三次10点伤害致死

- **WHEN** 初始30点生命的小怪连续收到三次10点伤害
- **THEN** 第三次伤害立即使其进入 `Dead`、生命为0并发布一次死亡事实

#### Scenario: 死亡后继续受伤

- **WHEN** 已进入 `Dead` 的小怪再次收到正伤害
- **THEN** 请求返回目标已死亡，死亡事实次数和剩余死亡时间不变

#### Scenario: 死亡表现结束

- **WHEN** 怪物 GameObject 已启用且玩法未冻结时，死亡状态累计达到0.5秒
- **THEN** 怪物 GameObject 被销毁

### Requirement: Animator 只消费稳定状态参数

运行时组件 MUST 只通过整数 `State` 参数表达当前状态，稳定映射为 `0=Moving`、`1=Biting`、`2=HitStun`、`3=Dead`。受击期间组件 MUST 使用 SpriteRenderer 闪白提供临时反馈并在离开受击状态时恢复原颜色。代码 MUST NOT 创建 Animator 状态、Transition、Clip 或 Sprite 切片。

#### Scenario: 项目负责人组装动画

- **WHEN** Prefab Animator 按稳定映射配置移动、啃食、受击和死亡状态
- **THEN** 运行时代码仅更新 `State` 参数，啃食可复用待机 Clip，受击可保持当前帧并由闪白反馈

#### Scenario: Animator 缺少 State 参数

- **WHEN** 小怪组件启用时 Animator 不包含整数 `State` 参数
- **THEN** 组件报告明确配置错误并停止玩法推进，不直接调用 `Play` 或 `CrossFade` 绕过合同
