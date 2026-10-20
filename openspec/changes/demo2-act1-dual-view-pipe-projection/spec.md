## 第一幕01共享规则

```text
初始视角            SideView
可编辑视角          TopView
玩家位置映射        保留X，Side/Top分别使用合法Y
可搬运管            3米直管
正确目标            右侧竖管与树苗之间白色横向Slot
错误位置            允许物理放置，但目标Slot不成立
管网权威            Side/Top根之外的共享PipeWaterSystem
侧视表现            按slotId自动显示干管与通水状态
转场                冻结水流，完成后从原状态继续
搬运中R             拒绝
对话与结算          本次不实现
```

详细场景见 `specs/demo2-act1-dual-view-pipe-projection/spec.md`。
