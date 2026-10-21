## Purpose

定义可作为 Prefab 重复放置的 Demo2 侧视刷怪装置，使树首次接水、独立波次计时、生成点阻挡、怪物场景依赖注入和单装置完成事实保持确定且可验证。

## ADDED Requirements

### Requirement: 树开始发芽时立即发布一次启动事实

树从 `Dormant` 成功进入 `Sprouting` 时 MUST 在同一调用中发布一次 `SproutingStarted`。重复水检测、重复 `BeginSprouting` 请求或后续阶段 MUST NOT 再次发布。刷怪装置 MUST 订阅树事实，不得自行查询水控制器。

#### Scenario: 水首次到达树苗

- **WHEN** 水检测首次使树成功从 `Dormant` 进入 `Sprouting`
- **THEN** 树立即发布一次 `SproutingStarted`，无需等待0.8秒发芽动画完成

#### Scenario: 重复水检测

- **WHEN** 树已经处于 `Sprouting` 或后续阶段时水检测再次执行
- **THEN** `SproutingStarted` 的累计发布次数仍为一次

### Requirement: 每个刷怪装置拥有独立可配置波次

每个装置实例 MUST 独立配置正整数生成数量和有限正数生成间隔。Prefab 默认数量 MUST 为8，默认间隔 MUST 为2秒。多个绑定同一树苗的装置 MUST 同时启动并分别生成各自配置数量；全场总数等于各装置数量之和。

#### Scenario: 两个装置独立配置

- **WHEN** 同一树苗绑定数量为3和5的两个装置后首次接水
- **THEN** 两个装置同时启动，并分别最多生成3只和5只怪物

#### Scenario: 非法波次配置

- **WHEN** 数量不是正整数或间隔不是有限正数
- **THEN** 装置报告具体配置错误并停止运行，不生成怪物

### Requirement: 第一只立即尝试生成且成功后重新计时

装置收到启动事实后 MUST 立即尝试生成第一只。每次成功生成后 MUST 从零重新计算配置间隔；未到间隔不得生成下一只。全局玩法冻结或装置随 `SideView` 停用期间 MUST 暂停计时，恢复后继续原剩余时间。

#### Scenario: 默认波次时序

- **WHEN** 默认装置在生成点持续空闲时启动
- **THEN** 第一只立即生成，后续怪物分别在每次成功生成约2秒后出现，直到总数达到8

#### Scenario: 对话暂停生成间隔

- **WHEN** 第二只生成前已累计1秒且对话冻结玩法3秒
- **THEN** 解除冻结后仍需约1秒才尝试生成第二只

### Requirement: 生成点被占用时等待且不消耗数量

装置 MUST 使用预分配的 Physics2D 重叠查询检查配置的阻挡层和生成区域。轮到生成时若区域内存在阻挡 Collider2D，装置 MUST 保持待生成状态、不增加已生成数量且不重置间隔；区域清空后的下一次运行更新 MUST 立即尝试生成。

#### Scenario: 怪物停在生成点

- **WHEN** 当前生成时刻到达但生成区域仍被上一只怪物占用
- **THEN** 装置不生成、不消耗数量，并在区域清空后立即生成当前这一只

### Requirement: 运行时生成怪物使用显式场景依赖注入

刷怪装置 MUST 从序列化的怪物 Prefab 实例化具体怪物，并通过一次性初始化入口显式传入装置绑定的树苗与暂停控制器。怪物和装置 MUST NOT 使用名称、标签、场景扫描、静态单例或 `Resources.Load` 补齐依赖。初始化失败时装置 MUST 销毁该实例、报告原因并停止后续生成。

#### Scenario: 成功生成史莱姆

- **WHEN** 生成点空闲且装置拥有有效史莱姆 Prefab、树苗与暂停控制器
- **THEN** 新实例位于 SpawnPoint、成为 SpawnedMonsters 子对象并使用同一树苗与暂停控制器

#### Scenario: 怪物初始化失败

- **WHEN** 怪物 Prefab 缺少有效自身配置或拒绝场景依赖初始化
- **THEN** 失败实例被销毁，装置明确报错且不继续生成误配置怪物

### Requirement: 单装置波次完成事实只发布一次

装置 MUST 统计已成功生成数量和仍存活的本装置怪物。只有配置数量全部成功生成且这些怪物全部发布死亡事实后，装置 MUST 发布一次 `WaveCompleted`。被其他装置生成的怪物不得影响该计数；禁用、重新启用或重复死亡通知不得重复完成。

#### Scenario: 本装置全部怪物死亡

- **WHEN** 数量为8的装置已经成功生成8只且最后一只怪物死亡
- **THEN** `WaveCompleted` 发布一次，已生成数量为8且存活数量为0

### Requirement: 刷怪装置是SideView下的不可见逻辑Prefab

Prefab MUST 包含根控制器、一个 `SpawnPoint` 子节点和一个 `SpawnedMonsters` 子节点，不得包含 SpriteRenderer、粒子或音频。场景设计者 MUST 将装置实例放在 `SideView` 下，并在 Inspector 明确连接树苗与暂停控制器。Prefab 资产 MUST 预先连接史莱姆 Prefab和自身子节点，但不得保存场景对象引用。

#### Scenario: 项目负责人放置装置

- **WHEN** 装置 Prefab 被放入某个 Demo2 场景的 `SideView` 并接好树苗与暂停引用
- **THEN** 无需额外运行时查找即可在树接水后开始独立波次
