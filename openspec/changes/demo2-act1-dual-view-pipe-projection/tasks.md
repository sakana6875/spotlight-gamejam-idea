## 1. OpenSpec确认

- [x] 1.1 确认仅TopView编辑、SideView自动投影
- [x] 1.2 确认侧视开始、保留X和共享水状态
- [x] 1.3 确认3米可搬运管与白色目标Slot
- [x] 1.4 编写proposal、spec、design、plan和tasks
- [x] 1.5 严格校验并获得代码实施确认

## 2. 管网与管段生命周期

- [x] 2.1 增加管段登记移除事件和格查询
- [x] 2.2 重构PipeSegment显式幂等初始化
- [x] 2.3 TopView停用时保持登记
- [x] 2.4 拾取、销毁和场景卸载时正确清理

## 3. 第一幕共享Slot

- [x] 3.1 新增稳定Slot定义和重复ID校验
- [x] 3.2 实现Shape、原点与朝向精确匹配
- [x] 3.3 实现Slot占用和通水状态变化事实
- [x] 3.4 显式初始化inactive TopView管段

## 4. 侧视投影与视角落点

- [x] 4.1 新增SideView干管和通水投影组件
- [x] 4.2 无效slotId明确失败
- [x] 4.3 ViewModeController增加TopLandingY并保留X
- [x] 4.4 保持搬运禁切换和转场冻结合同

## 5. 验证与交付

- [x] 5.1 添加Slot匹配与错误位置测试代码
- [ ] 5.2 添加跨View停用保留测试代码（按用户要求不继续补充Unity自动测试）
- [x] 5.3 编译生产、EditMode和PlayMode程序集
- [x] 5.4 按用户要求不执行自动测试
- [x] 5.5 提供完整Inspector接线清单
- [ ] 5.6 项目负责人完成双视图烟测

## 当前验证记录

- `demo2-act1-dual-view-pipe-projection` OpenSpec严格校验通过。
- 生产、EditMode和PlayMode程序集编译通过：0个警告、0个错误。
- Slot精确匹配测试代码已添加但未运行；跨View PlayMode测试按用户要求不再补充或执行。
- Agent未修改Scene、Prefab、Tilemap、Cinemachine或Build Settings；双视图资源与引用由项目负责人接线。
