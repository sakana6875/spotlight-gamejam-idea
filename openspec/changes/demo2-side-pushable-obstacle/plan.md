# Demo2 SideView 可推动障碍物实施计划

## 实施顺序

1. 读取现有玩家朝向、Input System 和测试场景配置，固定本 Change 的最小接口与范围。
2. 在 `Demo2PushableObstacle` 中校验动态 Rigidbody2D 和推动参数，并提供基于 `AddForce` 的水平推动入口。
3. 在 `Demo2PushController` 中读取 `Demo2/Push`，仅在 SideView 按住输入时使用玩家 Collider2D 的预分配 Cast 命中可推动障碍物，并沿只读玩家朝向推动。
4. 为 `PlayerMoveController` 增加只读朝向属性，不复制或接管玩家朝向状态。
5. 通过 Unity Editor 配置一个动态可推动障碍物，并将 Collider2D 接入固定行水道移动阻挡。
6. 验证 SideView 推动、TopView 禁止推动、墙体碰撞和障碍移开后原行继续流动。
7. 在 Demo2 场景确认推动不会生成第三条水道。

## 文件范围

- `openspec/changes/demo2-side-pushable-obstacle/`：本变更合同和任务。
- `Assets/Scripts/Demos/Demo2/`：推动组件、推动控制器和玩家只读朝向。
- `Assets/Tests/PlayMode/`：推动行为验证。
- `Assets/Scenes/demo2.unity`：由 Unity Editor 保存 Player、障碍物和固定行水道移动阻挡引用。

## 不修改

`WaterDampingArea` 的手工水格阻尼合同、`SceneLoader`、`SceneCatalog`、`SessionRoot`、Build Settings、Input Actions 资产、TopView 玩法、战斗和剧情系统。
