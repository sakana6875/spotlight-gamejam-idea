# 项目开发规范

## 1. 适用范围

本文件适用于仓库内全部 Unity 项目代码、场景、资源、编辑器工具、测试和内容数据。

本项目基于 Unity `2022.3.62f3c1`、URP 2D Renderer 和 Unity Test Framework。除非经过明确评审，不得改变核心技术路线。

本文件规定长期有效的开发约束；具体功能计划、阶段目标和一次性实施顺序不写入本文件。

## 2. 架构原则

### 2.1 组合根

`Bootstrap / SessionRoot` 是唯一组合根。

- 只有组合根创建跨场景服务的具体实现并组装依赖。
- 服务通过明确引用、初始化参数或构造器注入到场景入口和玩法模块；只有存在真实替换需求或外部边界时才引入接口。
- 其他模块不得自行查找、创建或替换全局服务。
- 禁止使用跨模块业务静态单例。
- 允许无状态纯函数、只读常量和由组合根持有的实例。

### 2.2 目录、分层和依赖方向

当前推荐目录结构：

```text
Assets/
├── Scripts/
│   ├── Bootstrap/
│   │   ├── SessionRoot.cs
│   │   └── SessionSmokeEntry.cs
│   ├── Domain/
│   │   └── Save/
│   ├── Save/
│   │   └── SaveService.cs
│   ├── Scene/
│   │   └── SceneLoader.cs
│   ├── Demos/
│   │   └── Demo2/
│   └── Spotlight.Game.asmdef
├── Tests/
│   ├── EditMode/
│   └── PlayMode/
└── Data/
```

目录职责必须保持：

```text
Bootstrap → Save / Scene / Domain
Save      → Domain
Scene     → Unity SceneManagement
Demos     → Save / Scene / Domain / Unity 组件
Domain    → 无 Unity 依赖
```

- `Domain` 只包含纯 C# 规则、值对象、状态和可序列化数据，不依赖 Unity 场景对象、UI、AudioSource、SceneManager 或具体 MonoBehaviour。
- `Bootstrap` 只负责启动、跨场景生命周期和当前真实服务组装，不承载菜单、Demo、存档合并或玩法规则。
- `Save` 实现当前真实存档和永久进度服务；没有文件持久化前不得伪装成可靠磁盘存档。
- `Scene` 是唯一允许封装 `SceneManager` 场景加载的运行时代码位置；普通玩法不得直接调用 `SceneManager`。
- `Demos` 存放真实 Demo 玩法。普通 Feature 可以靠近场景和 MonoBehaviour，只要职责清楚、易读、没有跨 Feature 具体实现依赖。
- 不保留没有真实调用方的 `Architecture/Application`、`Architecture/Adapters`、`Architecture/Composition`、`Architecture/Contracts` 或空 `Features` 目录。
- 需要结果的操作使用明确返回值、结果对象或枚举；只表达已发生事实的状态变化才使用类型化领域事件。
- EventBus 只能在出现真实跨对象事实通知需求后引入，且只负责发布、订阅和解除订阅，不得持有业务逻辑。

### 2.3 程序集

必须使用程序集定义（asmdef）保护仍有价值的依赖边界。当前生产程序集为：

```text
Spotlight.Domain
Spotlight.Game
Spotlight.Tests.EditMode
Spotlight.Tests.PlayMode
```

规则：

- `Spotlight.Domain` 不引用 Unity 场景相关程序集。
- `Spotlight.Game` 可引用 `Spotlight.Domain` 和 Unity，承载 Bootstrap、Save、Scene、Demos、UI 和 Data。
- 当前不保留独立的 `Spotlight.Application`、`Spotlight.Adapters` 或空的 `Spotlight.Features.*` 程序集。
- 只有出现明确独立编译隔离需求或真实模块边界时，才新增 Feature 程序集。
- 测试程序集只引用被测试的程序集和必要的 Unity Test Framework。
- 禁止程序集循环引用。
- `.asmdef` 和对应 `.meta` 文件必须提交。
- 不为单个小类创建独立程序集，避免无意义拆分。

## 3. Unity 约束

### 3.1 2D 基础

- 主要场景和 Demo 使用现有 URP 2D Renderer。
- Demo2 必须保持纯 2D，不创建核心 3D Renderer、真实 3D 摄像机或第二套物理世界。
- 主要玩法使用 Sprite、Tilemap、Collider2D、Rigidbody2D 和 2D 光照。
- 不用 3D 物理替代 2D 玩法约束。

