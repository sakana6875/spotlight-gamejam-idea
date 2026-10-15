# Demo2史莱姆障碍跳跃规格

本Change扩展既有 `demo2-small-monster` Moving物理行为。完整增量合同见 `specs/demo2-small-monster/spec.md`。

## 已确认规则

```text
可触发对象      所有前方实体非Trigger Collider
唯一业务例外    当前目标树
默认跳跃高度    3.5米
空中水平行为    继续以MoveSpeed朝树移动
落地重试间隔    0.3秒
允许起跳状态    Moving
禁止起跳状态    Biting / HitStun / Dead / 全局冻结
动画            继续使用Moving，不增加Jump状态
```

## 不变量

- 地面支撑接触不等于前方障碍。
- 起跳只写一次竖直速度，重力继续由Physics2D处理。
- 目标树不得触发跳跃，避免破坏啃食流程。
- 首次生成落地不产生无条件重试冷却。
- 无法越过SideWall时允许按0.3秒间隔继续尝试。
