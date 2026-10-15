## 1. OpenSpec确认

- [x] 1.1 确认所有前方实体Collider、目标树例外、3.5米高度、空中前进和0.3秒落地重试规则
- [x] 1.2 执行 `openspec validate demo2-slime-obstacle-jump --strict` 并修复全部文档错误
- [x] 1.3 获得用户对proposal、spec、design、plan和tasks的代码实施确认

## 2. 跳跃配置

- [x] 2.1 为怪物配置增加跳跃高度、前向检测距离和落地重试间隔
- [x] 2.2 校验跳跃数值有限范围和Rigidbody2D有效重力
- [x] 2.3 通过Unity Editor更新SlimeConfig默认3.5、0.35和0.3
- [x] 2.4 增加跳跃配置合法与非法值EditMode测试

## 3. 障碍跳跃运行时

- [x] 3.1 实现预分配接触结果和向上法线接地判断
- [x] 3.2 实现朝树方向身体Cast和所有前方实体Collider识别
- [x] 3.3 排除地面、自身、Trigger和当前目标树
- [x] 3.4 用实际重力计算3.5米起跳速度并保持空中水平速度
- [x] 3.5 实现自动起跳落地后的0.3秒重试冷却
- [x] 3.6 保持Biting、HitStun、Dead、冻结和SideView启停禁跳

## 4. 验证

- [x] 4.1 增加推动箱起跳、轨迹和空中水平速度PlayMode测试
- [x] 4.2 增加目标树、地面、Trigger和禁跳状态PlayMode测试
- [x] 4.3 增加落地冷却与SideWall重复尝试PlayMode测试
- [x] 4.4 检查SlimeMonster物理、Layer、Animator和场景资源未改变
- [x] 4.5 编译生产、EditMode和PlayMode程序集
- [ ] 4.6 按用户许可执行史莱姆跳跃目标测试并记录结果
- [x] 4.7 项目负责人烟测箱子、玩家、其他史莱姆、SideWall和树例外

## 当前验证记录

- SlimeConfig已保存 `JumpHeight=3.5`、`ObstacleDetectionDistance=0.35`、`LandingRetrySeconds=0.3`。
- SlimeMonster仍为Monster Layer、Dynamic Rigidbody2D、gravityScale 1、冻结旋转和1×0.778606身体BoxCollider2D；Animator与场景结构未修改。
- `Spotlight.Game`、`Spotlight.Tests.EditMode` 和 `Spotlight.Tests.PlayMode` 编译通过：0个警告、0个错误。
- 配置与PlayMode目标测试已添加但按用户此前要求暂未执行；项目负责人已确认史莱姆会提前起跳、成功越过现有推动箱并继续前进。