### 3.2 MonoBehaviour

MonoBehaviour 只负责 Unity 生命周期和外部适配：

- 输入读取；
- 碰撞、触发和物理桥接；
- Animator、Particle、Camera、UI 和 AudioSource 操作；
- 场景加载回调；
- 将 Unity 事件转换为明确服务调用或领域事件。

禁止把存档合并、阶段规则、节拍判定、进度推进等核心规则藏在 MonoBehaviour 中。

### 3.3 Unity 资源

- 场景、Prefab、Input Actions、AudioMixer 和复杂 YAML 资源优先通过 Unity Editor、CustomEditor 或专用生成器修改。
- 不手工编辑复杂 Unity YAML，除非没有可行的 Editor/生成方式且改动可重复验证。
- `[SerializeField] private` 优先于公开字段。
- 不序列化接口、场景对象引用、服务实例或任意 MonoBehaviour 作为领域状态。
- ScriptableObject 用于配置和内容资产，不作为跨场景业务状态容器。
- Unity 资源的 `.meta` 文件必须与资源一起提交。

## 4. 命名规范

代码标识符和 Unity 资源名使用英文；代码注释和文档使用中文；禁止用中文标识符。

```text
类型、接口、枚举、公开成员       PascalCase
接口                             I + PascalCase
私有字段                         _camelCase
局部变量、参数                   camelCase
常量                             PascalCase
事件类型                         XxxEvent
异步方法                         XxxAsync
布尔值                           Is/Has/Can/Should 开头
```

示例：

```csharp
public sealed class CheckpointManager
{
    private readonly SaveService _saveService;
    private bool _isRestoring;

    public CheckpointManager(SaveService saveService)
    {
        _saveService = saveService;
    }
}
```

命名空间必须明确，不使用全局命名空间、`Common`、`Utils` 等无法表达职责的垃圾桶命名空间。推荐：

```text
Spotlight.Bootstrap
Spotlight.Domain
Spotlight.Save
Spotlight.Scene
Spotlight.Demos.Demo2
Spotlight.Tests.EditMode
Spotlight.Tests.PlayMode
```

稳定 ID 使用小写英文和数字，采用与代码一致的 `demo1` 风格：

```text
demo1
demo2
checkpoint_demo2_stage1
story_demo1_complete
```

稳定 ID 一旦发布不得因类名、文件名或显示文案变化而改变。

## 5. C# 格式和实现风格

- 4 个空格缩进。
- 使用 Allman 风格大括号。
- 单行尽量不超过 120 个字符。
- 一个文件原则上只放一个主要公开类型。
- 明确写出访问修饰符，不依赖默认可见性。
- 优先使用 `readonly`、不可变数据、值对象和构造器注入。
- `var` 只用于类型从右侧表达式明显可知的场景。
- 不使用 `#region` 隐藏过长类型或掩盖职责混乱。
- 不为未确定的功能提前创建空壳、假实现、占位分支或误导性的 MVP 接口。
- 不用静默 catch、空 catch 或默认吞掉错误。
- 不用异常处理正常业务分支；外部输入和存档错误必须转换成可观察的结果。

## 6. 注释和文档

注释使用中文，代码标识符仍使用英文。

注释必须解释原因、约束、不变量或容易误解的决策，不重复代码表面行为。

允许的注释重点：

- 状态机允许和禁止的转换；
- 存档永久进度与临时状态的合并规则；
- DSP 时间、节拍窗口和暂停恢复的边界；
- 事件发布顺序和订阅生命周期；
- Unity API 或序列化格式的必要规避原因；
- 暂时保留但已明确范围的兼容性约束。

禁止以下无效注释：

```csharp
// 保存检查点
SaveCheckpoint();
```

公共接口、公共类型、公开序列化配置字段和复杂领域规则必须提供中文 XML 文档。不要在源文件中复制完整架构设计；长期规则写入本文件，功能设计写入对应代码或资源说明。

TODO 只能记录已确认的后续需求，不能用来掩盖未实现的必需功能。未完成的必需行为不得提交为 TODO、空方法、假返回值或 no-op。

## 7. 接口、事件和状态

