## 1. OpenSpec确认

- [x] 1.1 核对BootStrap场景、SessionRoot、SceneCatalog和Build Settings现状
- [x] 1.2 确认正式入口默认通过稳定ID demo2加载玩法场景
- [x] 1.3 编写proposal、spec、design、plan和tasks文档
- [x] 1.4 严格校验OpenSpec

## 2. 正式入口脚本

- [x] 2.1 新增BootstrapEntry及SessionRoot同对象约束
- [x] 2.2 在Start阶段校验组合根和初始稳定ID
- [x] 2.3 通过SessionRoot.SceneLoader发起一次场景加载
- [x] 2.4 明确处理成功、未知ID和加载启动失败结果
- [x] 2.5 保持SessionSmokeEntry与正式入口隔离

## 3. 验证与接线

- [x] 3.1 编译生产、EditMode和PlayMode程序集
- [x] 3.2 按用户要求不执行Unity自动测试
- [x] 3.3 提供BootStrap场景Inspector接线清单
- [x] 3.4 项目负责人从BootStrap烟测进入demo2

## 当前验证记录

- `production-bootstrap-entry` OpenSpec严格校验通过。
- `Spotlight.Game`、`Spotlight.Tests.EditMode` 和 `Spotlight.Tests.PlayMode` 编译通过：0个警告、0个错误。
- Unity已导入 `BootstrapEntry.cs` 及meta；未执行自动测试，未修改用户创建的场景与Build Settings。
- 项目负责人已确认从BootStrap启动会成功进入demo2。
