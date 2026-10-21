## Why

第一幕俯视管网当前要求为起点、中间和终点分别选择带方向数据的Route Tile，并额外绘制Source Tilemap。相同连续水路被拆成多种Tile和多张Tilemap，容易出现漏格、方向选错和水源错位；当前第一幕接线已经暴露了这种配置成本。

## What Changes

- **BREAKING**：Route从“每格Tile声明进入/离开方向”改为“整条路线只使用一个 `PipeRouteMarker` Tile”。
- **BREAKING**：删除Source Tilemap依赖；每个 `Demo2PipeWaterController` 通过一个序列化 `sourceCell` 声明唯一水源格。
- 控制器根据Marker格的上下左右相邻关系自动推导路线端口；没有Marker的格仍不得成为管网或合法放置格。
- 水从唯一水源沿相邻Marker形成的无向网络传播；管段占用、双方端口、阻挡和逐格时序继续共同限制实际供水。
- 保留独立Water Tilemap作为运行时输出，避免水流显示覆盖关卡作者绘制的路线数据。
- 删除不再需要的 `Demo2WaterRouteTile` 方向数据类型和三个方向Route资产，新增一个普通路线Marker资产。
- 迁移 `demo2_act1` 的控制器引用和现有路线，不改变路线坐标、3米目标Slot、管段搬运、双视图投影或SideView地形水。
- 更新纯模型与控制器验证，使直线、转弯、分支、错误水源、缺管、错误管口和断开重流都有明确结果。

## Capabilities

### New Capabilities

- `demo2-carryable-pipes`: 定义TopView E拾取、单件搬运、四方向前方预览、合法放置、动作互斥和实体恢复合同。
- `demo2-water-network`: 定义第一幕单一Marker路线、唯一水源、水管端口、阻挡、管内逐格传播及拓扑重算合同。

### Modified Capabilities

- 无。

## Impact

- 修改 `Demo2PipeWaterModel` 与 `Demo2PipeWaterController` 的路线和水源数据契约。
- 删除 `Demo2WaterRouteTile` 及 `Route_Source_Right`、`Route_Right`、`Route_End_FromLeft` 资产；新增一个 `PipeRouteMarker` Tile及对应 `.meta`。
- 更新相关EditMode/PlayMode测试代码、`demo2_act1` 场景引用和路线Tile内容。
- `Spotlight.Game`、`Spotlight.Tests.EditMode`、`Spotlight.Tests.PlayMode` 程序集边界不变，不新增包或全局服务。

## 主要决策与风险

- 路线图改为无向正交邻接图；从水源可达的分支都会尝试供水。这样才能用同一个Marker表达直线、转弯和分支，但不再支持用Route Tile单独限制单向流动。
- Water Tilemap继续独立；把路线和运行时水共用一张Tilemap虽然对象更少，却会覆盖作者数据并使拓扑重算依赖缓存，拒绝采用。
- `sourceCell` 若未绘制Marker必须明确报错并禁用控制器，不得静默选择第一个路线格。

## 非目标

- 不合并TopView管道水与SideView `TerrainWaterController`。
- 不实现自动管道生成、管道旋转、真实流体、液面物理或开放管口向地面扩散。
- 不改变玩家输入、搬运距离、Slot规则、转场、Cinemachine、树苗或怪物行为。
- 不把Water Tilemap与Route Tilemap合并为同一张运行时可写Tilemap。

## SideView水状态投影修订

- `Demo2PipeWaterController` 在实际Water Tilemap完成初始化或变化后发布水状态变化事实。
- 新增第一幕侧视水投影组件；每个绑定用一组TopView网格格和一个SideView视觉对象表达“这些格全部有水时显示”。
- SideView首次启用时必须主动读取当前权威水状态，因此玩家尚未切换TopView时，水源格对应的侧视水视觉已经显示。
- SideView停用期间不维护第二份状态；重新启用时重新查询，避免依赖停用期间接收事件。
- 本修订不改变水传播、Slot判定、管道搬运、视角切换和SideView固定管道外壳。


## 第一幕完整直线管网修订

- 把 `demo2_act1` 的Route从4格扩展为 `(0..9,-2)` 十格连续Marker。
- 保留 `(0,-2)` 为唯一水源；新增两根固定3米直管，分别占据 `(1..3,-2)` 与 `(7..9,-2)`。
- 把可搬运3米管目标Slot迁移到 `(4..6,-2)`，形成唯一缺口。
- 第一幕共享管段初始化数组包含两根固定管和一根可搬运管；TopView初始停用时仍能完成固定管登记。
- 树的水检测点对齐终点格 `(9,-2)`；水填满连续管网后触发现有发芽逻辑。
- 本修订不新增L形/竖管资源，不实现持续通水3秒后的最终成熟、对话或过关流程。

