## 1. 合并与程序集

- [x] 1.1 合并 `origin/develop`，记录并解决 PR #18 的全部 Git 冲突
- [x] 1.2 保留 `Spotlight.Game` 程序集及 develop 的 `.meta` GUID，删除旧 Features、Demo1 和 Contracts 程序集
- [x] 1.3 为 `Spotlight.Game` 增加 Input System 与 TMP 引用，并让 Demo1 Editor 程序集引用 `Spotlight.Game`
- [x] 1.4 将 `Spotlight.Tests.EditMode` 恢复为 `Spotlight.Domain`、`Spotlight.Game` 及被测 Demo1 Editor 导入器引用

## 2. Demo1 代码迁移

- [x] 2.1 将输入、音乐和局部事件契约迁入 Demo1，移除旧 Application/Contracts 命名空间引用
- [x] 2.2 将 Demo1 场景入口迁移到当前 SceneLoader、音频服务和组合根注入路径
- [x] 2.3 删除无行为的 checkpoint 空壳和未实现的 PlaySfx 路径，迁移全部调用方与场景引用
- [x] 2.4 保持节拍判定、得分、最终高度和完成事实行为，并迁移五组 EditMode 测试

## 3. 场景、输入和音频

- [x] 3.1 为稳定 ID `sample` 与 `demo1` 实现统一场景映射和明确加载结果
- [x] 3.2 接通 Demo1 结算返回 `SampleScene` 的实际场景调用
- [x] 3.3 创建统一 `Input_Actions` 的 `Pulse` 动作并绑定 Space 与鼠标左键，删除损坏的 Demo1 私有动作资产
- [x] 3.4 由组合根持有真实音频播放边界，使用实际 DSP 起点启动 Demo1 歌曲时钟
- [x] 3.5 通过 Unity Editor 更新 Demo1 场景、Prefab、InputActionReference 与 Build Settings 引用

## 4. 内容与资源清理

- [x] 4.1 校验 Demo1 JSON 导入失败不会创建或覆盖有效运行时资产
- [x] 4.2 删除 TMP Documentation/Examples、SceneTemplateSettings、`.vsconfig` 和无关 Renderer2D/PlayerSettings 改动
- [x] 4.3 检查 Demo1 场景、Prefab、音频、配置、JSON、生成资产和全部 `.meta` 完整一致

## 5. 验证与交付

- [x] 5.1 执行 OpenSpec strict 校验和旧程序集/命名空间静态检查
- [x] 5.2 使用 Unity 2022.3.62f3c1 重新导入并确认目标程序集编译通过
- [x] 5.3 运行现有及 Demo1 EditMode 测试并记录结果
- [x] 5.4 在 Editor/PlayMode 验证 Demo1 进入、两种 Pulse、BGM DSP 时钟、判定结算和返回 SampleScene
- [x] 5.5 检查无 Missing Script/Missing Reference，回写验证记录并完成交付清理

### 验证记录

- `openspec validate demo1-develop-integration --strict`：通过。
- 静态检查：无旧 Application/Adapters/Composition/Contracts、旧服务接口、checkpoint 空壳、旧音乐组件和损坏输入资产引用。
- Unity 2022.3.62f3c1 batchmode：最终程序集重新导入和编译通过。
- Unity EditMode：47/47 通过，包含 Pulse 双绑定和 JSON 导入失败不覆盖回归。
- Unity PlayMode：2/2 通过，覆盖 SampleScene 组合根、Demo1 加载和初始化、BGM 实际播放、返回 SampleScene 以及组合根唯一性。
- Unity Editor 场景清理：Demo1 场景无 Missing Script；Prefab/场景静态 Missing Script 标记检查通过。
- 已执行无头 Editor/PlayMode 验证；未进行人工可视截图验收。
