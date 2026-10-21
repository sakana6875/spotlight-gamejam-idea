## 可观察合同

```text
第一幕视角          TopView
交互键              Demo2/Interact，由项目负责人绑定E
拾取范围            玩家中心1米内最近可搬运水管
允许状态            TopView、未冻结、Idle或Moving
同时搬运            最多一根
允许动作            WASD四方向移动
禁止动作            攻击、F推动、R切视角、跳跃
旋转                不允许，保持Prefab authored朝向
放置位置            最后移动朝向前方最近1米网格
合法预览            全部占格存在PipeRouteMarker且物理占格合法时半透明绿色
非法预览            缺少Marker、越界或重叠时半透明红色
端口错误            仍可绿色放置，但不会通水
放置后              恢复实体Collider并登记管网
固定路线            Route Tilemap全部路线格使用同一个PipeRouteMarker Tile
路线连接            上下左右相邻Marker自动形成双向路线端口
水源                Controller序列化唯一sourceCell；不再绘制Source Tilemap
水流显示            独立Water Tilemap运行时输出
水流范围            水源和实际占据路线的匹配水管格
开放管口            停在最后有水管格，不流到地面
拓扑改变            清除断开旧水并逐格重流
俯视推动            本次不实现
```

## 失败合同

- Route、Marker、Water Tilemap、Water Tile或 `sourceCell` 缺失/无效时，控制器 MUST 输出具体错误并禁用。
- `sourceCell` MUST 位于一个 `PipeRouteMarker` 格；系统不得自动选择或修正水源。
- Route Tilemap中出现非Marker Tile时 MUST 作为配置错误报告，避免普通美术Tile被误认为合法管网。

## 交付边界

Agent负责脚本、测试代码、单一Marker资产、旧方向资产清理、`demo2_act1`迁移、OpenSpec和编译验证。Unity自动测试仍按用户既有要求不擅自执行；实际双视图和流水烟测由项目负责人确认。

详细行为见：

- `specs/demo2-carryable-pipes/spec.md`
- `specs/demo2-water-network/spec.md`

## 双视图水状态同步

```text
权威状态            Demo2PipeWaterController
侧视初始水          首次显示SideView时主动查询，不等待切换TopView
侧视后续更新        Water状态变化事实触发重新查询
绑定规则            Required Cells全部有水时显示对应Wet Visual
SideView重新启用    立即查询最新状态，不依赖停用期间事件
第二套水模拟        禁止
```

## 第一幕直线垂直切片

```text
Route Cells           (0..9,-2)
Source Cell           (0,-2)
FixedPipeA            Origin (1,-2)，占1..3
Movable Gap Slot      Origin (4,-2)，占4..6
FixedPipeB            Origin (7,-2)，占7..9
Tree Water Target     Route Cell (9,-2)中心
开局结果              水逐格到3并在缺口前停止
正确放置结果          水重新从源头逐格到9并触发树苗发芽
```


