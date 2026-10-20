# Demo2 树苗生命周期任务

## OpenSpec

- [x] 读取当前 Demo2 代码、场景和资源状态
- [x] 确认刷怪期间树保持待长大
- [x] 确认任意数量攻击者只累计一份时间
- [x] 确认允许使用 Square Sprite 临时占位
- [x] 确认树中心检测点使用最右端上层水格
- [x] 编写并严格校验 proposal、spec、design、plan 和 tasks
- [x] 获得用户对 OpenSpec 文档的确认

## 实现

- [x] 实现 Demo2TreeStateMachine 与状态结果
- [x] 实现 Demo2TreeController 攻击登记和20秒阈值
- [x] 实现 Sprouting 与4帧 Growing 非缩放动画
- [x] 实现水首次到中心的一次性触发器
- [x] 创建 Square Sprite 占位树 Prefab
- [x] 通过 Unity Editor 将 demo2 场景中心检测点接入最右端上层水格
- [x] 将水到达前状态改为可见 Dormant，并保持碰撞体关闭

## 验证

- [x] 严格校验 demo2-tree-lifecycle OpenSpec
- [x] 编译 Spotlight.Game 生产程序集
- [ ] 验证刷怪期间保持 AwaitingGrowth
- [ ] 验证多只怪物同时攻击不加速20秒计时
- [ ] 验证对话冻结时最终成长动画继续播放
- [ ] 项目负责人确认占位树位置与状态表现

## 当前验证记录

- 树状态机、20秒单份啃食计时、非缩放动画、水触发器、Square占位Prefab和最右端上层检测点均已实现。
- `Spotlight.Game` 编译通过：0个警告、0个错误；OpenSpec严格校验通过。
- 水到达前状态已改为 Dormant：占位 Sprite 可见、初始缩放为25%、Collider2D关闭；水到达后启用碰撞并播放发芽动画。
- `Spotlight.Game` 再次编译通过：0个警告、0个错误；树生命周期 OpenSpec 严格校验通过。
- 树视觉已拆到 Prefab 的 `Visual` 子节点：编辑器初始缩放0.25、Sprite可见、Collider2D关闭；运行时 Awake 只校验配置，不再改写视觉或碰撞状态。
- Sprouted Scale、Sprouted Color 和 Mature Scale 已暴露到 Inspector；水到达后才启用碰撞并播放配置目标动画。
- `Spotlight.Game` 编译通过：0个警告、0个错误；OpenSpec严格校验通过。
- 按用户要求未启动Unity Test Runner；等待项目负责人在Play Mode确认占位树状态表现。
- 回归诊断确认WaterTarget曾落在 `(13,-2)` 而水道位于 `y=-1`；已将Demo2Tree Prefab的WaterTarget局部位置修正为 `(0,0,0)`。
- 项目负责人已确认修正后水到树会触发发芽，并在同一流程启动刷怪装置。
