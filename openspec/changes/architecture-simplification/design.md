# 架构简化设计记录

## 当前问题

当前架构的主要成本来自“未来能力先抽象”，不是来自依赖方向本身。`SessionRoot`、服务工厂、服务聚合、记录适配器和烟测入口共同证明的是接口能互相调用，而不是玩家可观察行为。

## 决策一：组合根保持单一，固定组装直接化

`SessionRoot` 仍然是唯一组合根。由于当前只有一个组装方案，工厂接口和 Bootstrapper 不再承担真实替换职责。组装过程折叠后，初始化入口、失败路径和服务生命周期应更容易从一个文件追踪。

不允许以“简化”为理由改成静态服务查找或 Service Locator。

## 决策二：烟测不是生产能力

`SessionSmokeEntry` 可以作为临时验证工具，但它不能继续成为默认运行时中所有服务的消费者，也不能通过自发发布 `SessionInitializedEvent` 证明正式功能已经可用。删除或收窄烟测时，保留真正有价值的初始化失败和生命周期验证。

## 决策三：场景加载保留窄边界

Feature 不直接调用 Unity `SceneManager`，这是便宜且清晰的约束，应保留。需要删除的是没有调用方的场景 API 和 Recording/Unity 两套生产形状，而不是场景边界本身。

若当前 Unity adapter 尚未能承担真实流程，必须明确其为未接入适配器，不能把 Recording fake 当作正式运行时实现。

## 决策四：普通 Feature 允许靠近 Unity

普通 Feature 使用清晰的 MonoBehaviour 组合，不强制建立 Application Service、Adapter、Port、Result 的转发链。与 Unity 无关、复杂并且需要行为测试的规则才进入 Domain。这样仍能保持跨 Feature 依赖禁止和全局服务由组合根创建。

## 决策五：存档行为优先保留

存档中的稳定检查点覆盖和永久进度保护直接影响玩家体验，不能因为删除架构脚手架而删除。设置、多快照列表和迁移模型只有在产品需求出现时才增加。

## 删除判断标准

一个抽象只有在至少满足以下条件之一时保留：

- 存在真实生产调用方；
- 隔离 Unity/文件/外部系统边界；
- 有独立且可观察的领域规则；
- 已确认需要替换实现；
- 删除会破坏稳定存档或公开行为。

仅因为未来可能复用、测试替身容易记录或架构图预留，不足以保留抽象。
## 程序集决策

当前保留两个生产程序集：

```text
Spotlight.Domain
Spotlight.Game
```

`Spotlight.Domain` 保护纯 C# 规则不引用 Unity；`Spotlight.Game` 合并当前 Application、Adapters、Composition、Features、UI 和 Data 的 Unity 运行时代码。测试程序集继续独立存在。

这样保留最有价值的硬边界，同时避免为尚不存在的 Feature 和没有替换实现的 Application/Adapter 层付出程序集迁移成本。目录仍然按职责组织，但目录不再自动意味着独立程序集。

## 风险控制

每次删除前先检查生产引用、测试引用、asmdef 引用和 OpenSpec 要求；删除后运行针对性编译和行为测试。任何需要修改存档格式、公开接口、Build Settings 或正式场景的变化必须停下来建立迁移决策，不在本 Change 中隐式扩大范围。