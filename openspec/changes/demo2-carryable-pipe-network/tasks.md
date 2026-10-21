## 1. OpenSpec确认

- [x] 1.1 核对现有SideView地形水、玩家输入和资源边界
- [x] 1.2 确认第一幕为TopView且水只在水管内流动
- [x] 1.3 确认独立方向Tilemap、开放管口停止和暂不做俯视推动
- [x] 1.4 确认1米最近拾取、禁止旋转、禁止携带切视角和前方放置
- [x] 1.5 确认预览只判断物理合法性，错误端口可放但不通水
- [x] 1.6 确认放置后实体碰撞与Agent仅负责脚本的交付边界
- [x] 1.7 重写proposal、spec、design、plan和tasks俯视方案
- [x] 1.8 严格校验OpenSpec并获得代码实施确认

## 2. 水查询与有向管网

- [x] 2.1 新增统一世界坐标水查询基类并迁移Terrain实现
- [x] 2.2 新增上下左右端口、反向查询和90度旋转规则
- [x] 2.3 新增带进入离开方向的Route Tile资产类型
- [x] 2.4 新增PipeWaterModel路线、水源、管占格和阻挡状态
- [x] 2.5 实现路线与管端口共同约束的逐格传播
- [x] 2.6 实现缺管、错误端口和开放管口停止
- [x] 2.7 实现拓扑改变后的断开清水和源端重流

## 3. 水管与搬运脚本

- [x] 3.1 新增水管Shape资产类型和格定义校验
- [x] 3.2 新增PipeSegment登记、移除和实体碰撞切换
- [x] 3.3 新增CarryablePipe标识和最近目标筛选
- [x] 3.4 新增四方向前方网格物理合法性查询
- [x] 3.5 新增红绿精确占格Preview驱动
- [x] 3.6 新增Demo2/Interact按名称查找和E拾取放置流程
- [x] 3.7 保持水管authored朝向并禁止运行时旋转

## 4. 玩家动作互斥

- [x] 4.1 记录TopView最后四方向移动朝向
- [x] 4.2 搬运期间保留俯视四方向移动
- [x] 4.3 搬运期间忽略攻击并拒绝F推动
- [x] 4.4 搬运期间拒绝R视角切换
- [x] 4.5 SideView、冻结、攻击和受击状态拒绝拾取

## 5. Pipe Water Controller

- [x] 5.1 捕获Route Tilemap、Source Tilemap和网络边界
- [x] 5.2 接入管段登记移除和占格冲突查询
- [x] 5.3 接入静态动态阻挡和允许放置区域查询
- [x] 5.4 渲染有水管格并实现世界坐标水查询
- [x] 5.5 保持现有SideView Terrain水流行为不变

## 6. 验证与交付

- [x] 6.1 添加路线方向、端口旋转、缺管停止和重流EditMode测试
- [x] 6.2 添加四方向朝向、红绿预览和动作互斥测试代码
- [x] 6.3 编译生产、EditMode和PlayMode程序集
- [ ] 6.4 按用户许可执行水管目标自动测试
- [x] 6.5 提供Input、Route、Source、Shape、Prefab、Preview和场景接线清单
- [ ] 6.6 项目负责人完成第一幕TopView短直管垂直切片烟测

## 当前验证记录

- `demo2-carryable-pipe-network` OpenSpec严格校验通过。
- `Spotlight.Game`、`Spotlight.Tests.EditMode` 和 `Spotlight.Tests.PlayMode` 编译通过：0个警告、0个错误。
- 按用户要求未执行Unity自动测试；测试代码只完成编译。
- Agent未修改Input Actions、Tile、Shape资产、Prefab或场景；Editor接线与第一幕烟测由项目负责人完成。

## 7. 单一路线Tile修订确认

- [x] 7.1 确认使用单一Marker Tile、唯一sourceCell和独立Water Tilemap
- [x] 7.2 更新proposal、spec、design、plan和水网能力合同
- [x] 7.3 严格校验修订后的OpenSpec
- [x] 7.4 获得用户对修订文档的代码实施确认

## 8. 单一路线Tile实施

- [x] 8.1 将纯模型路线数据收敛为双向端口掩码
- [x] 8.2 从Marker正交邻接捕获路线并校验唯一sourceCell
- [x] 8.3 移除Source Tilemap和自定义方向Route读取路径
- [x] 8.4 更新Marker直线转弯分支与失败边界测试代码
- [x] 8.5 创建PipeRouteMarker并迁移demo2_act1场景
- [x] 8.6 删除Demo2WaterRouteTile和三个旧方向资产
- [x] 8.7 编译生产、EditMode和PlayMode程序集
- [ ] 8.8 按用户许可执行管网目标自动测试
- [x] 8.9 项目负责人烟测3米Slot、逐格通水和双视图状态

## 修订验证说明

- 已完成代码、`PipeRouteMarker`资产和`demo2_act1`迁移；旧方向类型、三个方向资产和Source Tilemap已删除。
- `Spotlight.Game`、`Spotlight.Tests.EditMode`、`Spotlight.Tests.PlayMode`编译均为0警告、0错误。
- 非Test Runner的Play Mode烟测通过：Controller已配置，水源为`(0,-2,0)`，`(0..3,-2,0)`四个Marker格合法，`(4,-2,0)`保持非法。
- Unity自动测试继续保持未执行，除非用户明确许可。

## 9. SideView初始水同步

- [x] 9.1 确认使用共享权威水状态、主动初始查询和显式视觉绑定
- [x] 9.2 更新并严格校验SideView水同步OpenSpec
- [x] 9.3 发布管网水状态变化事实
- [x] 9.4 实现多格SideView水视觉投影
- [x] 9.5 接入第一幕初始水源视觉
- [x] 9.6 编译生产、EditMode和PlayMode程序集
- [x] 9.7 Play Mode烟测初始与返回SideView同步
- [x] 9.8 记录Unity自动测试保持未执行

## SideView水同步验证记录

- 三个程序集编译通过：0个警告、0个错误。
- 非Test Runner的Play Mode烟测通过：开局SideView水视觉显示、成功切到TopView、返回SideView后水视觉按权威状态恢复。
- `InitialWaterVisual` 当前用 `Wave.asset` 覆盖SideView `(0,-2)` 与 `(1,-2)` 两格；绑定读取TopView水源格 `(0,-2,0)`。
- 按用户既有要求未运行Unity Test Runner。



## 10. 第一幕完整直线管网

- [x] 10.1 确认十格路线、两根固定管和中间3米缺口
- [ ] 10.2 更新并严格校验完整管网OpenSpec
- [ ] 10.3 创建FixedPipe_Straight3 Prefab
- [ ] 10.4 扩展Route并放置两根固定管
- [ ] 10.5 迁移目标Slot和三管初始化数组
- [ ] 10.6 对齐树水检测点到终点格
- [ ] 10.7 编译生产、EditMode和PlayMode程序集
- [ ] 10.8 Play Mode烟测缺口停水与补全后发芽
- [ ] 10.9 记录Unity自动测试保持未执行


