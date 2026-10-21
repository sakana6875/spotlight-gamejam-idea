## 实施顺序

1. 新增 `BootstrapEntry` MonoBehaviour，声明RequireComponent、DisallowMultipleComponent和较晚执行顺序。
2. Awake取得同对象SessionRoot并在异常配置时明确禁用。
3. Start校验非空稳定ID、幂等初始化SessionRoot并调用其SceneLoader。
4. Started与AlreadyActive成功返回；其他SceneLoadResult记录ID和结果。
5. 严格校验OpenSpec并编译三个程序集。
6. 提供BootStrap场景Inspector接线和启动烟测步骤。

## 接线

```text
BootStrap.unity
└── SessionRoot
    ├── SessionRoot
    └── BootstrapEntry
        Initial Scene Id = demo2
```

Build Settings现状已经满足：

```text
0 Assets/Scenes/BootStrap.unity
1 Assets/Scenes/demo2.unity
```

BootStrap场景不得添加SessionSmokeEntry；demo2场景不得添加第二个SessionRoot。

## 验证路径

- `openspec validate production-bootstrap-entry --strict`
- 编译 `Spotlight.Game`、`Spotlight.Tests.EditMode`、`Spotlight.Tests.PlayMode`
- Editor从BootStrap Play：自动进入demo2；Hierarchy保留一个DontDestroyOnLoad SessionRoot；Console无加载错误。
