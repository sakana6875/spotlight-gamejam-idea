## Purpose

定义第一幕TopView有限1米网格中单一Marker路线、唯一水源、固定及可搬运水管、实体阻挡之间的管内供水规则，使关卡作者无需手工配置每格方向或额外Source Tilemap。

## ADDED Requirements

### Requirement: 单一Marker Tile定义正交水路

第一幕Route Tilemap MUST 只使用一个指定的 `PipeRouteMarker` Tile声明合法管网格。上下左右相邻的Marker格 MUST 自动形成双向路线连接；斜角接触不得连接。未绘制Marker的格不得成为管内水路或合法放置格。

#### Scenario: 相邻Marker形成转弯

- **WHEN** 当前Marker格的上方和右侧各存在Marker格
- **THEN** 当前格自动拥有向上和向右的路线连接，不需要更换或旋转Tile

#### Scenario: 斜角Marker不连接

- **WHEN** 两个Marker格只在角点接触且没有正交Marker连接
- **THEN** 两格之间不存在可传播连接

#### Scenario: Route混入其他Tile

- **WHEN** Route Tilemap任一非空格不是配置的 `PipeRouteMarker`
- **THEN** 控制器报告该格配置错误并禁用，不得把它当作路线

### Requirement: 控制器声明唯一水源格

每个管网控制器 MUST 通过一个稳定的 `sourceCell` 声明唯一水源。水源格 MUST 已绘制Marker，可以没有水管，并持续保持供水；系统不得依赖Source Tilemap，也不得自动选择其他路线格作为水源。

#### Scenario: 合法水源

- **WHEN** `sourceCell` 对应一个Marker格且该格未被阻挡
- **THEN** 该格持续有水并可向所有满足连接条件的相邻Marker格供水

#### Scenario: 水源缺少Marker

- **WHEN** `sourceCell` 没有配置的Marker Tile
- **THEN** 控制器输出包含该坐标的错误并禁用，Water Tilemap不得显示假水源

### Requirement: 水离开水源后只在匹配水管中传播

除水源格外，Marker格 MUST 只在实际被固定或可搬运水管占据，且相邻水管双方端口覆盖Marker邻接方向时变为有水。普通地面、空Marker格和端口不匹配的管格不得继续传播水。

#### Scenario: 水管补全Marker路线

- **WHEN** 一组已放置水管占据从水源可达的连续Marker格，且每对相邻管端口互相匹配
- **THEN** 水从水源按步长逐格流入这些水管

#### Scenario: 路线上缺少水管

- **WHEN** Marker路线下一格没有任何管段占据
- **THEN** 水停在最后一个已通水的水源或管格

#### Scenario: 水管端口错误

- **WHEN** 两格Marker正交相邻，但占据格的水管缺少对应入口或出口
- **THEN** 水不得穿过错误水管

### Requirement: 开放管口不向地面扩散

水到达水管开放出口时，如果下一格缺少Marker、匹配水管或反向端口，MUST 停在当前管格。系统不得在管外创建自由水格。

#### Scenario: 管口前方为空地

- **WHEN** 已通水管的出口指向未绘制Marker或没有水管的相邻格
- **THEN** 当前管格保持有水，前方地面保持无水

### Requirement: 阻挡格中断管内水流

墙、石头或配置的动态阻挡占据Marker或水管格时，该格 MUST 暂时不可供水。

#### Scenario: 石头占据已拼接路线

- **WHEN** 石头阻挡一个原本端口匹配的管格
- **THEN** 水不得进入该格，后续与水源断开的管格清除水状态

### Requirement: 水按固定步长逐格传播

新连通的管道水 MUST 从水源开始按配置步长逐层扩展，不得在单个物理步瞬间填满整条路线。

#### Scenario: 分支路线恢复连接

- **WHEN** 玩家放置水管首次补全水源到多个分支的连接
- **THEN** 水按网格距离逐步进入每个可达分支

### Requirement: 拓扑改变清除断开的旧水

水管拾取、重新放置或阻挡改变连接时，系统 MUST 重新计算水源可达区域。与唯一水源断开的管格不得继续保持有水；恢复连接后从水源按步长重新供水。

#### Scenario: 拾取连接中的水管

- **WHEN** 玩家从已通水路径中拾取一根水管
- **THEN** 被拾取管格及所有下游断开管格清除水状态

### Requirement: 路线作者数据与水显示分离

