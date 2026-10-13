# Demo2 空白水域 Prefab 任务

## OpenSpec

- [x] 确认当前水域不是 Prefab
- [x] 确认 Edit Mode 绘制、Play Mode 模拟接管合同
- [x] 编写并严格校验 proposal、spec、design、plan 和 tasks
- [x] 获得用户对 OpenSpec 文档的确认

## 实现

- [x] 创建空白 TerrainWaterArea.prefab
- [x] 配置 Prefab 内部组件与资源引用
- [x] 迁移 demo2 现有两行水 Tile 到 Prefab 实例
- [x] 恢复 Ground、墙体、移动障碍和流动参数 Override
- [x] 删除旧普通 TerrainWaterArea 场景对象

## 验证

- [x] 确认 Prefab 资产 WaterInteriorTilemap Tile 数量为0
- [x] 确认 demo2 实例保留现有两行水 Tile
- [x] 严格校验 demo2-water-area-prefab OpenSpec
- [x] 编译 Spotlight.Game 生产程序集
- [x] 确认 Edit Mode 可以擦除并恢复实例 Tile
- [ ] 项目负责人确认 Play Mode 水流行为未回归

## 当前验证记录

- Unity Editor 迁移结果：Prefab 资产0格，demo2实例16格，捕获与恢复数量一致；实例目标格可擦除并恢复，Prefab 保持空白。
- `Spotlight.Game` 编译通过：0个警告、0个错误；OpenSpec 严格校验通过。
- 等待项目负责人在 Play Mode 确认水流行为。
