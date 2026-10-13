## Purpose

定义 Demo2 SideView 中可推动动态障碍物的输入、碰撞、物理和固定行水道联动行为。

## ADDED Requirements

### Requirement: 只有显式可推动障碍物可以被推动

系统 MUST 只对挂载 `Demo2PushableObstacle` 的动态 `Rigidbody2D` 响应玩家推动。未挂载该组件的 Rigidbody2D MUST NOT 因玩家 `Push` 输入获得推动力。

#### Scenario: 普通刚体不被误推动

- **WHEN** 玩家按住 `Push` 且前方是普通动态刚体
- **THEN** 推动控制器不对该刚体施加推动力

### Requirement: Push 只在 SideView 持续生效

玩家处于 SideView 且持续按住 `Push` 时，系统 MUST 在物理步推动前方接触的可推动障碍物；松开后停止施力。TopView 或玩法冻结时 MUST NOT 推动。

#### Scenario: SideView 持续推动

- **WHEN** 玩家朝向并接触障碍物后持续按住 `Push`
- **THEN** 障碍物沿玩家水平朝向移动

### Requirement: 推动只使用水平物理力

推动 MUST 使用 Rigidbody2D 物理力，不得直接修改 Transform 或绕过墙体碰撞。推动不得增加 Y 方向速度。

#### Scenario: 障碍物撞墙

- **WHEN** 玩家持续推动障碍物撞向静态墙体
- **THEN** 障碍物不得穿过墙体

### Requirement: 障碍物只阻挡原水道行

可推动障碍物 Collider2D MAY 作为水道移动阻挡。障碍物覆盖候选水格时，该水道行停止水平推进；水 MUST NOT 向上或向下生成额外水道。障碍物移开后，水 MAY 沿原行继续向右推进。

#### Scenario: 推开水道障碍物

- **WHEN** 玩家将障碍物移出水道候选格
- **THEN** 水在后续流动步沿原行继续向右
- **AND** 不生成第三条水道
