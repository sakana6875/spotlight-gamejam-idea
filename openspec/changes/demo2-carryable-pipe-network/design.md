## Context

第一幕TopView管道水已经实现为有限网格模型：Route Tilemap用自定义Tile保存每格进入/离开方向，Source Tilemap另行标记水源，Water Tilemap显示运行时结果。当前第一幕只需要作者绘制合法管网区域，实际连通仍由水管端口决定；方向Route和独立Source绘制重复表达了同一条路线，并已造成漏格、终点类型和引用错误。现有SideView `TerrainWaterController` 已用于树苗与史莱姆流程，必须保持不变。

## Goals / Non-Goals

**Goals:**

- 用一个Marker Tile完成TopView路线绘制，支持直线、转弯和分支。
- 用控制器上的唯一 `sourceCell` 取代Source Tilemap。
- 保留管段双方端口、阻挡、逐格传播和拓扑断开清水规则。
- 保留独立Water Tilemap，使作者数据与运行时输出职责分离。
- 迁移当前第一幕场景，清理不再有调用方的方向Tile代码和资产。

**Non-Goals:**

- 不合并TopView管道水与SideView地形水。
- 不把Route和Water合并到同一Tilemap，不实现运行时路线编辑。
- 不实现单向阀、管道旋转、自由水扩散、流体物理或完整第一幕内容。
- 不修改玩家参数、战斗、树苗、怪物或双视图规则。

## Decisions

### 1. PipeWaterController继续持有唯一TopView水模型

`Demo2PipeWaterModel`继续使用预分配有限数组保存路线端口、管占用端口、阻挡、水源和有水状态。`Demo2PipeWaterController`负责捕获作者路线、登记管段、采样阻挡、逐步推进和渲染，不引入新服务或程序集。

保留独立模型能避免把“SideView地形上自由向右流”和“TopView只在管内传播”塞入同一类型，也保护已烟测的水树流程。

### 2. 用抽象MonoBehaviour表达世界坐标水查询边界

`Demo2WaterPresenceSource` 继续公开 `IsWaterAtWorldPosition(Vector2)`。`TerrainWaterController` 与 `Demo2PipeWaterController` 分别实现；本次只迁移TopView内部作者数据，不改变调用方。

### 3. 单一Marker和唯一sourceCell取代方向Tile与Source Tilemap

Route Tilemap只允许绘制序列化 `_routeMarkerTile` 指向的同一个普通Tile资产。控制器在Awake遍历有Marker的格，并按上下左右相邻Marker生成该格的路线端口掩码。斜角接触不连接；没有Marker的格不属于网络。

控制器序列化一个 `_sourceCell`。该格必须存在Marker，可以没有水管，并持续作为唯一供水起点。空路线、非Marker Tile或水源格缺少Marker时输出具体错误并禁用控制器，不做自动修正。

`Demo2WaterRouteTile`、每格Incoming/Outgoing数据和Source Tilemap引用全部删除；不保留兼容别名或双路径。

### 4. 管Shape继续声明本地占格和双向端口

`Demo2PipeShape` 每格定义本地偏移和双向 `Demo2WaterPort`。`Demo2PipeSegment` 从Transform Z读取 authored 的90度倍数朝向，把偏移和端口旋转后登记到模型。运行时不提供旋转。

固定管只挂Segment；可搬运管额外挂 `Demo2CarryablePipe`。拾取移除登记并禁用实体Collider，放置重新登记并恢复Collider。

### 5. 传播同时要求Marker邻接和管端口

从当前有水格向方向 `d` 传播必须同时满足：

1. 当前Marker格与相邻Marker格正交相邻，双方路线掩码包含对应端口；
2. 相邻格由管段占据；
3. 当前格若非水源也由管段占据；
4. 当前管有 `d` 端口，相邻管有反向端口；
5. 两格均未被阻挡。

任一条件失败即停在当前格。路线图是无向图，水从唯一水源向所有满足条件的可达分支逐步扩展；模型不创建Marker外自由水。

### 6. Water Tilemap保持独立输出

Water Tilemap只由控制器写入统一Water Tile，初始应为空。Route Marker不会被运行时替换，因此放置合法性、世界坐标换算和拓扑重算始终读取稳定作者数据。把两者合并虽然减少一个Tilemap对象，却要求缓存和恢复被覆盖的路线，失败时会丢失关卡合同，因此拒绝。

### 7. 搬运、预览和动作互斥保持既有合同

放置预览继续检查全部Shape格是否属于允许路线并且没有物理冲突；实现改为检查Marker而非自定义方向Tile。端口是否能通水仍不影响绿色预览，保留拼图试错。E拾取、四方向携带、攻击/F/R互斥和Prefab authored朝向保持不变。

## Risks / Trade-offs

