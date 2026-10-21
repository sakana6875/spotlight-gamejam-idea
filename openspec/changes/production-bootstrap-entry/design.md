## Context

`BootStrap.unity`已存在并位于Build Settings index 0，包含根GameObject `SessionRoot`及现有 `SessionRoot`组件；`demo2.unity`位于index 1。`SessionRoot.Awake`幂等创建SaveService与SceneLoader并调用DontDestroyOnLoad。当前唯一入口 `SessionSmokeEntry` 位于SampleScene，会保存检查点和解锁demo1，只能用于测试。

## Goals / Non-Goals

**Goals:**

- 为正式BootStrap场景提供无业务副作用的最小场景入口。
- 保持SessionRoot为唯一服务创建者，所有场景加载继续经过SceneLoader。
- 失败明确可见，不添加猜测式回退。

**Non-Goals:**

- 不增加异步加载进度UI、菜单选择或存档恢复。
- 不修改SessionRoot生命周期或引入静态实例。
- 不自动修改用户场景和Build Settings。

## Decisions

### 1. BootstrapEntry与SessionRoot同对象

使用 `[RequireComponent(typeof(SessionRoot))]` 表达必须共存的Unity合同。入口只消费组合根公开的SceneLoader，不自行创建服务。

### 2. 在Start而非Awake中加载

SessionRoot在Awake初始化；BootstrapEntry设置较晚执行顺序并在Start调用一次幂等Initialize，保证场景加载前服务可用。Start在组件生命周期中只执行一次，不需要额外防重复字段。

### 3. 序列化稳定ID，默认SceneCatalog.Demo2

Inspector显示 `_initialSceneId`，默认值为稳定ID `demo2`。这允许未来切换为 `demo2_act1`，但仍必须先在SceneCatalog登记；入口不接受Unity场景名作为第二套映射。

### 4. 不等待AsyncOperation

SceneLoader当前只返回请求是否启动；BootstrapEntry按现有合同处理Started、AlreadyActive和失败结果，不绕过边界获取AsyncOperation或增加新事件。

## Risks / Trade-offs

- [用户把Unity场景名填进稳定ID字段] → SceneLoader返回UnknownSceneId并明确报错；Inspector接线说明强调填写 `demo2`。
- [Bootstrap场景误挂SessionSmokeEntry] → 正式接线清单明确只挂SessionRoot与BootstrapEntry，烟测入口保留在SampleScene。
- [玩法场景也放SessionRoot] → 接线说明禁止；本变更不引入静态单例或全局查找来掩盖场景配置错误。

## Migration Plan

1. 新增BootstrapEntry脚本与meta。
2. 编译生产、EditMode和PlayMode程序集；不执行用户禁止的自动测试。
3. 项目负责人将BootstrapEntry挂到BootStrap/SessionRoot，并保持初始ID为demo2。
4. 从BootStrap场景Play，确认SessionRoot持久化且demo2加载。
