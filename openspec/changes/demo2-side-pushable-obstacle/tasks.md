# Demo2 SideView 可推动障碍物任务

## OpenSpec

- [x] 阅读现有玩家、输入、手工水格阻尼和测试实现
- [x] 确认按住持续推动与只水平推动合同
- [x] 编写并校验 proposal、spec、plan 和 tasks

## 实现

- [x] 新增 `Demo2PushableObstacle`，校验动态 Rigidbody2D 和推动参数
- [x] 新增 `Demo2PushController`，仅在 SideView 按住 Push 时检测并推动
- [x] 为 `PlayerMoveController` 提供只读水平朝向
- [x] 通过 Unity Editor 配置 Player 推动控制器和一个场景障碍物
- [x] 将障碍物 Collider2D 接入固定行水道移动阻挡

## 验证

- [x] 增加并运行 SideView 持续推动行为测试
- [x] 验证松开 Push 后停止施力
- [x] 验证 TopView、背向对象和普通刚体不会被推动
- [x] 验证障碍物与墙体碰撞不穿透
- [x] 验证障碍物移开后原水道行继续流动且不生成第三行
- [x] 在 Unity Editor 中执行 Demo2 SideView 实际烟测

## 当前验证记录

- 推动验证覆盖 SideView、TopView、普通刚体和墙体碰撞；水道合同已修正为只在两条手工源水行内水平推进。
- 项目负责人已在 Unity Editor 中手动确认 SideView 按住 Push 推动成功。
- 测试执行期间临时 InputTestFixture 自动化触发 Unity Input System 原生崩溃与后续空引用日志；相关临时启动器、回调和 InputTestFixture 用法均已删除，不属于运行时实现。

## 严格F授权变更

- [x] 确认箱子保持Dynamic、Y方向重力下落且推动目标速度改为1米/秒
- [x] 将障碍物实现改为未授权锁X、授权设置X速度并保留Y速度
- [x] 保持玩家仅在SideView持续按F且状态允许时提交推动请求
- [x] 更新未按F、怪物碰撞、重力下落、松开锁定和墙体阻挡PlayMode测试
- [x] 编译受影响程序集
- [ ] 执行重力与严格F授权目标PlayMode测试
- [ ] 项目负责人在Editor确认Dynamic、速度1并烟测

## 重力与严格F授权验证记录

- `Spotlight.Game` 和 `Spotlight.Tests.PlayMode` 编译通过：0个警告、0个错误。
- 已增加未授权玩家碰撞、怪物碰撞、Y方向重力、1米/秒目标速度、松开锁X和墙体阻挡验证，但按用户此前要求暂未执行。
- 场景 `SidePushObstacle` 已保持 Dynamic Rigidbody2D，不需要修改 Body Type；Editor 烟测未完成。
