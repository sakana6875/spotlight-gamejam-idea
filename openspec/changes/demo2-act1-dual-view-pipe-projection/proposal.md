## Why

第一幕01是同一水管谜题的侧视与俯视投影：侧视入场并展示目标，俯视负责搬运3米直管，正确放置后侧视必须自动显示对应水平管段和共享水流。当前管网与管段依附TopView生命周期，切到SideView会注销管段并丢失状态，无法满足跨视角共享。

## What Changes

- 新增第一幕01场景级共享管道谜题控制器，放在SideView与TopView根之外，持续保存稳定Slot占用和通水状态。
- TopView仍是唯一允许E拾取和放置的视图；搬运中继续禁止R。
- 通过稳定`slotId`、目标网格原点、要求的PipeShape和90度朝向判定目标Slot。第一幕01正确Slot对应右侧竖管与树苗之间的3米水平缺口。
- 扩展PipeWaterController报告管段登记/移除事实并提供格子有水查询；共享控制器据此更新Slot状态。
- 新增SideView投影组件，根据Slot是否占用、是否通水控制侧视管段与水动画，不在SideView编辑管道。
- 重构PipeSegment为可显式幂等初始化；TopView停用不注销逻辑登记，场景卸载或拾取时才移除。
- 扩展视角控制器分别配置Side/Top合法Y，切换时保留玩家X；第一幕默认Side。
- 管网在转场暂停期间冻结，转场完成后从同一状态继续，不重置水。
- 只交付脚本、meta、测试代码和接线清单；场景、3米管Shape/Prefab、Slot坐标、侧视投影、Cinemachine与Inspector引用由项目负责人完成。

## Capabilities

### New Capabilities

- `demo2-act1-dual-view-pipe-projection`: 定义第一幕01稳定Slot、TopView权威编辑、SideView自动投影、跨视角管段生命周期和共享水状态。

### Modified Capabilities

- 无。

## Impact

- 修改现有PipeSegment、PipeWaterController和Demo2ViewModeController。
- 新增第一幕共享Slot定义、控制器和SideView投影脚本。
- 不修改任何Scene、Prefab、Tilemap、Cinemachine或Build Settings。

## 非目标

- 不实现对话、3分钟跳过、树苗3秒成长、关卡结算或场景推进。
- 不实现第一幕02水池同步、第零幕刷怪或第二幕Boss。
- 不允许SideView拾取和放置水管。