- [相邻Marker自动形成连接，无法再用Tile表达单向限制] → 第一幕当前不需要单向阀；若以后出现真实单向玩法，应作为独立管件能力而不是恢复方向地板Tile。
- [误刷相邻Marker会创建意外分支] → 只接受同一个Marker资产，并用直线、转弯、分支测试和场景烟测验证实际拓扑。
- [`sourceCell` 手填错误] → 启动时强校验该格存在Marker，输出坐标并禁用控制器。
- [删除旧资产导致场景Missing引用] → 先迁移代码和测试，再通过Unity Editor迁移场景，确认无引用后删除旧类型和资产。
- [自动测试未获许可] → 完成针对性编译和实际场景烟测；目标自动测试保持未执行记录，除非用户明确允许。

## Migration Plan

1. 更新纯模型，把Incoming/Outgoing两组路线数据收敛为一个双向路线端口掩码，并保留管口、阻挡和传播规则。
2. 更新控制器，增加单一Marker引用与唯一 `sourceCell`，从正交邻接捕获路线，移除Source Tilemap和自定义Route读取。
3. 更新行为测试覆盖直线、转弯、分支、非Marker、错误水源、缺管和断开重流。
4. 创建 `PipeRouteMarker` 普通Tile资产，通过Unity Editor把 `demo2_act1` 路线全部替换为该Tile并迁移控制器引用。
5. 删除 `Demo2WaterRouteTile` 和三个旧方向Route资产，确认无Missing引用。
6. 编译三个程序集；按既有约束不擅自运行Unity自动测试。
7. 在Unity Editor烟测错误位置、正确3米Slot、逐格通水和双视图状态保留。

## SideView水状态投影修订

### 8. Water Controller发布无负载状态变化事实

`Demo2PipeWaterController` 继续是唯一管网水状态权威。每次 `RenderWater` 将最新模型写入Water Tilemap后发布 `WaterStateChanged`；消费者收到事实后通过现有 `IsWaterAtCell` 查询所需格，不复制模型数组或持有可写水状态。

初始化事件可能早于SideView订阅，因此事件不是初始化数据源。侧视投影必须在 `Start` 和每次 `OnEnable` 主动刷新；事件只减少活动期间无意义的逐帧查询。

### 9. SideView用显式多格绑定投影视觉

新增 `Demo2Act1SideWaterProjection` 和可序列化 `Demo2SideWaterVisualBinding`。每个绑定声明非空 `requiredCells` 与唯一 `wetVisual`；全部格当前有水时显示视觉，否则隐藏。一个TopView水源格可以映射到任意SideView美术对象，不要求两套视图使用同一坐标或Tilemap。

组件只控制水覆盖层，不控制 `StaticPipeVisual` 固定管道外壳，也不取代现有 `Demo2Act1SidePipeProjection` 的目标Slot干/湿状态。当前第一幕初始绑定读取 `(0,-2,0)`，驱动独立 `InitialWaterVisual`。

### 10. 停用视图通过重查恢复一致性

SideView停用时投影组件解除订阅；TopView中的水仍由共享Controller推进。SideView再次启用时立即查询所有绑定，因此不会要求不可见对象接收并缓存每次水变化事件。

### 风险补充

- [水控制器Awake早于投影订阅] → `Start` 主动刷新，初始显示不依赖事件顺序。
- [SideView停用期间错过变化] → 每次 `OnEnable` 重新查询权威状态。
- [多个组件争用同一视觉对象] → 配置校验拒绝空引用和同一组件内重复Wet Visual；场景把初始水覆盖层独立于Slot Dry/Wet视觉。


## 第一幕完整直线管网修订

### 11. 固定管复用PipeSegment，不引入第二套类型

新增 `FixedPipe_Straight3.prefab`，复用 `PipeShape_Straight3`、实体Collider和 `Demo2PipeSegment`，但不挂 `Demo2CarryablePipe`。固定与可搬运管通过同一登记合同相邻连接，模型不区分所有权类型。

### 12. 十格路线只保留一个玩家缺口

路线使用十个连续Marker。水源格0不要求管道；固定管A占1至3，可搬运目标占4至6，固定管B占7至9。相邻边界3→4与6→7只有双方Left/Right端口匹配时传播。

开局固定管先登记，水在缺口前停于格3。玩家把3米管放到Origin `(4,-2)`后，拓扑重置并从水源逐层重流，最终格9变湿。

### 13. 树检测点跟随管网终点而非SideView树位置

`Act1TreeWaterTarget` 位于Route格 `(9,-2)`中心，并继续由 `Demo2TreeWaterTrigger` 查询。树的SideView世界位置不参与TopView格换算。终点变湿只触发现有 `BeginSprouting`；最终成熟仍由后续第一幕流程控制器负责。

### 风险补充

- [固定管未在TopView激活前登记] → `Act1PipePuzzle.TopViewPipeSegments` 显式包含三根管并统一调用幂等初始化。
- [目标Slot或树检测点仍使用旧坐标] → Editor迁移同时设置Slot Origin为4、Water Target为格9中心，并通过Play Mode验证停水和到达。