- 命令或查询需要结果时，通过接口返回明确结果对象、枚举、`bool` 或错误信息。
- 事件只能表示已经发生的事实，不能用事件等待命令结果。
- 事件类型使用过去时或事实名，例如 `StageChangedEvent`、`MemoryCollectedEvent`。
- 事件发布者不得持有订阅者的具体类型。
- 订阅必须有明确的解除时机，场景对象销毁或场景卸载时必须解除订阅。
- 失败、取消、主动退出和成功必须使用不同的状态，不得用一个布尔值混淆。
- 领域状态必须可序列化为稳定数据，不能依赖 Unity 对象引用恢复。

## 8. 存档和永久进度

- 存档容器、快照、设置和永久进度使用纯数据契约。
- `checkpointId` 必须稳定且唯一；同一 ID 的后写入快照覆盖旧快照。
- 检查点是可靠持久化边界，退出保存只能作为尽力补充。
- 写入使用临时文件成功后替换正式文件，禁止直接覆盖造成半截存档。
- 读取旧快照时允许场景、玩家位置和临时机关回退，但不得撤销已解锁 Demo、已看剧情和永久收集。
- 不序列化 Unity 对象、场景引用或任意 MonoBehaviour。
- 开始新游戏必须通过明确的确认流程重置唯一存档容器。

## 9. 内容和数据

- 剧情、旁白和 CG 源文件使用外部 JSON，运行时只依赖生成的 ScriptableObject 资产。
- JSON 导入器必须校验必填字段、稳定 ID、重复 ID、内容类型和标记引用。
- 校验失败时不得生成或覆盖对应运行时资产，并报告文件路径和字段。
- JSON 源文件、生成资产和 `.meta` 文件都提交版本控制。
- 运行时代码只保存稳定 `contentId`，不得把跨场景台词直接硬编码到 NPC 或 Demo 控制器中。
- 当前不添加配音字段或 AudioClip 引用；旁白文本仍由统一 DialogueManager 播放。

## 10. 音频和输入

- UI、玩家、交互、战斗、BGM 和旁白通过统一接口或 AudioCue 触发，不在玩法代码中直接操作 AudioSource。
- 全局 AudioManager 由组合根创建并跨场景存活；场景对象不得销毁全局音乐源。
- Mixer 分组和设置键名必须保持稳定：`Master`、`BGM`、`SFX`、`UI`、`Player`、`Interaction`、`Combat`、`Narration`，以及 `masterVolume`、`bgmVolume`、`sfxVolume`、`narrationVolume`。
- 输入通过 Input System 动作资产和输入适配器提供；玩法不得直接绑定按键或使用旧 `Input.GetKey` 作为正式实现。
- 玩法只读取 `Move`、`Interact`、`Pulse`、`SwitchMode`、`Attack`、`Pause`、`SkipDialogue` 等动作，不依赖具体键位。

## 11. 测试和验证

验证按改动类型执行，不把“能编译”当作完整验证：

- Domain 纯规则：使用 EditMode NUnit 行为测试。
- 场景、Physics2D、AudioSource、SceneManager、实际 UI：使用 PlayMode 测试或可重复手动烟测。
- 输入、音频、内容导入、存档和场景流程改动：必须验证对应运行时路径。
- 每个新增核心规则至少覆盖一个边界或失败分支。
- 测试行为输入和输出，不测试 MonoBehaviour 私有字段、Unity 序列化细节或实现内部调用次数。
- 测试替身只用于真实外部边界，不为不存在的端口提前创建记录型 fake。
- 不为“有测试”而添加只断言不抛异常、长度大于零或内部字段复制的测试。

最低验证范围包括：

- 空存档和无 Demo 存档；
- 重复 `checkpointId` 的覆盖；
- 旧快照读取时永久进度保留；
- Demo 成功、失败、重试和退出；
- 非法和合法 Demo2 阶段转换；
- 事件发布与订阅；
- BeatClock 判定边界；
- 内容导入失败不覆盖旧资产；
- 场景加载和 Bootstrap 服务生命周期。

## 12. Git 和文件管理

必须提交：

- C# 源文件；
- `.asmdef` 和 `.meta` 文件；
- JSON 源文件；
- 生成的 ScriptableObject 资产；
- 场景、Prefab、Input Actions、AudioMixer 等项目资源；
- 与代码变更匹配的测试和配置。

禁止提交：

```text
Library/
Temp/
Logs/
Obj/
UserSettings/
```

不得覆盖、删除或重置不属于当前任务的未提交工作。发现已有改动时，应基于现状调整，不得假设工作区干净。

