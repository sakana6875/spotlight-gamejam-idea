## Context

现有 `Demo2TreeWaterTrigger` 唯一检测水到达并调用 `Demo2TreeController.BeginSprouting()`；树只在发芽动画结束后发布 `SproutingCompleted`。`SlimeMonster.prefab` 已存在，但其控制器在 `Awake` 要求树苗和暂停控制器非空，因此 Prefab 资产无法直接由运行时实例化。`IMonster.Died` 已提供波次统计所需的死亡事实。

## Goals / Non-Goals

**Goals:**

- 让树成功开始发芽成为所有绑定装置的一次性启动事实。
- 让每个装置实例独立维护数量、计时、阻挡等待和完成状态。
- 通过具体初始化入口解决运行时 Prefab 的场景依赖，不引入服务定位。
- 提供无可见资源的可复用逻辑 Prefab，不修改正式场景。

**Non-Goals:**

- 不建立全场波次协调器、关卡流程控制器、对话或树成长结算。
- 不实现对象池、随机表、Boss或通用生成框架。

## Decisions

### 1. 树发布 SproutingStarted，装置不重复检测水

`Demo2TreeController.BeginSprouting()` 在纯状态转换成功后、启动发芽动画前发布 `SproutingStarted`。事件只表示“树已经开始发芽”的事实。`Demo2TreeWaterTrigger` 保持唯一水检测适配器；装置只订阅树事件。

装置 `OnEnable` 订阅事件。如果启用时树已不处于 `Dormant` 且装置尚未启动，则立即补启动，避免 SideView 启停造成事实遗漏。`_hasStarted` 保证一个实例只启动一次。

### 2. 每个装置是独立的一点波次

`Demo2MonsterSpawnDevice` 序列化：树苗、暂停控制器、怪物 Prefab、SpawnPoint、SpawnedMonsters 根节点、数量、间隔、阻挡层和生成区域尺寸。Prefab 默认8只、2秒；场景实例可以分别覆盖数量，因此多个装置的总数自然相加，不需要共享协调器。

启动时把下一次生成标记为立即到期。成功生成后清零间隔计时；到期但阻挡时保持到期状态，每次 Update 重查，清空后立即生成。使用 Update 状态字段而不是 Coroutine，使 GameObject 停用和全局冻结自然暂停且恢复原进度。

### 3. 生成区域使用无分配重叠查询

装置使用固定 `Collider2D[]` 缓存和 `Physics2D.OverlapBox` 的 ContactFilter2D 重载查询 `_spawnBlockingLayers`。任何命中均视为阻挡。生成区域由 Prefab Inspector 明确配置，并通过 `OnDrawGizmosSelected` 只在 Editor 显示线框；运行时不创建视觉对象。

### 4. 怪物初始化分为自身配置和场景依赖

`Demo2MonsterController.Awake` 只校验 Config、Rigidbody2D、Collider2D、SpriteRenderer、Animator和Visual Root，并创建纯状态模型。场景内手工放置实例可继续序列化树苗与暂停引用；`Start` 在未初始化时使用这些引用完成配置，缺失则明确报错并禁用。

运行时生成时，Unity 会先执行 Prefab 的 Awake/OnEnable，装置随后在同一帧调用 `Initialize(tree, simulation)`，该方法在首次 Update/FixedUpdate 前完成场景依赖配置并同步状态。重复初始化、空引用或自身配置无效均返回失败。这样保留明确依赖，同时不要求 inactive 模板或场景扫描。

### 5. 每个装置只统计自己的怪物

装置在成功初始化后订阅具体怪物的 `Died`，增加已生成和存活计数。死亡时立即解除订阅并减少存活计数。当已生成等于配置数量且存活为零时发布一次 `WaveCompleted`。装置禁用只暂停生成，不解除存活怪物的死亡订阅；装置销毁时统一解除，避免回调残留。

### 6. Prefab 只保存自身引用

`MonsterSpawnDevice.prefab` 层级为根节点、`SpawnPoint`、`SpawnedMonsters`。Prefab 连接 `SlimeMonster.prefab` 与两个子 Transform，树苗和暂停控制器保持空，由每个场景实例在 Inspector 覆盖。Prefab 不含 SpriteRenderer、Animator、Collider或AudioSource。

## Risks / Trade-offs

- [多个装置默认各8只会快速增加总数] → 数量明确按实例配置，总量由场景设计者负责；不隐式共享。
- [生成点持续阻挡会让波次永久等待] → 这是防止重叠的明确合同；场景设计必须给生成点留出移动空间。
- [运行时初始化改变怪物启动顺序] → Awake只处理自身资源，Start前必须完成场景依赖；测试覆盖手工实例和生成实例两条路径。
- [装置在树触发后才启用] → OnEnable检查树阶段并补启动，但一个实例仍只执行一次。

## Migration Plan

1. 先合入树事件、怪物初始化入口、装置代码、测试和逻辑 Prefab，不修改正式场景。
2. 项目负责人把装置 Prefab放入 `SideView`，为每个实例配置数量、树苗、暂停控制器、阻挡层和位置。
3. 后续关卡控制器订阅多个装置的 `WaveCompleted`，再接对话和树成长结算。
4. 回滚时删除装置 Prefab和脚本，并移除树事件与怪物初始化改动；现有场景无格式迁移。
