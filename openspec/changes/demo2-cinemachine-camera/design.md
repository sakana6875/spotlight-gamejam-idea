## Context

参见 `proposal.md` 的问题背景。当前场景已安装 Cinemachine 2.10.7：Main Camera上的Brain已启用Smart Update、Late Update Blend、Ignore Time Scale和0.5秒Ease In Out；`CM_Gameplay`以Player为Follow目标、优先级20并带Pixel Perfect扩展；`CM_ViewTransition`无Follow目标、优先级10，但仍保留Framing Transposer且缺少Pixel Perfect扩展。旧 `Demo2CameraFollowController` 仍启用，`Demo2ViewModeController` 仍直接插值主相机Transform。

## Goals / Non-Goals

**Goals:**

- 让Cinemachine成为Demo2主相机位置和镜头Blend的唯一控制者。
- 保留现有冻结、黑幕、地图根节点切换、玩家状态取消和侧视安全落点时序。
- 玩家传送或调整落点后，Gameplay镜头首帧直接根据新位置建立状态，不从旧位置阻尼追赶。
- 场景引用缺失时明确失败，不提供直接移动主相机的兼容路径。

**Non-Goals:**

- 不增加Confiner、震屏、Lookahead、动态缩放、Target Group或侧视/俯视独立跟随配置。
- 不修改用户已经选择的Gameplay Framing Transposer阻尼、正交尺寸和Brain Blend参数。
- 不迁移到Cinemachine 3.x，不建立通用相机服务或接口。

## Decisions

### 1. 使用一个Gameplay镜头和一个固定转场镜头

`CM_Gameplay` 始终Follow同一个Player；侧视和俯视只改变玩家位置与激活地图根节点，不需要配置相同的第二个Gameplay镜头。`CM_ViewTransition`只表达固定世界构图。

替代方案是为两种视图各建Gameplay与Closeup镜头。当前相机参数和四个旧Anchor位置完全相同，四镜头方案只增加序列化引用和优先级组合，没有可观察收益。

### 2. 通过固定优先级切换控制活动镜头

基础优先级为Gameplay 20、Transition 10；进入转场时Transition临时提升到30，返回Gameplay时恢复为10。控制器在Awake和收尾路径写入这些确定值，避免场景误调导致两个镜头竞争。

不启停Virtual Camera GameObject：启停会引入额外生命周期状态，优先级已经是Cinemachine的标准镜头选择入口。

### 3. 等待Brain实际Blend而非手工插值主相机

进入固定镜头后先等待一帧让Brain选中新镜头，再等待 `CinemachineBrain.IsBlending` 结束。回到Gameplay时，优先级切换与0.5秒黑幕淡出同时开始，淡出完成后继续等待尚未结束的Blend。Brain已配置Ignore Time Scale，因此冻结期间仍使用非缩放时间推进。

不再保留 `Vector3.Lerp` 主相机位置路径；任何直接写入都会与Brain输出竞争。

### 4. 玩家换位后使Gameplay上一帧状态失效

在黑幕覆盖下调用 `SetViewMode` 后设置 `CM_Gameplay.PreviousStateIsValid = false`。Framing Transposer下一次更新会跳过旧状态阻尼并从玩家当前真实位置建立构图。该行为直接修复远离Anchor时玩家落到画面边缘的问题。

不调用 `ForceCameraPosition`：Gameplay构图规则属于Framing Transposer，手工计算相机位置会重复其职责并绕过Pixel Perfect调整。

### 5. 固定转场镜头使用空Body并保留Pixel Perfect

`CM_ViewTransition`无Follow目标，使用自身Transform作为固定位置，因此移除Framing Transposer；同时补上 `CinemachinePixelPerfect`，避免Brain在两个镜头间Blend结束后与Main Camera的Pixel Perfect Camera争夺正交尺寸。

### 6. 通过现有Demo控制器直接依赖Cinemachine类型

`Demo2ViewModeController`属于Unity运行时适配层，可以直接序列化 `CinemachineBrain` 与 `CinemachineVirtualCamera`。`Spotlight.Game`显式引用包程序集 `Cinemachine`。不为单一场景包裹接口或通用CameraManager。

## Risks / Trade-offs

- [Brain未勾选Ignore Time Scale会令冻结中的Blend停住] → 场景修正脚本固定该配置，运行时缺失引用直接报错；烟测覆盖冻结时转场。
- [Gameplay阻尼为1秒，正常移动时跟随较慢] → 这是用户当前Inspector配置，本变更只在换位时重置旧状态，不擅自修改手感参数。
- [转场中途停用可能留下高优先级镜头或暂停] → `CancelTransition`统一恢复优先级、使Gameplay状态失效、隐藏黑幕并释放暂停。
- [Pixel Perfect在镜头Blend过程中暂时无法逐像素对齐] → Cinemachine 2.x官方限制；Blend结束后两个镜头均恢复Pixel Perfect，本次不改变0.5秒Blend。
- [删除旧组件后无法快速回退] → 版本控制可整体回退本变更；不保留并行兼容路径，避免再次产生双重控制。

## Migration Plan

1. 在生产与PlayMode测试程序集显式引用 `Cinemachine`。
2. 迁移 `Demo2ViewModeController` 的字段和协程，先保持旧组件文件存在以保证场景可加载。
3. 更新行为测试为真实Brain与两个Virtual Camera路径，删除对旧控制器内部状态和固定相机坐标的断言。
4. 通过一次性Editor脚本连接场景引用、修正两个镜头与Brain配置、移除旧组件和CameraAnchors。
5. 删除一次性Editor脚本、`Demo2CameraFollowController.cs`及其meta，确认无调用点。
6. 编译生产、EditMode与PlayMode程序集；获得许可后执行目标PlayMode测试，并由项目负责人烟测远距离双向切换。