Route Tilemap MUST 保持只读作者数据；独立Water Tilemap MUST 只显示运行时有水格。运行时不得用Water Tile覆盖或清除Route Marker。

#### Scenario: 水流重算

- **WHEN** 拓扑变化导致Water Tilemap清除并重新绘制水格
- **THEN** Route Tilemap的全部Marker保持不变，后续放置与拓扑判断仍读取同一作者路线

### Requirement: 两种水系统共享世界坐标查询合同

现有SideView地形水和TopView管道水 MUST 向树苗等目标提供相同的世界坐标有水查询能力，但各自保持独立模型和场景配置。

#### Scenario: 管道水到达树苗检测点

- **WHEN** TopView管道水使树苗检测点所在管格变为有水
- **THEN** 统一世界坐标查询返回真，树苗可以触发现有成长流程

### Requirement: 管网水变化提供只读事实通知

`Demo2PipeWaterController` MUST 在初始化水源或后续水状态实际重新渲染后发布水状态变化事实。订阅者 MUST 通过公开水查询读取结果，不得获得或修改模型内部数组。

#### Scenario: 水源初始化

- **WHEN** Controller在Awake中建立唯一水源并完成首次Water Tilemap渲染
- **THEN** 当前水状态可立即由任意SideView投影通过格查询读取

#### Scenario: 拓扑变化后重流

- **WHEN** 放置、拾取或阻挡变化使Water Tilemap重新渲染
- **THEN** 活动订阅者收到变化事实并重新查询所需格

### Requirement: SideView首次显示时同步当前水状态

SideView水投影 MUST 在首次Start和每次OnEnable主动查询权威Controller。玩家尚未切换TopView时，已处于有水状态的源格对应视觉 MUST 显示；初始化不得依赖事件订阅先于Controller Awake。

#### Scenario: 第一幕从SideView开始

- **WHEN** 第一幕以SideView启动且TopView水源格 `(0,-2,0)` 已由Controller设为有水
- **THEN** 对应该格的 `InitialWaterVisual` 在首帧初始化后显示

#### Scenario: 返回SideView

- **WHEN** SideView停用期间TopView水状态发生变化，随后玩家返回SideView
- **THEN** 投影在OnEnable查询最新状态并显示正确视觉，不要求补收历史事件

### Requirement: 多格绑定只在全部有水时显示

每个SideView水视觉绑定 MUST 声明至少一个Required Cell和一个Wet Visual。仅当全部Required Cell当前有水时Wet Visual才显示；任一格无水时必须隐藏。配置缺失、空格数组或同一组件重复Wet Visual MUST 产生可观察错误并禁用投影。

#### Scenario: 初始单格水源绑定

- **WHEN** 绑定只要求水源格且该格有水
- **THEN** 对应SideView水覆盖层显示

#### Scenario: 多格管段尚未全湿

- **WHEN** 多格绑定中至少一个格尚未进水
- **THEN** 对应SideView水覆盖层保持隐藏


### Requirement: 第一幕连续固定管与可搬运缺口共享管网

第一幕01 MUST 使用同一个 `Demo2PipeWaterController` 登记固定管和可搬运管。Route MUST 连续覆盖 `(0..9,-2)`；水源为 `(0,-2)`；固定3米管 MUST 占据 `(1..3,-2)` 与 `(7..9,-2)`；唯一目标缺口 MUST 为 `(4..6,-2)`。

#### Scenario: 开局缺少中间管

- **WHEN** 两根固定3米管已经登记且目标可搬运管尚未占据 `(4..6,-2)`
- **THEN** 水从源头逐格进入固定管A并停在 `(3,-2)`，格4至9保持无水

#### Scenario: 可搬运管补全连续链

- **WHEN** 玩家把 `PipeShape_Straight3` 以0圈朝向放到Origin `(4,-2)`
- **THEN** 管端口在3→4和6→7两处匹配，水从源头重新逐格传播到 `(9,-2)`

### Requirement: 管网终点驱动树苗发芽

第一幕树水检测点 MUST 位于Route格 `(9,-2)`中心，并查询同一个 `Demo2PipeWaterController`。终点首次有水时 MUST 请求现有树Controller开始发芽；树的SideView显示位置不得替代TopView终点检测坐标。

#### Scenario: 连续管网到达终点

- **WHEN** `(9,-2)`首次变为有水且树仍处于Dormant
- **THEN** 树进入Sprouting并播放现有0.8秒发芽动画

