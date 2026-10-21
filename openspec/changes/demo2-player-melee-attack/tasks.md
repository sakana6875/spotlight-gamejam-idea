## 1. OpenSpec确认

- [x] 1.1 确认两段各10点、0.25秒命中、默认1.5×0.8可调尺寸、AttackPoint定位、全目标、SideWall遮挡和Monster Layer规则
- [x] 1.2 执行 `openspec validate demo2-player-melee-attack --strict` 并修复全部文档错误
- [x] 1.3 获得用户对proposal、spec、design、plan和tasks的代码实施确认

## 2. 状态命中事实

- [x] 2.1 为玩家状态推进增加None、Attack1Strike和Attack2Strike一次性结果
- [x] 2.2 实现两段各自首次跨过0.25秒报告且低帧率不漏报
- [x] 2.3 保持暂停、受击中断、视角取消、二击窗口和冷却既有行为
- [x] 2.4 增加精确边界、单段一次、跨帧与取消EditMode测试

## 3. 近战查询

- [x] 3.1 新增玩家近战组件及配置校验和选中Gizmo
- [x] 3.2 实现以AttackPoint为中心、默认1.5×0.8可调尺寸的复用OverlapBox查询
- [x] 3.3 实现Monster Layer筛选、多Collider去重和全目标各10点伤害
- [x] 3.4 实现逐目标SideWall遮挡且不影响同段其他目标
- [x] 3.5 让PlayerMoveController消费Strike结果并调用近战组件

## 4. Unity资源

- [x] 4.1 在TagManager增加Monster Layer且保持Physics2D矩阵不变
- [x] 4.2 将SlimeMonster根对象迁移到Monster Layer
- [x] 4.3 通过Unity Editor给Demo2 Player增加AttackPoint、近战组件和引用
- [x] 4.4 检查场景刷怪装置、玩家身体Collider和全部meta未被破坏

## 5. 验证

- [x] 5.1 增加AttackPoint左右镜像、可调尺寸、范围外和俯视最近水平朝向PlayMode测试
- [x] 5.2 增加多目标、多Collider去重和SideWall遮挡PlayMode测试
- [x] 5.3 增加三次10点命中触发现有史莱姆死亡流程测试
- [x] 5.4 编译生产、EditMode和PlayMode程序集
- [ ] 5.5 按用户许可执行玩家近战目标测试并记录实际结果
- [x] 5.6 项目负责人烟测左右攻击、二段伤害、多目标、墙后目标和三次击杀

## 当前验证记录

- `Spotlight.Game`、`Spotlight.Tests.EditMode` 和 `Spotlight.Tests.PlayMode` 编译通过：0个警告、0个错误。
- Unity Editor已在Player下创建局部位置 `(0.9, 0, 0)` 的AttackPoint，并连接默认1.5×0.8、10点、Monster和SideWall配置。
- 现有玩家身体BoxCollider2D的偏移与尺寸保持 `(-0.31989497, 0)` 和 `1.1727101×1.8125`；场景刷怪装置实例及树苗、暂停接线仍存在。
- 玩家近战目标测试已添加但按用户此前要求暂未执行；项目负责人已确认攻击生效、三次10点击杀、左右判定、多目标、SideWall遮挡和浅红受击反馈均正常。
