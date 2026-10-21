## Purpose

定义 Demo2 SideView 中只有玩家持续按住 `F` 才能授权水平移动、同时保留Y方向重力掉落的 Dynamic 障碍物合同。

## ADDED Requirements

### Requirement: 只有玩家按住F可以授权水平移动

系统 MUST 只对挂载 `Demo2PushableObstacle` 的 Dynamic Rigidbody2D 接受玩家推动请求。未收到当前物理步的玩家请求时，障碍物 MUST 冻结X位置并清零水平速度；玩家普通碰撞、怪物碰撞、水流或其他物理冲量 MUST NOT 产生水平位移。Y方向 MUST 保持未冻结并继续受重力影响。

#### Scenario: 玩家未按F顶住箱子

- **WHEN** 玩家未按 `F` 并持续向箱子移动
- **THEN** 箱子X位置保持不变，同时Y方向仍可因重力下落

#### Scenario: 怪物撞击箱子

- **WHEN** 怪物持续向箱子移动且没有玩家推动请求
- **THEN** 箱子X位置保持不变并阻挡怪物

### Requirement: Push 只在 SideView 持续生效

玩家处于 SideView 且持续按住 `F` 时，系统 MUST 在每个物理步向前方接触的可推动障碍物提交水平推动请求。障碍物 MUST 在该物理步解除X冻结，将X速度设为玩家朝向的1米/秒，并保留当前Y速度。松开 `F` 后的下一物理步 MUST 重新冻结X并清零X速度。TopView、玩法冻结、玩家攻击或受击状态下 MUST NOT 授权推动。

#### Scenario: SideView 持续推动

- **WHEN** 玩家朝向并接触障碍物后持续按住 `F`
- **THEN** 障碍物以1米/秒目标水平速度移动，同时保留Y方向重力

### Requirement: 授权推动继续使用Dynamic物理碰撞

推动 MUST 通过 Dynamic Rigidbody2D 的速度和位置约束实现，不得直接修改 Transform 或使用 Kinematic `MovePosition`。墙体、质量和既有水体阻尼 MUST 继续参与物理解算，障碍物不得穿过静态墙体。

#### Scenario: 障碍物撞墙

- **WHEN** 玩家持续按住 `F` 将障碍物推向静态墙体
- **THEN** 障碍物在墙前停止且不得穿过墙体

### Requirement: 障碍物只阻挡原水道行

可推动障碍物 Collider2D MAY 作为水道移动阻挡。障碍物覆盖候选水格时，该水道行停止水平推进；水 MUST NOT 向上或向下生成额外水道。障碍物移开后，水 MAY 沿原行继续向右推进。

#### Scenario: 推开水道障碍物

- **WHEN** 玩家将障碍物移出水道候选格
- **THEN** 水在后续流动步沿原行继续向右
- **AND** 不生成第三条水道
