## Context

玩家状态模型已经独立维护两段各31/60秒的攻击、0.25秒二击排队起点、0.8秒间隔、受击中断与暂停边界。`PlayerMoveController` 每帧推进状态并写Animator参数。怪物通过 `IMonster.TryTakeDamage` 接收明确伤害结果，史莱姆为30点生命并已有受击硬直、闪白、死亡和一次性死亡事实。玩家与史莱姆目前都在Default Layer，玩家身体Collider也仍位于带剑的根对象上。

## Goals / Non-Goals

**Goals:**

- 由纯状态推进结果精确产生每段一次性命中时刻。
- 用独立近战适配组件执行无持续物理副作用的瞬时范围查询。
- 让朝向、怪物筛选、多Collider去重和SideWall遮挡可明确调试与验证。
- 复用现有怪物伤害入口，不复制生命或死亡规则。

**Non-Goals:**

- 不把剑做成Trigger或修改玩家身体Collider。
- 不增加击退、无敌帧、命中特效、音频或通用战斗框架。
- 不改变连击输入、动作时长和攻击间隔。

## Decisions

### 1. 状态推进返回一次性Strike结果

`Demo2PlayerStateMachine.Advance` 返回轻量值结果，使用 `None`、`Attack1Strike`、`Attack2Strike` 区分本次推进是否首次跨过0.25秒。状态模型在进入每段攻击时重置该段结算标记，并用“上一进度小于0.25且新进度大于等于0.25”处理低帧率跨越。取消状态会丢弃未发生的命中；全局冻结时调用方不推进状态，因此无需额外实时时钟。

不发布静态事件，也不读取Animator normalizedTime。返回值表达当前调用已经发生的事实，调用方同帧消费，不存在订阅生命周期或遗漏回调。

### 2. 瞬时OverlapBox替代剑Collider

新增 `Demo2PlayerMeleeAttack` MonoBehaviour，与Player位于同一GameObject。Player下新增 `AttackPoint` 子节点，默认局部位置为 `(0.9, 0, 0)`；既有根节点负X缩放会自动把它镜像到左侧。组件只在收到Strike结果时以 `AttackPoint.position` 为中心执行一次 `Physics2D.OverlapBox`，不参与FixedUpdate、Trigger或碰撞求解。默认尺寸为1.5×0.8并可在Inspector调整；`OnDrawGizmosSelected` 使用同一中心和尺寸画线框。

持续启停剑Trigger会引入FixedUpdate延迟、`OnTriggerEnter2D`漏报或跨段残留集合；逐帧修改PolygonCollider2D还会把视觉轮廓误当身体物理。因此都不采用。

### 3. PlayerMoveController只负责连接状态结果与近战适配器

`PlayerMoveController` 保持Attack输入与状态推进入口，并持有同对象 `Demo2PlayerMeleeAttack` 的明确引用。每次 `Advance` 返回Strike时调用近战组件；组件从显式 `AttackPoint` 引用取得世界中心，不自行读取Input、改变玩家状态或查找全局对象。

该拆分避免把Physics查询、目标去重和伤害循环继续塞进已经负责两套移动规则的控制器，同时没有引入接口或通用攻击基类。

### 4. Monster Layer限定候选目标

在TagManager空闲层位增加稳定名称 `Monster`，将 `SlimeMonster.prefab` 根对象及其Collider设置到该层。近战组件的目标LayerMask只包含Monster；既有Physics2D碰撞矩阵保持不变，因此史莱姆原有身体碰撞行为不变。

不在Default层查询后再筛组件：玩家、树、箱子和Tilemap都在Default，固定结果缓存可能先被无关Collider占满并漏掉真实怪物。

### 5. 查询结果复用并按怪物实例去重

近战组件复用预分配的Collider列表和怪物列表，每次命中前Clear，不为正常8只波次逐次分配。Collider解析到父级 `Demo2MonsterController` 后，以控制器实例引用线性去重；当前近战规模小，线性扫描比HashSet更轻且不会产生额外对象。

查询容量若超过初始容量，复用List允许扩容以保正确性，后续攻击继续复用扩容后的存储，不以固定数组截断“全部目标”合同。

### 6. 每个目标独立执行SideWall视线检查

候选怪物去重后，从玩家Transform中心向怪物Transform中心执行只包含SideWall Layer的Linecast。命中墙体则跳过该目标；其他候选目标继续结算。使用怪物中心而不是命中Collider边缘，使多Collider不会产生不同遮挡结果。

### 7. 资源修改保持显式

Player场景对象新增 `AttackPoint` 子节点和近战组件，并由 `PlayerMoveController` 引用近战组件。SlimeMonster Prefab迁移到Monster Layer。伤害、区域尺寸、AttackPoint、目标层和遮挡层作为近战组件的私有序列化字段并提供中文Header/XML文档；区域宽高必须是有限正数。这样可在Scene中拖动AttackPoint并通过Gizmo调节尺寸，但本Change不引入ScriptableObject武器系统。

## Risks / Trade-offs

- [0.25秒与动画素材未来漂移] → 代码时刻写入规格；修改Clip节奏时必须显式更新规格、常量和验证，不能偷偷依赖Animation Event。
- [Monster Layer迁移遗漏其他怪物] → 当前只有SlimeMonster一个真实Prefab；新增怪物必须把身体Collider放入Monster层并通过Prefab验收。
- [负缩放影响坐标] → AttackPoint作为Player子节点随根节点自然镜像；OverlapBox尺寸使用绝对世界宽高，不再额外乘朝向，避免二次翻转。
- [墙体边缘判定偏保守] → 合同使用玩家中心到怪物中心；需要剑尖绕墙时应另开范围形状设计，不在本次加入复杂几何。
- [Scene资源修改可能覆盖用户接线] → 只增加近战组件和引用，不重建Player或场景；使用Unity Editor修改并核对现有刷怪装置实例仍保留。

## Migration Plan

1. 先增加状态Strike结果和纯状态验证，再实现近战查询组件。
2. 添加Monster Layer并迁移SlimeMonster Prefab；Physics2D碰撞矩阵不改变。
3. 在Demo2 Player对象创建AttackPoint子节点，挂载并连接近战组件，不修改其他场景对象。
4. 编译并执行获准的目标验证；项目负责人烟测左右、多目标、墙后目标和三次命中死亡。
5. 回滚时移除近战组件、Player接线和Monster Layer迁移，并恢复 `Advance` 的无返回值合同；怪物生命逻辑无需迁移。
