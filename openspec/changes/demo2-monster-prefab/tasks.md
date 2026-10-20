## 1. OpenSpec 确认

- [x] 1.1 执行 `openspec validate demo2-monster-prefab --strict` 并修复全部文档错误
- [x] 1.2 获得用户对 proposal、spec、design、plan 和 tasks 的代码实施确认

## 2. 怪物合同与纯状态规则

- [x] 2.1 在 `Assets/Scripts/Demos/Demo2/Monster/` 增加窄 `IMonster`、伤害结果和一次性死亡事实合同
- [x] 2.2 增加 `Demo2MonsterConfig` ScriptableObject 类型及有限正数配置校验，不静默夹紧非法资产值
- [x] 2.3 实现 `Moving`、`Biting`、`HitStun`、`Dead` 四态生命与计时模型
- [ ] 2.4 用 EditMode 测试验证非法伤害、三次10点伤害、硬直重置、啃食中断和死亡计时边界

## 3. Unity 运行时适配

- [x] 3.1 实现 `Demo2MonsterController` 的 Inspector 引用和 Animator `State` 参数校验，缺失配置时明确报错并禁用
- [x] 3.2 接入侧视水平移动、竖直重力保留、0.5米距离判定和树苗攻击者成对登记
- [x] 3.3 让 `SideView` 父节点停用完整暂停怪物，重新启用后保留生命、位置和状态剩余进度
- [x] 3.4 接入受击浅红、0.5秒硬直、立即死亡事实、碰撞关闭和0.5秒后销毁
- [x] 3.5 删除怪物对玩家视角事件的订阅和手动视觉、碰撞、Rigidbody2D切换

## 4. 运行时验证

- [ ] 4.1 用 PlayMode 测试验证移动只改水平速度、到树登记及受击/禁用/销毁清理登记
- [ ] 4.2 用 PlayMode 测试验证GameObject停用暂停、重新启用恢复、死亡事件一次且表现结束后销毁
- [x] 4.3 编译 `Spotlight.Game`、`Spotlight.Tests.EditMode` 和 `Spotlight.Tests.PlayMode` 程序集
- [ ] 4.4 执行怪物目标 EditMode 与 PlayMode 测试并记录实际结果
- [x] 4.5 确认未修改场景、Prefab、动画、Sprite切片和配置资产，并记录 Editor 资源与视觉验证未完成

## 当前验证记录

- `Spotlight.Game`、`Spotlight.Tests.EditMode` 和 `Spotlight.Tests.PlayMode` 均编译通过：0个警告、0个错误。
- 已按最新决定删除玩家视角事件引用；小怪由 `SideView` 父节点整体启停。
- 用户明确要求暂不运行测试；任务2.4、4.1、4.2和4.4保持未完成。
- 未修改场景、Prefab、Animator、AnimationClip、Sprite切片或ScriptableObject配置资产。
- Prefab组装、动画连线、Collider尺寸和实际场景视觉的 Editor 验证未完成。
- 项目负责人已确认玩家命中史莱姆时浅红受击反馈可见，硬直结束恢复正常。
