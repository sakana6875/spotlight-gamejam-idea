## Purpose

定义第一幕01在侧视和俯视之间共享同一管网状态、通过稳定Slot映射不同空间投影，并在切换视角时保留管段占用与水流进度的行为。

## ADDED Requirements

### Requirement: 第一幕01从侧视开始并保留水平进度

第一幕01 MUST 从SideView开始。切换视角时玩家X坐标 MUST 保留，Y坐标 MUST 替换为目标视图配置的合法落点。

#### Scenario: 首次切到俯视
- **WHEN** 玩家在SideView按R且未搬运水管
- **THEN** 系统完成既有1.5秒转场，保留X并使用TopView合法Y

### Requirement: 只有俯视允许编辑水管

拾取和放置 MUST 只在TopView生效。SideView只显示投影，不得修改管网；搬运中R必须继续被拒绝。

#### Scenario: 侧视按E
- **WHEN** 玩家处于SideView并按E
- **THEN** 不拾取或放置任何水管

### Requirement: 稳定Slot判定正确管位

每个共享Slot MUST 具有稳定slotId、目标网格原点、要求Shape和90度朝向。只有全部条件一致时Slot才视为占用。第一幕01目标Slot使用3米水平直管。

#### Scenario: 3米管放到白色目标位
- **WHEN** 匹配Shape和朝向的3米管登记到目标原点
- **THEN** 对应Slot变为占用

#### Scenario: 管放到红叉位置
- **WHEN** 水管物理合法但原点或朝向不符合目标Slot
- **THEN** 水管允许放置，但目标Slot保持未占用

### Requirement: SideView自动投影共享Slot

SideView投影 MUST 根据slotId显示对应管段。Slot未占用时隐藏目标管段；占用后显示；Slot内水到达后显示通水状态。

#### Scenario: 俯视正确放管后切回侧视
- **WHEN** 目标Slot在TopView变为占用且玩家切回SideView
- **THEN** 侧视对应水平管段显示，并反映共享通水状态

### Requirement: 视图停用不得注销管网状态

TopView根停用 MUST NOT 移除已放置管段或重置水网。只有明确拾取、销毁或场景卸载才允许移除登记。

#### Scenario: 带已放置管道切到侧视
- **WHEN** TopView因视角切换停用
- **THEN** Slot占用和水状态保持，SideView可以继续投影

### Requirement: 转场冻结但不重置水流

视角转场期间 MUST 使用既有暂停理由冻结管网步进；转场完成后 MUST 从同一水状态继续推进。

#### Scenario: 水流中途切换视角
- **WHEN** 水尚未到达终点时玩家切换视角
- **THEN** 转场期间水不推进，完成后从原格继续且不回到水源
