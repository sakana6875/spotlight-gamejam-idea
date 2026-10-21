## 可观察合同

```text
正常活动镜头        CM_Gameplay
固定转场镜头        CM_ViewTransition
基础优先级          Gameplay=20, Transition=10
转场活动优先级      Transition=30
镜头Blend时长       0.5秒，Ease In Out，忽略Time.timeScale
黑幕淡入/淡出       各0.5秒，使用非缩放时间
玩家换位后          Gameplay上一帧状态失效并按真实位置重新构图
相机控制权          仅Cinemachine Brain写主相机位置
```

## 验收规则

- 玩家在正常侧视和俯视玩法中均由同一个Gameplay镜头跟随。
- 玩家离固定转场点任意远时，完成视图切换后仍位于Gameplay镜头可见范围内。
- 返回侧视保留玩家X坐标并使用已配置安全Y；镜头基于该落点重新构图。
- `Time.timeScale=0` 的转场冻结期间，两个0.5秒镜头Blend与黑幕仍可推进。
- 正常完成或中途取消后，Gameplay恢复活动、黑幕清除、转场暂停释放。
- 场景缺少Brain或任一Virtual Camera时明确报错并禁用控制器，不直接移动主相机兜底。
- Main Camera不再挂载旧跟随组件，代码和场景均不存在四个Camera Anchor调用路径。

详细场景见 `specs/demo2-cinemachine-camera/spec.md`。
