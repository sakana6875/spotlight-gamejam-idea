## Why

Demo2 当前由 `Demo2CameraFollowController` 和 `Demo2ViewModeController` 直接修改主相机位置。视角切换结束时，相机先落到固定 Gameplay Anchor，再按玩家后续位移增量恢复跟随；当玩家离 Anchor 较远时，玩家会停在画面边缘甚至画面外。场景已经配置 Cinemachine 2.10.7、Brain、玩家跟随镜头和固定转场镜头，需要完成单一相机控制路径的代码迁移。

## What Changes

- 由 `CM_Gameplay` 持续以玩家为 Follow 目标，由 `CM_ViewTransition` 表达固定转场构图；两个 Virtual Camera 通过优先级和 Cinemachine Brain 的0.5秒 Blend切换。
- 视角切换继续冻结玩法、执行黑幕淡入淡出、切换 SideView/TopView，并保留回到侧视时的安全Y落点。
- 玩家切换视图并改变位置后，使 Gameplay Virtual Camera 的上一帧状态失效；恢复为活动镜头时必须根据玩家当前真实位置重新构图，不从固定 Anchor 或旧玩家位置继续追赶。
- 删除 `Demo2CameraFollowController` 及 `Demo2ViewModeController` 对主相机 Transform、四个 Camera Anchor 的直接控制，使 Cinemachine成为唯一相机位置写入者。
- `Spotlight.Game` 增加对 Cinemachine 运行时程序集的显式引用；保留现有 URP 2D、正交相机、Pixel Perfect Camera、5.4正交尺寸和已配置的跟随阻尼。
- 修正 `CM_ViewTransition` 为固定镜头管线并补齐 Cinemachine Pixel Perfect扩展；保留用户已配置的 Brain、Virtual Camera优先级和场景构图。
- 更新现有视角转场行为验证，覆盖远离固定转场点的玩家在切换后仍被 Gameplay镜头正确构图。

## Capabilities

### New Capabilities

- `demo2-cinemachine-camera`: 定义 Demo2 玩家跟随镜头、固定转场镜头、暂停时间下的Blend、视图切换后重新构图和单一相机控制权合同。

### Modified Capabilities

- 无。

## Impact

- 官方包依赖固定为 `com.unity.cinemachine` 2.10.7；不引入第三方包。
- 修改 `Assets/Scripts/Spotlight.Game.asmdef`，显式引用 `Cinemachine` 程序集。
- 修改 `Demo2ViewModeController` 及其 PlayMode行为验证。
- 删除 `Demo2CameraFollowController.cs` 及对应 `.meta`，清理全部调用点和场景组件。
- 通过 Unity Editor更新 `Assets/Scenes/demo2.unity`：连接 Brain与两个 Virtual Camera、移除旧跟随组件和 Camera Anchor、补齐固定镜头配置。
- `Packages/manifest.json` 和 `Packages/packages-lock.json` 保留用户已安装的 Cinemachine 2.10.7记录。

## 非目标

- 不增加 `CinemachineConfiner2D`、地图边界、镜头震动、Lookahead、动态缩放、Target Group或新相机动画。
- 不改变0.5秒镜头段、0.5秒黑幕段、暂停规则、玩家视图切换规则和侧视安全落点。
- 不升级 Unity、URP、Input System或其他包，不迁移到 Cinemachine 3.x。
- 不为侧视和俯视复制配置相同的 Gameplay Virtual Camera。
