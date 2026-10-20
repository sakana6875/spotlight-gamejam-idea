## 实施顺序

1. 为PipeWaterController增加管段登记/移除事件、指定格占用和有水查询。
2. 将PipeSegment改为显式幂等Initialize，移除OnDisable注销，保留拾取与销毁清理。
3. 新增 `Demo2Act1PipeSlotDefinition`、`Demo2Act1PipePuzzleController` 和Slot状态事件。
4. 新增 `Demo2Act1SidePipeProjection`，按slotId显示干管与通水视觉。
5. ViewModeController增加TopLandingY并保持X。
6. 添加Slot匹配、错误位置、跨停用保留和侧视投影测试代码。
7. 严格校验OpenSpec并编译三个程序集，不运行用户禁止的自动测试。

## 用户接线范围

- 把PipeWaterSystem和Act1PuzzleController放到SideView/TopView之外。
- 创建3米PipeShape和Prefab。
- 配置目标Slot的slotId、原点、Shape和朝向。
- 把所有TopView管段连接到Act1控制器的初始化数组。
- 为SideView创建对应slotId的干管/通水视觉。
- 恢复SideView、TopView、Cinemachine、Blackout和ViewModeController引用。
- Player InitialViewMode设为Side，配置SideLandingY与TopLandingY。

## 验证路径

- 侧视入场，R切俯视且保留X。
- 俯视错误位置可放但Slot不成立。
- 3米管放白色目标位后Slot成立并通水。
- 切侧视后投影管段和水状态显示。
- 来回切换不注销、不重置水。
