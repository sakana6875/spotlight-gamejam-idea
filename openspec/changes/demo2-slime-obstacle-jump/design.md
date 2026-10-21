## Context

`Demo2MonsterController` 当前在FixedUpdate中仅计算树方向并写水平速度，始终保留Rigidbody2D竖直速度和gravityScale。SlimeMonster使用Dynamic Rigidbody2D、1×0.778606身体BoxCollider2D和冻结旋转；推动箱是1×1的Dynamic身体，SideWall与Ground由Tilemap Collider提供。怪物状态已经区分Moving、Biting、HitStun和Dead，暂停通过Time.timeScale=0冻结Physics2D。

## Goals / Non-Goals

**Goals:**

- 复用现有Dynamic Rigidbody2D，实现可调高度的一次性物理起跳。
- 只根据当前身体接触与前向碰撞决定起跳，不引入路径图或场景扫描。
- 保持目标树、啃食、受击、死亡、暂停和SideView启停合同。
- 所有正常物理步使用预分配查询结果，不制造逐帧GC。

**Non-Goals:**

- 不推算完整抛物线路径或保证越过任意高度墙体。
- 不增加Jump状态、动画、落地特效或空中攻击。
- 不改变其他对象的Layer或碰撞矩阵。

## Decisions

### 1. 接地使用接触法线，障碍使用身体前向Cast

控制器复用预分配 `ContactPoint2D[]` 读取身体当前接触。存在 `normal.y > 0.5` 的非Trigger接触时视为接地，因此Ground、箱顶和其他实体表面都可支撑史莱姆。前向检测使用身体Collider沿树方向做短距离Cast，并只把法线明显反向于移动方向的命中视为前方障碍；地面向上法线即使出现在结果中也不会触发跳跃。

选择身体Cast而不是单根Ray：史莱姆身体有高度，Ray容易从箱体上方或下方漏过。查询不依赖Layer，从而满足用户确认的所有前方碰撞体规则。

### 2. 目标树是唯一显式业务例外

前向命中解析到当前 `_treeTarget` 或其子节点时跳过。树是移动目的地而不是需要翻越的障碍；若对树起跳，怪物会越过目标并破坏Biting流程。自身Collider由ColliderCast自动排除；Trigger不参与查询。

### 3. 起跳速度由目标高度和实际重力计算

配置新增 `JumpHeight=3.5`、`ObstacleDetectionDistance=0.35` 和 `LandingRetrySeconds=0.3`。0.35米提前量让史莱姆在身体接触箱沿前先抬升，避免起跳瞬间被边缘卡住。起跳速度按

```text
sqrt(2 × abs(Physics2D.gravity.y × gravityScale) × JumpHeight)
```

计算，只在合法触发帧写一次Rigidbody2D的Y速度。FixedUpdate继续每帧写朝树的X速度，因而腾空时保持水平前进；不直接修改Transform或累计自定义竖直位移。

若gravityScale或全局Y重力不能产生向下重力，自身配置视为无效并明确禁用，避免假跳跃。

### 4. 落地转换启动重试计时

控制器保存“是否已采样接地”“上一物理步是否接地”“本次腾空是否由自动跳跃发起”和剩余重试秒数。首次采样接地不启动冷却；自动跳跃后从未接地回到接地的物理步才设置0.3秒。这样生成后落地不会额外等待，而跳墙失败不会立即连续弹跳。

重试计时使用FixedUpdate的缩放时间。玩法冻结时FixedUpdate不推进，SideView停用时MonoBehaviour和物理均停止；重新启用后清除接地采样并从实际接触恢复，不伪造一次落地。

### 5. 状态模型不增加Jump

Jump只是Moving状态的物理适配结果，不影响生命、硬直或Animator稳定映射。Biting、HitStun和Dead沿用现有水平停速；它们不会执行起跳判断。空中受击只清零X速度并保留Y速度，硬直结束后若仍在空中则恢复朝树水平速度。

### 6. 配置进入现有怪物资产

跳跃高度、前向检测距离和落地重试间隔属于该怪物类型的可调数值，放入 `Demo2MonsterConfig`，不新建只服务一个史莱姆的第二个ScriptableObject。SlimeConfig写入已确认默认值。Controller通过现有Config读取，不硬编码玩法参数。

## Risks / Trade-offs

- [SideWall无法越过会重复尝试] → 这是用户选择“所有前方碰撞体”和0.3秒重试的明确结果；冷却避免每帧弹跳。
- [多个史莱姆互相触发跳跃] → Monster Collider属于所有前方碰撞体范围，符合确认规则；目标树仍被排除。
- [动态箱体在史莱姆落地时移动] → 接地逐物理步重算，不缓存支撑对象。
- [前向距离过小会撞箱沿] → 默认0.35米，让3.5米跳跃在接触障碍前获得足够抬升时间；资产仍可调。
- [接触数组容量不足] → 正常BoxCollider场景接触很少；实现使用足够容量并只需找到一个向上法线。

## Migration Plan

1. 扩展Config和配置校验，写入SlimeConfig默认跳跃数值。
2. 在Controller中加入预分配接触/前向Cast、目标树过滤、起跳速度和落地冷却。
3. 保持Prefab Rigidbody2D、Collider、Animator与Layer不变。
4. 增加目标PlayMode验证并编译受影响程序集；按用户许可决定是否执行自动测试。
5. 项目负责人使用现有推动箱、玩家、史莱姆和SideWall完成场景烟测。