## 13. 修改流程

完成一个功能改动时按以下顺序执行：

1. 先读取相关项目状态、现有模式和调用点。
2. 明确影响的目录、程序集、资源和验证路径。
3. 先修改纯数据或 Domain 规则，再接入 Save、Scene、Bootstrap 或具体 Demo。
4. 保持每个改动可编译，避免跨目录临时引用。
5. 按改动类型运行针对性测试或实际烟测。
6. 检查永久进度、场景生命周期、事件订阅和资源 `.meta` 是否完整。
7. 删除已被新实现替代的旧路径，不保留无必要兼容别名、静态单例或重复入口。

## 14. 明确非目标

当前阶段不做以下内容：

- 不把 Demo2 改成 3D 或实现 2D/3D 维度切换；
- 不实现完整武器、技能树、装备、属性克制或敌人状态机；
- 不把 Odin Inspector 作为核心依赖；
- 不因普通 Demo 失败自动触发坏结局；
- 不提前锁死 Demo1 的 Miss 失败规则；
- 不创建万能 GameManager、超级玩家类或超级敌人基类；
- 不用静态单例替代组合根和接口注入；
- 不用运行时解析外部 JSON 替代导入后的运行时资产。
 
## 15. Agent 执行规范
 
### 15.1 需求确认
 
- 开始编码或修改资源前，Agent 必须先明确复述：目标、修改范围、明确不做的内容和验收条件。
- 需求存在行为、数据格式、接口、错误策略、键位、默认值或兼容性歧义时，必须先询问，不得自行猜测。
- 用户未确认方案时，只能读取、分析和整理方案，不得修改代码、场景、资源或配置。
- 只有不影响外部行为的机械细节，才允许直接沿用仓库既有惯例。
 
### 15.2 范围控制
 
- 只修改完成当前需求所必需的文件和资源。
- 禁止顺手重构、提前抽象、添加未要求的重试、缓存、遥测、兼容层或扩展点。
- 禁止创建“以后可能有用”的空接口、占位实现、万能管理器或无实际调用方的配置。
- 需求未覆盖的产品行为必须保留为问题并询问，不得用“大概版本”替代确认。
 
### 15.3 兜底和错误处理
 
- 外部输入、存档、JSON 和 Unity 生命周期边界必须做必要校验。
- 禁止为理论上不会发生的情况堆叠无意义的 null 检查、catch、默认值和静默降级。
- 失败必须可观察并保留原因；禁止用空结果、假成功、自动跳过或吞异常掩盖错误。
- 兜底逻辑必须对应已知边界、已有产品规则或可验证的故障模式，并在注释或设计记录中说明原因。
 
### 15.4 验证和失败处理
 
- 修改前必须确认现状、调用关系、资源状态和现有未提交改动。
- 修改后必须验证对应行为；不能只报告“代码已写入”或“应该可以”。
- 测试或烟测失败后，Agent 必须继续定位和修复，不得删测试、放宽断言、绕过执行路径或隐瞒已知失败。
- 只有确实存在外部阻塞、缺少用户决策或工具无法访问的前提时，才能暂停并报告阻塞原因、已完成工作和所需信息。
 
### 15.5 危险操作
 
- 删除、覆盖或移动已有文件、场景、Prefab、资源和配置前，必须确认操作范围。
- 修改 Build Settings、存档格式、公开接口或已存在资源的稳定 ID 前，必须先确认影响范围和迁移策略。
- 当前任务明确要求的新文件和明确指定的目标文件可以直接修改。
- 禁止重置、覆盖或删除不属于当前任务的未提交工作。
 
### 15.6 交付纪律
 
- 不得交付带有必需 TODO、空方法、假返回值、no-op 或未说明降级行为的实现。
- 所有代码、资源、测试和验证结论必须基于实际读取或实际执行结果，不得臆测。
- 发现实现前提失效、接口冲突或产品规则缺失时，立即停止受影响的实现并询问；可以继续完成不依赖该决定的独立工作。
 
## 16. Issue、Commit 和 PR 规范
 
### 16.1 Issue 拆分
 
