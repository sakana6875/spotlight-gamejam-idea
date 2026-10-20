## 实施顺序

1. 保留用户已安装的 `com.unity.cinemachine` 2.10.7，在 `Spotlight.Game` 和需要直接创建Cinemachine对象的PlayMode测试程序集增加 `Cinemachine` 引用。
2. 将 `Demo2ViewModeController` 的旧跟随组件与四个Anchor引用替换为 `CinemachineBrain`、`CM_Gameplay` 和 `CM_ViewTransition` 明确引用。
3. 用20/10/30确定优先级表达Gameplay活动、Transition待机和Transition活动；视图控制器不再写Main Camera Transform。
4. 进入转场时冻结玩法、提升Transition优先级并等待Brain实际完成0.5秒Blend，再执行既有0.5秒黑幕淡入。
5. 黑幕覆盖时切换视图与玩家落点，使Gameplay镜头上一帧状态失效，降低Transition优先级，并在0.5秒黑幕淡出期间由Brain混合回玩家真实位置。
6. 正常完成和中途取消统一恢复Gameplay活动、Transition待机、黑幕透明且不阻挡、视图暂停释放。
7. 更新 `Demo2ViewTransitionTests`：保留状态取消、地图切换、冻结与侧视落点合同；用远离固定点的玩家在切换后处于相机视口内替代旧Transform实现断言。
8. 使用一次性 `Assets/Editor` 脚本修改 `demo2.unity`：连接Brain与两个Virtual Camera、移除Main Camera旧跟随组件、删除无调用方CameraAnchors、移除Transition Framing Transposer并增加Pixel Perfect扩展。
9. 删除一次性脚本和旧跟随组件源码/meta，检查无旧字段、类型或Anchor调用点。
10. 严格校验OpenSpec，编译 `Spotlight.Game`、`Spotlight.Tests.EditMode`、`Spotlight.Tests.PlayMode`；自动目标测试仍需用户明确许可，Editor烟测覆盖玩家位于远距离时的双向视图切换。

## 目录与程序集

- `Assets/Scripts/Demos/Demo2/Flow/Demo2ViewModeController.cs`：唯一转场编排入口，依赖Cinemachine运行时类型。
- `Assets/Scripts/Demos/Demo2/Camera/Demo2CameraFollowController.cs`：迁移后删除，不保留兼容别名。
- `Assets/Scripts/Spotlight.Game.asmdef`：增加 `Cinemachine`。
- `Assets/Tests/PlayMode/Demo2ViewTransitionTests.cs`：验证消费者可观察的视图、冻结、落点和最终构图。
- `Assets/Tests/PlayMode/Spotlight.Tests.PlayMode.asmdef`：增加 `Cinemachine` 以构造真实测试镜头。
- `Assets/Scenes/demo2.unity`：通过Unity Editor连接和清理组件，不手工编辑YAML。

## 验证路径

- OpenSpec：`openspec validate demo2-cinemachine-camera --strict`。
- 编译：生产、EditMode、PlayMode三个csproj分别执行 `dotnet build --no-restore`。
- PlayMode目标行为：冻结时Blend仍完成；远离固定转场点时，侧视转俯视和俯视转侧视后玩家均位于Gameplay镜头视口内；返回侧视保持X并使用安全Y。
- Editor烟测：玩家分别移动到场景左右远端后双向切换；观察黑幕、0.5秒镜头段、玩家居中、Pixel Perfect稳定和后续持续跟随。
