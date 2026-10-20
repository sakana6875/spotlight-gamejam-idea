## 实施顺序

1. 保留现有 `Demo2WaterPresenceSource` 世界坐标查询边界和SideView Terrain实现。
2. 修改 `Demo2PipeWaterModel`，把每格Incoming/Outgoing路线收敛为根据Marker邻接得到的双向端口掩码；保留唯一水源、管端口、阻挡、有水状态和逐格传播。
3. 修改 `Demo2PipeWaterController`：序列化Route Tilemap、唯一Marker Tile、`sourceCell`、Water Tilemap和Water Tile；删除Source Tilemap依赖。
4. 捕获作者网络时只接受指定Marker Tile，根据上下左右相邻Marker生成端口；空路线、混入其他Tile或水源不在Marker格时明确报错并禁用。
5. 保持 `Demo2PipeShape`、`Demo2PipeSegment`、搬运、预览、Slot和双视图逻辑；放置合法性改为检查Marker格。
6. 更新纯模型和控制器测试，覆盖直线、转弯、分支、斜角不连接、错误水源、非Marker、缺管、端口错误、阻挡和断开重流。
7. 创建一个普通 `PipeRouteMarker` Tile资产和 `.meta`。
8. 通过Unity Editor迁移 `demo2_act1`：Route全部改刷Marker、删除Source Tilemap对象、配置 `sourceCell`、保留独立Water Tilemap和现有路线坐标。
9. 删除 `Demo2WaterRouteTile`、三个旧方向Route资产及对应 `.meta`，检查场景无Missing引用。
10. 编译 `Spotlight.Game`、`Spotlight.Tests.EditMode` 和 `Spotlight.Tests.PlayMode`；Unity自动测试仅在用户明确许可后执行。
11. 实际烟测第一幕错误放置、正确3米Slot、逐格供水、断开重流和双视图状态保留。

## 目录与程序集

- `Assets/Scripts/Demos/Demo2/Water/`：水存在查询抽象与现有Terrain实现，本次不改行为。
- `Assets/Scripts/Demos/Demo2/Pipe/`：纯管网、场景控制器、管段、Shape、搬运和预览；删除方向Tile类型。
- `Assets/Data/Demo2/`：保留一个 `PipeRouteMarker` Tile，删除三个方向Route资产。
- `Assets/Scenes/demo2_act1.unity`：迁移Route、Source和控制器序列化引用。
- 全部仍属于 `Spotlight.Game`，不新增程序集、包或全局服务。

## 最终Editor接线

1. `PipeRouteTilemap` 全部合法路线格只刷同一个 `PipeRouteMarker` Tile。
2. `Demo2PipeWaterController.Route Marker Tile` 连接该资产。
3. `Source Cell` 填写路线起点坐标；该格必须已刷Marker，不要求管段占据。
4. `PipeWaterTilemap` 保持独立并在编辑态为空；`Water Tile` 继续作为运行时显示。
5. 不再创建或连接 `PipeSourceTilemap`。
6. 管段、Act1 Slot、SideProjection、Player和ViewMode引用保持既有合同。

## 验证路径

- OpenSpec：`openspec validate demo2-carryable-pipe-network --strict`。
- 编译：`dotnet build Spotlight.Game.csproj --no-restore`、EditMode和PlayMode对应工程。
- 纯规则：Marker直线/转弯/分支邻接、斜角不连接、唯一水源、管端口匹配、缺管停止、开放管口停止、逐格传播、断开清水、恢复重流。
- 配置失败：Route混入其他Tile或 `sourceCell` 没有Marker时记录具体错误并禁用。
- Editor烟测：TopView E拾取；错误位置可放但不通；正确3米管逐格通水；拾管断水；放回恢复；切换Side/Top后状态不重置。

## SideView初始水同步实施

12. `Demo2PipeWaterController` 在完成Water Tilemap渲染后发布 `WaterStateChanged` 事实。
13. 新增 `Demo2SideWaterVisualBinding`，校验非空Required Cells和Wet Visual。
14. 新增 `Demo2Act1SideWaterProjection`，在Start、OnEnable和水变化事件后刷新视觉，并在OnDisable解除订阅。
15. 在 `demo2_act1` 的SideView新增独立 `InitialWaterVisual`，绑定TopView水源格 `(0,-2,0)`；固定管道仍由 `StaticPipeVisual` 显示。
16. 编译生产、EditMode和PlayMode程序集；不擅自运行Unity Test Runner。
17. Play Mode烟测从SideView开局即可看到初始水，切换TopView后水模型不重置，返回SideView读取最新状态。


## 第一幕完整直线管网实施

18. 从现有3米可搬运Prefab派生 `FixedPipe_Straight3.prefab`，移除搬运标识，保留Shape、Collider和Segment。
19. Route Tilemap统一绘制 `(0..9,-2)` Marker，保持Source Cell `(0,-2)`。
20. 在TopView放置固定管A Origin `(1,-2)`、固定管B Origin `(7,-2)`，连接共享Water Controller。
21. 把目标Slot Origin迁移为 `(4,-2)`，并把两根固定管与可搬运管全部加入TopView管段数组。
22. 把 `Act1TreeWaterTarget` 放到Route格 `(9,-2)`中心并保持树触发器引用。
23. 编译三个程序集；Play Mode验证开局停在格3、放入缺口后到达格9并触发Sprouting；不运行Unity Test Runner。

