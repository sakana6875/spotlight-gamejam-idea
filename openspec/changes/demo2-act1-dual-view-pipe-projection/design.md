## Context

现有PipeWaterController维护TopView有限网格；PipeSegment在Start登记并在OnDisable注销；ViewModeController会直接SetActive切换SideView/TopView。第一幕01从侧视开始，导致TopView中的管段可能未初始化，并在每次切换时丢失登记。第一幕只允许俯视编辑，但侧视必须投影同一解谜状态。

## Goals / Non-Goals

**Goals:**
- 共享逻辑状态独立于两个视图根生命周期。
- 以稳定Slot而不是两套世界坐标硬同步管道。
- 用户只需在Inspector配置Slot和投影引用。

**Non-Goals:**
- 不做对话、计时跳过、树苗成长和场景结算。
- 不运行时生成场景对象或修改资源。

## Decisions

### 1. PipeWaterController继续作为唯一权威水模型

控制器移到两个视图根之外并继续持有Route/Source/Water Tilemap引用。Top Water Tilemap可以随TopView隐藏，但模型始终活动。转场通过Time.timeScale=0冻结FixedUpdate。

### 2. Slot使用显式配置而不是坐标投影公式

新增可序列化Slot定义：slotId、gridOrigin、requiredShape、requiredQuarterTurns。第一幕不同视角的几何并不等价；稳定Slot避免用脆弱的世界坐标换算。

### 3. 管段登记事实驱动共享状态

PipeWaterController发布管段Placed/Removed事实，并提供指定格水查询。Act1控制器订阅后重算Slot，占用或湿状态变化时发布SlotChanged事实。

### 4. PipeSegment显式幂等初始化并跨停用保留

把Awake/Start配置与登记合并为公开幂等Initialize。Act1控制器在Start显式初始化序列化的TopView管段，即使其GameObject当前inactive。OnDisable不再注销；拾取明确注销，OnDestroy负责清理。

### 5. SideView投影只消费状态

每个Side投影配置slotId、dryVisual和wetVisual。它订阅Act1控制器，绝不修改管网或处理输入。

### 6. ViewModeController增加Top合法Y

目标位置始终保留当前X；Side使用sideLandingY，Top使用topLandingY。初始视图仍由PlayerMoveController的InitialViewMode配置为Side。

## Risks / Trade-offs

- [inactive管段的Awake未执行] → Act1控制器显式调用幂等Initialize。
- [OnDestroy顺序导致Controller先销毁] → 清理前检查Controller有效与已配置状态。
- [slotId重复] → Act1控制器启动时校验非空且唯一，失败明确禁用。
- [Side投影漏接] → 找不到slotId时明确报错，不创建默认投影。

## Migration Plan

1. 增加管段登记事件和查询。
2. 重构PipeSegment生命周期。
3. 新增Slot定义、Act1共享控制器与Side投影。
4. 增加Top落点配置。
5. 添加纯Slot状态与跨停用行为测试代码并编译。
6. 提供场景接线清单，由项目负责人搭建双视图资源。
