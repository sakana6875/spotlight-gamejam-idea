# Demo2 自动水源与地形水道技术设计

## 上下文

`TerrainWaterArea` 预制体实例保留其 `WaterInteriorTilemap`、阻尼 Trigger 和运行组件。场景 `SideView` 同级 `GroundTilemap` 是水道覆盖的权威数据；`SideWallTilemap` 与移动 Collider2D 继续仅定义阻挡。水从手工源格开始可见地向右扩张、在有限障碍前蓄积并可越过其顶端；水既不向下流，也不改变刚体速度以外的物理状态。

## 决策

### 1. 扫描全部手工水源

`WaterTilemapRenderer` 在 `WaterInteriorTilemap.cellBounds` 中检查每个 Tile，仅以 `GetTile(cell) != null` 识别源格。它返回包围所有源格的 `BoundsInt`，并将所有原始 Tile 缓存在与推导水道域等大的 `TileBase[]` 中。空 Tilemap 被控制器视为配置错误；运行时扩张格不会在首次扫描前污染源格。

### 2. GroundTilemap 定义有限水道域

控制器先扫描源格，再扫描 `GroundTilemap`。空 GroundTilemap 是配置错误。域的下界是最低源行，上界是最高源行和最高地面占据行的较大值；横向从最低源列延至最高有地面覆盖列。每个候选水格必须在同列具有严格位于该格下方的地面 Tile，否则在模型中被标为阻挡。每个源格也必须可用，避免看似启用但无法供水的悬空源水。

### 3. 逐步采样边界与同 Tilemap 渲染

`TerrainWaterModel` 继续使用有限的预分配数组。控制器使用推导 BoundsInt 建立模型，首次只渲染源格。每个物理步重新采样地面覆盖、SideWallTilemap 和移动 Collider2D；缺少地面覆盖的格与墙体和箱子一样标为阻挡。渲染器保留源格的原始 Tile，非源的占据格写入既有 `_waterTile`，域内其余格清空。动画相位使用相对于该 BoundsInt 的列索引，保持水纹右向视觉而不改变模型或物理。

### 4. 自动配置阻尼候选范围

`WaterDampingArea` 必须与根节点的 `BoxCollider2D` 绑定，并要求它是 Trigger。控制器在成功推导水道域后传入 WaterInteriorTilemap 和 BoundsInt。阻尼组件将域最小和最大单元的四角转换到 Trigger 根节点局部坐标后计算 Box 的 offset 与 size。Box 仅收集候选刚体；每个物理步仍调用 `IsWaterAtWorldPosition`，只有实际占水格内且有速度的刚体接受不变的 `-velocity * damping` 力。

## 风险与限制

- 域由启动时的手工源与静态地面范围推导；不支持动态地形重建。
- 地面支持不要求紧邻下方，适配当前源水高于地面的关卡布局。
- 仍不支持下落、瀑布、完整压力传播、浮力或推力。
- Unity Editor 必须保存两个现有水根的 `GroundTilemap` 实例引用；不手改场景 YAML。
