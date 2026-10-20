# Demo2 空白水域 Prefab 提案

## 背景

当前 `TerrainWaterArea` 和 `WaterInteriorTilemap` 是 `demo2.unity` 中的普通场景对象，不存在可复用水域 Prefab。每个新关卡若复制这些对象，会重复组件配置，也无法形成“拖入空白水域后直接绘制起始水格”的稳定制作流程。

## 目标

1. 创建初始不包含任何 Tile 的 `TerrainWaterArea.prefab`。
2. Prefab 内固定持有 Grid、空白 WaterInteriorTilemap、水控制器、渲染器、阻尼区和 BoxCollider2D Trigger。
3. 场景实例允许在 Edit Mode 对 WaterInteriorTilemap 自由绘制和擦除，Tile 数据作为实例 Override 保存。
4. GroundTilemap、SideWallTilemap 和移动障碍物保持场景实例 Override，不进入 Prefab 资产。
5. 迁移当前 demo2 的两行手工水格和所有外部引用，不改变现有关卡运行效果。

## 范围

- 新增 `Assets/Resources/Demo2/Water/TerrainWaterArea.prefab` 及 `.meta`。
- Prefab 资产中的 WaterInteriorTilemap 保持空白。
- 用 Prefab 实例替换 demo2 当前普通 TerrainWaterArea 对象。
- 将当前两行 Wave Tile 复制到新实例的 WaterInteriorTilemap。
- 恢复 Wave Tile、GroundTilemap、SideWallTilemap、移动障碍和流动步长实例引用。
- 保留固定行水平逐格水流、阻尼和动画规则。

## 非目标

- 不允许在 Play Mode 中使用 Tile Palette 修改模拟状态；运行时仍由水流模型接管 Tilemap。
- 不在 Prefab 资产中引用具体场景 Ground、墙体或障碍物。
- 不修改现有两行水道坐标、水流步长、树、玩家或推动规则。
- 不创建运行时对象扫描、按名称自动绑定或第二套水 Tilemap。

## 主要决策

- Prefab 根节点持有 Grid，确保在 Prefab Mode 和任意场景层级中都具有独立、明确的单元坐标系。
- WaterInteriorTilemap 是 Prefab 内部子对象；Prefab 资产本身零 Tile。
- 编辑器迁移工具只执行一次：创建空白 Prefab、实例化、复制旧 Tile、配置 Override、保存场景，然后删除工具。
- 进入 Play Mode 后，控制器扫描实例当前手工 Tile 并缓存为源格；之后 SetTilesBlock 写回属于运行时模拟，Play Mode 手工擦除会被覆盖。
- 空白实例未绘制任何水格时明确报错并禁用，不自动创建水源。

## 风险

- Unity 会把场景实例上的 Tilemap 数据记录为 Prefab Override；Apply Overrides 时不得把关卡专用水格误写回空白 Prefab 资产。
- 每个实例必须手动配置 GroundTilemap、SideWallTilemap 和移动阻挡，否则启动时报配置错误。
