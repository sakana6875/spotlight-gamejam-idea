## 1. OpenSpec 确认

- [x] 1.1 执行 `openspec validate demo2-monster-spawn-device --strict` 并修复全部文档错误
- [x] 1.2 获得用户对 proposal、spec、design、plan 和 tasks 的实施确认

## 2. 树与怪物边界

- [x] 2.1 为树增加成功进入 Sprouting 时一次性发布的 `SproutingStarted`
- [x] 2.2 为怪物增加一次性场景依赖初始化入口并保持手工场景实例兼容
- [ ] 2.3 验证重复发芽不重复发布且无效怪物初始化明确失败

## 3. 独立刷怪装置

- [x] 3.1 实现每实例独立数量、默认8只和2秒成功生成间隔
- [x] 3.2 实现首只立即、全局冻结与GameObject停用暂停计时
- [x] 3.3 实现预分配生成区域查询、阻挡等待且不消耗数量
- [x] 3.4 实现怪物Prefab实例化、树苗与暂停依赖注入及失败清理
- [x] 3.5 实现本装置存活计数和一次性 `WaveCompleted`

## 4. Prefab 资源

- [x] 4.1 创建无视觉 `MonsterSpawnDevice.prefab`
- [x] 4.2 连接SlimeMonster、SpawnPoint、SpawnedMonsters和默认波次配置
- [x] 4.3 检查Prefab与全部 `.meta` 完整且未修改正式场景

## 5. 验证

- [x] 5.1 增加树事实、首只立即、2秒间隔和多装置独立计数测试
- [x] 5.2 增加阻挡等待、运行时注入、死亡计数和完成一次性测试
- [x] 5.3 编译生产、EditMode和PlayMode程序集
- [ ] 5.4 执行刷怪装置目标测试并记录实际结果
- [ ] 5.5 项目负责人在Demo2 SideView放置装置并完成实际刷怪烟测

## 当前验证记录

- `Spotlight.Game`、`Spotlight.Tests.EditMode` 和 `Spotlight.Tests.PlayMode` 编译通过：0个警告、0个错误。
- 已增加树启动事实、首只立即、独立装置、冻结暂停、阻挡等待、运行时注入和完成一次性测试，但按用户此前要求暂未执行。
- `MonsterSpawnDevice.prefab` 已连接SlimeMonster、SpawnPoint、SpawnedMonsters、默认8只、2秒、Default阻挡层和0.9×1.5生成区域。
- 正式场景未放置或引用刷怪装置；Editor场景烟测未完成。