- 一个可独立验收的垂直能力对应一个 Issue，不按接口、实现、场景和测试机械拆分。
- Issue 必须说明目标、背景、范围、非目标、接口与约束、验收条件、验证方式、依赖和待确认问题。
- Issue 必须描述用户可观察行为和边界，不得只列类名、文件名或实现步骤。
- 未确定的产品规则必须记录为待确认问题，不得在 Issue 中假定一个未经确认的行为。
- 一个 Issue 应尽量在一个 PR 内完成；确实需要跨 PR 时，必须说明依赖关系和每个 PR 的可验证边界。
 
### 16.2 Commit
 
- Commit 必须代表单一、可解释、可回退的逻辑变更。
- Commit 必须可编译，不得提交破损状态、临时调试代码、空实现、假返回值或无关格式化。
- 行为改动必须在提交前完成对应验证；资源改动必须包含必要的 `.meta` 和引用变更。
- Commit 不得混入其他 Issue、无关重构或大规模格式化。
- 推荐使用 Conventional Commit 格式，提交说明正文使用中文；类型和 scope 可使用稳定的英文标识：
 
```text
feat(save): 增加最新检查点覆盖
fix(scene): 修复场景加载后的快照恢复
test(domain): 增加永久进度合并测试
```
 
### 16.3 PR 完成条件
 
- 一个主要 Issue 对应一个 PR；跨 Issue 时必须在 PR 描述中说明原因。
- PR 必须包含关联 Issue、变更目标、实现范围、非目标、验证记录、已知限制、风险和 Review 重点。
- Issue 的全部验收条件必须完成，相关调用方必须迁移，不能留下必需 TODO、占位实现或静默降级。
- 代码、程序集、Unity 资源、`.meta`、生成资产和测试必须保持完整且一致。
- 场景、UI、音频、输入或剧情改动必须提供实际运行验证、复现步骤和预期结果；必要时附截图或录屏。
 
### 16.4 Review
 
- 普通 PR 至少需要一名了解相关模块的 reviewer 批准。
- 存档格式、公开接口、Build Settings、核心场景流程等高风险 PR 需要两名 reviewer 批准。
- Review 优先检查验收条件、失败和空数据边界、依赖方向、稳定 ID、存档兼容性、Unity 资源引用和验证真实性。
- Reviewer 不得只检查格式；必须确认实际行为没有超出 Issue 范围。
- 阻塞性 Review 意见必须在合并前解决，不能通过隐藏、放宽测试或绕过路径处理。
 
### 16.5 合并
 
- 合并前必须完成验收、针对性测试或实际烟测，并通过项目要求的 CI 检查。
- 已知非阻塞问题可以合并，但必须有独立 Issue、影响说明和明确负责人；阻塞性问题不得合并。
- 默认使用 Squash merge，使一个 Issue 在主分支形成一个清晰的逻辑提交。
- PR 描述、Review 结论和实际改动必须一致；不得以未执行的验证或未完成的审查作为合并依据。
 
## 17. 分支、依赖和需求变更
 
### 17.1 分支
 
- 不直接在 `main` 上开发。
- 一个 Issue 使用一个工作分支；分支名包含 Issue 编号和简短中文摘要。
- 推荐格式：`feature/123-存档检查点`、`fix/145-场景恢复错误`、`chore/167-更新测试配置`。
- PR 合并后删除对应工作分支。
 
### 17.2 Unity 和包依赖
 
- 禁止自行升级 Unity Editor 版本；当前项目固定使用 `2022.3.62f3c1`。
- 新增 Unity 官方包必须在 Issue 中说明原因、版本和影响范围。
- 第三方包不得随功能顺手引入，必须单独评估兼容性、许可证、CI 配置和移除方案。
- Odin、UniTask、第三方存档库等不得作为核心依赖，除非经过单独确认。
 
### 17.3 Editor 验证责任
 
- 代码和纯 Domain 测试由 Agent 完成。
- 场景、UI、音频、输入、物理和 PlayMode 验证通常由项目负责人在 Unity Editor 中完成。
- Agent 未实际完成 Editor 验证时，必须明确标记“Editor 验证未完成”，不得声称场景或运行时行为已验证。
 
### 17.4 存档版本
 
- 首版只支持当前 `saveVersion`，暂不实现旧版本迁移。
- 存档格式变化必须递增 `saveVersion` 并更新相关验收条件。
- 遇到不支持的存档版本必须明确报错或提示，不得静默重置、删除或当作新档读取。
 
### 17.5 需求变更
 
