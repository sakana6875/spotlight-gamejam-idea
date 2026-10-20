## 1. OpenSpec确认

- [x] 1.1 核对Cinemachine 2.10.7、Brain、Gameplay和Transition现有场景配置
- [x] 1.2 编写proposal、spec、design、plan和tasks完整变更文档
- [x] 1.3 执行 `openspec validate demo2-cinemachine-camera --strict` 并修复全部问题
- [x] 1.4 获得用户对完整接管方案与删除旧路径的实施确认

## 2. 程序集与运行时迁移

- [x] 2.1 为Spotlight.Game与PlayMode测试程序集增加Cinemachine引用
- [x] 2.2 用Brain和两个Virtual Camera替换旧跟随与Anchor序列化字段
- [x] 2.3 实现20/10/30优先级切换和冻结期间实际Blend等待
- [x] 2.4 玩家换位后使Gameplay旧状态失效并同步淡出返回
- [x] 2.5 统一正常完成与中途取消的镜头、黑幕和暂停恢复
- [x] 2.6 删除全部Main Camera Transform直接插值路径

## 3. 场景资源清理

- [x] 3.1 通过Unity Editor连接Brain、Gameplay和Transition引用
- [x] 3.2 将Transition改为固定空Body并补齐Pixel Perfect扩展
- [x] 3.3 移除Main Camera旧跟随组件和无调用方CameraAnchors
- [x] 3.4 删除一次性Editor脚本及其meta
- [x] 3.5 删除Demo2CameraFollowController源码、meta和全部调用点

## 4. 行为验证

- [x] 4.1 更新视角切换测试为真实Cinemachine镜头与远距离玩家构图
- [x] 4.2 覆盖冻结Blend、地图切换、侧视落点和中途恢复边界
- [x] 4.3 编译生产、EditMode和PlayMode程序集
- [ ] 4.4 按用户许可执行Cinemachine目标PlayMode测试并记录结果
- [x] 4.5 项目负责人烟测左右远端的双向视图切换与持续跟随

## 当前验证记录

- `demo2-cinemachine-camera` OpenSpec严格校验通过。
- `Spotlight.Game`、`Spotlight.Tests.EditMode` 和 `Spotlight.Tests.PlayMode` 编译通过：0个警告、0个错误。
- Cinemachine目标PlayMode测试已更新，但按用户此前要求暂未执行。
- 项目负责人已确认左右远端双向视图切换、玩家重新构图、持续跟随和Console状态全部正常。
