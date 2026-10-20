## Why

Demo2史莱姆目前只会水平朝树移动，遇到箱子、玩家、其他怪物或侧墙时会持续顶住，无法表现基本的障碍规避。需要在不引入寻路系统和新动画状态的前提下，让移动中的史莱姆利用现有Rigidbody2D自动起跳并继续接近树苗。

## What Changes

- 史莱姆处于 `Moving`、已经接地且前方身体Cast命中非Trigger碰撞体时，自动执行一次跳跃。
- 地面接触不视为前方障碍；目标树作为唯一业务例外，不触发跳跃，保留到树后啃食流程。
- 箱子、玩家、其他怪物、SideWall及其他前方实体Collider均允许触发尝试，符合“所有前方碰撞体”规则。
- 默认跳跃高度为3.5米，通过现有重力计算起跳速度；数值进入 `Demo2MonsterConfig` 并可调。
- 腾空期间继续以 `MoveSpeed` 朝树保持水平移动，竖直速度只由起跳和Physics2D重力决定。
- 落地后0.3秒内不得再次起跳；仍被阻挡时冷却结束后可以重试。
- `Biting`、`HitStun`、`Dead`、玩法冻结、GameObject停用或Rigidbody2D停止模拟时不得起跳。
- 不新增Jump状态或动画，空中继续使用 `Moving` 状态与既有移动动画。
- 增加前方碰撞、目标树例外、空中速度、落地冷却和状态禁止边界验证。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `demo2-small-monster`: 在既有Moving物理行为中增加接地、前方障碍检测、自动起跳、空中水平移动和落地重试规则。

## Impact

- 修改 `Demo2MonsterConfig` 和 `SlimeConfig.asset`，增加跳跃高度、前向检测距离和落地重试间隔。
- 修改 `Demo2MonsterController`，使用预分配ColliderCast结果执行接地与前方碰撞判断。
- 修改怪物EditMode/PlayMode测试，保持生命、啃食、受击、死亡与视角启停合同不变。
- 不新增程序集、Layer、Unity包、第三方依赖或场景对象。

## 非目标

- 不实现A*、NavMesh、平台路径搜索、绕路、攀爬、二段跳、冲刺或飞行。
- 不新增跳跃、起飞、下落或落地Animator状态与动画素材。
- 不让史莱姆主动跳过目标树，不修改啃食距离、伤害、生命或波次规则。
- 不保证3.5米轨迹能翻过无限高SideWall；按已确认规则，无法越过时允许落地冷却后继续尝试。
- 不修改玩家跳跃、推动箱授权或全局暂停实现。
