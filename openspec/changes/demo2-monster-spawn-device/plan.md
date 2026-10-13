# Demo2 刷怪装置实施计划

## 实施顺序

1. 为 `Demo2TreeController` 增加一次性 `SproutingStarted` 事实，在成功进入 Sprouting 的同一调用内发布。
2. 重构 `Demo2MonsterController` 的启动校验，增加一次性 `Initialize(tree, simulation)`，同时保持手工场景实例兼容。
3. 新增 `Demo2MonsterSpawnDevice`，实现独立数量、首只立即、2秒间隔、冻结暂停、阻挡等待和单装置完成事实。
4. 增加树事件、怪物初始化、独立波次、阻挡等待与完成一次性的 EditMode/PlayMode验证。
5. 创建不可见 `MonsterSpawnDevice.prefab`，连接史莱姆 Prefab、SpawnPoint与SpawnedMonsters；不放置场景实例。
6. 编译生产和测试程序集，执行目标验证并记录 Editor 场景接线未完成。

## 文件范围

### 修改生产代码

```text
Assets/Scripts/Demos/Demo2/Tree/Demo2TreeController.cs
Assets/Scripts/Demos/Demo2/Monster/Demo2MonsterController.cs
```

### 新增生产代码与资源

```text
Assets/Scripts/Demos/Demo2/Monster/Demo2MonsterSpawnDevice.cs
Assets/Scripts/Demos/Demo2/Monster/Demo2MonsterSpawnDevice.cs.meta
Assets/Prefabs/Demo2/MonsterSpawnDevice.prefab
Assets/Prefabs/Demo2/MonsterSpawnDevice.prefab.meta
```

### 测试

```text
Assets/Tests/EditMode/
Assets/Tests/PlayMode/
```

## 依赖与边界

- 生产代码继续位于 `Spotlight.Game`，不新增程序集。
- 装置只依赖具体 `Demo2TreeController`、`Demo2SimulationController` 和 `Demo2MonsterController`，不引入通用生成接口。
- 树不引用装置；怪物不引用装置；装置通过公开事实和初始化入口编排。
- 多装置总完成、对话、树成长和关卡阶段由后续控制器负责。
- 不修改水检测、Input Actions、Build Settings或正式场景。

## Prefab合同

```text
MonsterSpawnDevice
├── SpawnPoint
└── SpawnedMonsters
```

默认配置：

```text
Spawn Count       8
Spawn Interval    2
Blocking Layers   Default
Clearance Size    0.9 × 1.5
```

Prefab资产连接 `SlimeMonster.prefab` 和两个子节点；`Tree Controller` 与 `Simulation Controller` 由场景实例接线。

## 验证路径

- OpenSpec：`openspec validate demo2-monster-spawn-device --strict`。
- 编译：`Spotlight.Game`、`Spotlight.Tests.EditMode`、`Spotlight.Tests.PlayMode`。
- EditMode：树启动事实与波次时序边界。
- PlayMode：阻挡等待、Prefab实例化注入、独立装置计数和完成事实。
- Editor：Prefab层级与资产引用由Agent检查；场景放置和实际刷怪烟测由项目负责人完成。
