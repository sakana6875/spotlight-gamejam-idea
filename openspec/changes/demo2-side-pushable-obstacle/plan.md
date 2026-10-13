# Demo2 SideView 可推动障碍物实施计划

## 实施顺序

1. 读取现有玩家朝向、Input System 和测试场景配置，固定本 Change 的最小接口与范围。
2. 将 `Demo2PushableObstacle` 改为校验 Dynamic Rigidbody2D 和1米/秒推动速度，并维护当前物理步推动请求。
3. 在障碍物晚于玩家控制器的 FixedUpdate 中切换 `FreezePositionX`，未授权时锁X，授权时设置X速度并保留Y速度。
4. 保持 `Demo2PushController` 只在 SideView 按住 `Demo2/Push`、玩家状态允许且前方接触箱子时提交推动请求。
5. 更新 PlayMode 测试，覆盖未按F、怪物碰撞、重力下落、持续按F、松开锁定和墙体阻挡。
6. 编译受影响程序集并执行目标验证。
7. 在 Demo2 场景确认箱子使用Dynamic、推动速度为1，并且仍阻挡原水道行。

## 文件范围

- `openspec/changes/demo2-side-pushable-obstacle/`：本变更合同和任务。
- `Assets/Scripts/Demos/Demo2/`：推动组件、推动控制器和玩家只读朝向。
- `Assets/Tests/PlayMode/`：推动行为验证。
- `Assets/Scenes/demo2.unity`：无需修改 Body Type；项目负责人在 Unity Editor 烟测重力与严格F授权。

## 不修改

`SceneLoader`、`SceneCatalog`、`SessionRoot`、Build Settings、Input Actions 资产、TopView 玩法、战斗和剧情系统。
