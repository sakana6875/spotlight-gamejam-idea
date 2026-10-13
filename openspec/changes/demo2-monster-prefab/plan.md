# Demo2 侧视小怪代码实施计划

## 实施顺序

1. 在 `Assets/Scripts/Demos/Demo2/Monster/` 增加窄 `IMonster`、伤害结果枚举和 `Demo2MonsterConfig` ScriptableObject 类型，先固定配置校验与公开合同。
2. 增加纯 C# `Demo2MonsterStateMachine`，实现30点配置生命、移动/啃食转换、0.5秒可重置受击硬直、立即死亡事实和0.5秒死亡计时。
3. 增加 `Demo2MonsterController`，通过 Inspector 引用适配 Rigidbody2D、树苗登记、侧视根节点启停、全局冻结、Animator `State` 和受击闪白。
4. 增加 EditMode 行为测试，覆盖非法伤害、三次10点伤害、重复受击重置、啃食中断、死亡一次性和计时暂停所需的纯规则。
5. 增加目标 PlayMode 测试，动态组装运行时对象验证水平移动保留重力、树苗登记清理、GameObject启停暂停和死亡后销毁；不创建正式 Prefab或场景资源。
6. 编译生产、EditMode 与 PlayMode 程序集，执行目标测试，并记录未完成的 Editor 资源与视觉验证。

## 文件范围

### 新增生产代码

```text
Assets/Scripts/Demos/Demo2/Monster/IMonster.cs
Assets/Scripts/Demos/Demo2/Monster/Demo2MonsterConfig.cs
Assets/Scripts/Demos/Demo2/Monster/Demo2MonsterStateMachine.cs
Assets/Scripts/Demos/Demo2/Monster/Demo2MonsterController.cs
```

每个新文件及目录包含对应 `.meta`。

### 新增测试

```text
Assets/Tests/EditMode/Demo2MonsterStateMachineTests.cs
Assets/Tests/PlayMode/Demo2MonsterControllerTests.cs
```

### 不修改

```text
Assets/Scenes/
Assets/Prefabs/
Assets/Animations/
Assets/Resources/Demo2/TIles/Slime.png.meta
```

## 依赖与边界

- 生产代码继续位于 `Spotlight.Game`，不新增程序集。
- 纯状态机只依赖 C# 基础类型；ScriptableObject 和控制器留在 Game 程序集。
- 控制器只依赖现有 `Demo2TreeController` 和 `Demo2SimulationController` 公开边界；视角切换由场景 `SideView` 根节点启停负责。
- `IMonster` 不包含 Demo2 树苗、视角、Animator 或物理类型。
- 不修改现有玩家、树苗或暂停公开接口；若实现发现当前边界不足，停止受影响实现并先更新 OpenSpec。

## Editor 组装合同

项目负责人后续在 Editor 中完成：

- 将 `Slime.png` 切为待机4帧、移动4帧、死亡3帧；
- 创建首个 `Demo2MonsterConfig` 资产并填写30、1、0.5、0.5、0.5；
- 创建带 `State` Int 参数的 Animator Controller，映射0至3四个状态；
- 创建0.9米 × 1.5米身体 BoxCollider2D 的史莱姆 Prefab；
- 将怪物放在 `SideView` 根节点下，并在每个场景实例上配置树苗和暂停控制器引用。

代码不生成、搜索或猜测这些资源与引用。

## 验证路径

- OpenSpec：`openspec validate demo2-monster-prefab --strict`。
- 编译：`dotnet build Spotlight.Game.csproj --no-restore`、EditMode 与 PlayMode 测试程序集。
- EditMode：目标执行 `Demo2MonsterStateMachineTests`。
- PlayMode：目标执行 `Demo2MonsterControllerTests`，验证实际 Rigidbody2D、GameObject启停暂停、树苗登记和销毁生命周期。
- Editor：Prefab、Sprite切片、Animator连线、Collider尺寸和场景视觉由项目负责人完成；在实际执行前必须记录“Editor 验证未完成”。
