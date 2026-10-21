## Why

Demo2 已有树苗接水事件、可复用史莱姆 Prefab 和怪物死亡事实，但缺少把这些能力连接成独立波次的刷怪装置。需要一个可在不同 SideView 场景重复放置的逻辑 Prefab，让每个装置在树首次接水时独立生成配置数量的怪物，而不把波次职责塞进树苗或水流控制器。

## What Changes

- 树成功从 `Dormant` 进入 `Sprouting` 时立即发布一次 `SproutingStarted` 事实，现有水检测仍只由 `Demo2TreeWaterTrigger` 负责。
- 新增不可见的 `Demo2MonsterSpawnDevice` 逻辑 Prefab，包含一个固定 `SpawnPoint` 和一个 `SpawnedMonsters` 子节点。
- 每个场景装置实例独立配置生成数量；Prefab 默认8只、成功生成间隔2秒。
- 树接水成功后第一只怪物立即尝试生成；后续每次成功生成后重新计时2秒。
- 生成点被配置阻挡层占用时等待，不消耗数量；清空后立即生成当前待生成怪物。
- 装置放在 `SideView` 下；切换俯视、对话冻结或其他玩法暂停时不推进波次计时。
- 为 `Demo2MonsterController` 增加一次性显式运行时初始化入口，使刷怪装置在实例化后注入树苗和暂停控制器，不使用单例、标签或场景扫描。
- 每个装置独立统计已生成和存活怪物；全部配置数量均已生成且全部死亡时发布一次 `WaveCompleted` 事实。
- 新增波次时序、阻挡等待、独立装置、怪物初始化和完成事实的目标测试。

## Capabilities

### New Capabilities

- `demo2-monster-spawn-device`: 定义树接水事实、独立刷怪装置、生成阻挡、怪物注入和单装置波次完成合同。

### Modified Capabilities

无。

## Impact

- 修改 `Demo2TreeController`，新增 `SproutingStarted` 事实事件。
- 修改 `Demo2MonsterController`，拆分自身配置校验与场景依赖注入，并新增一次性初始化入口。
- 新增 `Assets/Scripts/Demos/Demo2/Monster/Demo2MonsterSpawnDevice.cs` 及对应 `.meta`。
- 新增 `Assets/Prefabs/Demo2/MonsterSpawnDevice.prefab` 及对应 `.meta`；不放置或修改正式场景实例。
- 复用现有 `SlimeMonster.prefab`、`Demo2SimulationController` 和 `IMonster.Died`，不新增程序集、Unity 包或第三方依赖。

## 非目标

- 不实现多个装置共享总数量、全场波次协调器、左右轮换调度或关卡结算。
- 不实现对话、树最终成长、20秒强制过关、玩家攻击或小怪接触伤害。
- 不实现对象池、随机怪物表、概率权重、Boss生成或跨Demo通用刷怪框架。
- 不创建可见传送门、粒子、音效或占位 Sprite。
- 不让装置自行查找树苗、暂停控制器、SideView 或其他装置。
