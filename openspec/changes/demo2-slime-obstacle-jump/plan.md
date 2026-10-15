## 实施顺序

1. 在 `Demo2MonsterConfig` 增加3.5米跳跃高度、0.35米前向检测距离和0.3秒落地重试间隔，并纳入有限值校验。
2. 通过Unity Editor更新 `SlimeConfig.asset` 默认数值，不修改史莱姆Prefab物理形状。
3. 在 `Demo2MonsterController` 增加预分配接触结果和身体前向Cast，识别接地与朝树方向的实体障碍。
4. 过滤地面法线、自身和目标树；其他非Trigger前方Collider均允许触发尝试。
5. 用实际重力和3.5米目标高度计算一次性Y速度，空中继续写朝树MoveSpeed水平速度。
6. 记录自动起跳后的落地转换，并在落地后执行0.3秒重试冷却。
7. 保持Biting、HitStun、Dead、冻结、SideView停用和Animator映射不变。
8. 增加箱体跳跃、目标树例外、空中受击、重复墙体重试和禁跳状态PlayMode验证。
9. 编译生产和PlayMode程序集；按用户许可执行目标测试并完成Editor场景烟测。

## 文件范围

```text
Assets/Scripts/Demos/Demo2/Monster/Demo2MonsterConfig.cs
Assets/Scripts/Demos/Demo2/Monster/Demo2MonsterController.cs
Assets/Data/SlimeConfig.asset
Assets/Tests/EditMode/Demo2MonsterConfigTests.cs（若现有配置测试适合扩展）
Assets/Tests/PlayMode/Demo2MonsterControllerTests.cs
openspec/changes/demo2-slime-obstacle-jump/**
```

不修改SlimeMonster Collider、Rigidbody2D、Animator、Layer、场景结构、推动箱规则或Physics2D碰撞矩阵。

## 验证路径

- 配置：非法高度、检测距离和冷却明确失败，不静默夹紧。
- 物理：接地Moving史莱姆在1米箱前获得正确Y速度，X速度继续朝树。
- 例外：目标树不触发跳跃；Ground接触不误触发。
- 状态：Biting、HitStun、Dead与冻结不产生新跳跃，空中受击保留Y速度。
- 重试：自动跳跃落地后0.3秒内不再跳，结束后仍受阻可重试。
- Editor：现有Demo2场景验证推动箱、玩家、其他史莱姆和SideWall。