- 需求扩大、增加外部行为、改变数据格式或改变已确认设计时，必须拆分新 Issue。
- 小型文字澄清可以直接更新当前 Issue，但不得改变原验收边界。
- 发现产品规则缺失或设计方向冲突时，暂停受影响实现并询问，不得在当前 PR 中隐式扩大范围。
 
### 17.6 回归范围
 
- 不要求每个小 PR 执行完整项目回归。
- 按改动类型执行针对性测试或烟测。
- Domain 改动验证相关 EditMode 测试；场景、输入、UI、音频和剧情改动验证对应路径。
- 存档、场景生命周期和核心流程改动需要更完整的主流程验证；发布前再执行完整回归。
 
## 18. OpenSpec 变更工作流
 
### 18.1 适用范围
 
- 涉及架构、公开接口、程序集边界、存档格式、场景生命周期或跨模块行为的中大型改动，必须先建立 OpenSpec 变更。
- OpenSpec 文档使用中文编写；代码标识符、稳定 ID 和命令名称仍使用英文。
- 当前项目使用 OpenSpec 1.7.0；实际命令和模板以仓库安装版本为准，不得凭记忆假定生成结构。
 
### 18.2 变更文档
 
每个 OpenSpec 变更至少包含以下文档：
 
- `proposal.md`：背景、问题、目标、范围、非目标、主要决策和风险。
- `spec.md`：可观察行为、接口契约、数据约束、边界条件和验收规则。
- `plan.md`：实现顺序、技术方案、目录、程序集、依赖和验证路径。
- `tasks.md`：可执行的实现任务和验证任务。
 
文档职责不得混淆：`proposal.md` 解释为什么做，`spec.md` 定义必须满足什么，`plan.md` 说明如何组织实现，`tasks.md` 列出具体执行项。
 
### 18.3 实施顺序
 
变更必须按以下顺序推进：
 
1. 读取项目现状、相关调用点、资源状态和未提交改动。
2. 初始化或更新 OpenSpec，并创建变更文档。
3. 编写并审阅 `proposal.md`、`spec.md`、`plan.md` 和 `tasks.md`。
4. 执行 `openspec validate <change-name>`，修复文档或结构问题。
5. 在用户确认 OpenSpec 文档前，不得开始代码、场景、资源或配置实现。
6. 用户确认后，严格按 `tasks.md` 顺序实现，并在完成任务后立即更新任务状态。
7. 每个可验证阶段执行对应的编译、测试或实际烟测；失败后必须继续定位和修复。
8. 全部验收条件完成后，执行最终验证，再使用 OpenSpec archive 流程归档变更。
 
### 18.4 tasks.md 与执行 Todo 同步
 
- `tasks.md` 是代码实现阶段的唯一细粒度任务来源，同时作为当前执行 Todo 的来源。
- 开始实现前，将 `tasks.md` 中每个可独立验证的任务映射为一个 Todo，不得合并、遗漏或凭记忆重排任务。
- 开始处理任务时，将对应 Todo 标记为进行中；完成并验证后同时更新 `tasks.md` 和 Todo 状态。
- 任务失败或遇到外部阻塞时，保留失败原因或阻塞原因，不得删除任务、放宽验收条件或伪造完成。
- OpenSpec 的架构依赖顺序优先于 GitHub Issue 编号顺序；Issue、`tasks.md` 和实际代码范围必须保持一致。
 
### 18.5 GitHub Issue 关系
 
- 一个 OpenSpec Change 可以对应多个垂直 GitHub Issue；不为每个 Issue 重复创建互相冲突的完整架构规范。
- Issue 描述必须引用对应的 OpenSpec Change 或文档路径，并保持范围、非目标和验收条件一致。
- OpenSpec 文档确认前可以创建 Issue 草稿，但不得据此开始实现代码。
- 需求扩大、改变已确认接口或修改验收边界时，必须更新 OpenSpec，并在必要时拆分新的 Change 或 Issue。
 
### 18.6 注释与验证
 
- OpenSpec 文档中的设计决策必须在代码的中文 XML 文档或原因注释中得到必要体现。
- `tasks.md` 中的验证任务必须对应实际执行的命令、测试或运行场景；未执行的验证不得标记完成。
- Unity Editor、场景、UI、输入、音频、物理和 PlayMode 验证未由 Agent 实际执行时，必须标记“Editor 验证未完成”。
- 归档前必须确认代码、测试、资源、`.meta`、OpenSpec 文档和 GitHub Issue 的实际状态一致。