# winDrawer 文档索引

> 更新：SunnyUI 移除后整理。现行实现以本索引中“当前实现”部分为准；历史阶段文档保留当时决策与验收记录，不逐行回写成新实现。

## 当前实现

| 文档 | 内容 |
|---|---|
| [`WHITEPAPER.md`](WHITEPAPER.md) | 当前架构、运行模型、模块边界、抽屉管线、主题/DPI、不变量和技术债 |
| [`CONFIGURATION_FORMAT.md`](CONFIGURATION_FORMAT.md) | `Drawer-settings.toml` 当前 schema、默认值、夹取和错误处理 |
| [`../README.md`](../README.md) | 用户功能、快速开始、构建/发布和项目结构 |
| [`SUNNYUI_REMOVAL_IMPACT.md`](SUNNYUI_REMOVAL_IMPACT.md) | SunnyUI 移除的影响评估、逐批试装记录、最终 UI 栈和回归清单 |

### 当前 UI 栈摘要

- 单个 WinForms 项目，无第三方 UI 框架；依赖 BCL/WinForms 与 Tomlyn。
- 项目内控件：`DrawerListBox`、`ThemedScrollBar`、`ScrollableListView`、`ColorPreviewPanel`、`BehaviorCheckBoxGroup`、`EllipsisLabel`。
- 按钮为原生扁平 `Button`；`Cancel`/`Save` 使用 10F 品牌字体，其他按钮使用 9.5F。
- 设置窗体/抽屉设置对话框使用原生 `Form`、`GroupBox`、`TrackBar`、`CheckBox`、`Label`。
- 抽屉内容区保留原生 ListView 垂直滚动条用于滚轮/`GetScrollInfo`，但通过加宽 ListView 裁剪到宿主外侧；`ThemedScrollBar` 覆盖可见区域，轨道约 3px。

## 迁移与历史记录

| 文档 | 状态/用途 |
|---|---|
| [`CODE_REVIEW_STAGE_10.md`](CODE_REVIEW_STAGE_10.md) | 10.3 代码审查快照；C1（移除 SunnyUI）已完成，A/B 组清理仍待讨论 |
| [`DP_STAGE_10.md`](DP_STAGE_10.md) | 阶段 10 计划与实施记录；末尾补充 UI 迁移记录，迁移不属于 10.3 |
| [`DEVELOPMENT_PLAN.md`](DEVELOPMENT_PLAN.md) | 阶段 0–5 历史计划；顶部已标注 SunnyUI 移除和主题方案更新 |
| [`NEXT_DEVELOPMENT_PLAN.md`](NEXT_DEVELOPMENT_PLAN.md) | 阶段 6+ 历史计划；顶部已标注当前原生控件/按钮字体状态 |
| [`DP_STAGE_7.md`](DP_STAGE_7.md) | 阶段 7 实施记录（历史） |
| [`DP_STAGE_8.md`](DP_STAGE_8.md) | 阶段 8 实施记录；顶部标注 `BehaviorCheckBoxGroup` 取代旧控件 |
| [`DP_STAGE_9.md`](DP_STAGE_9.md) | 阶段 9 实施记录；顶部标注原生 Label 取代旧 `UILabel` |
| [`CODE_MODULARIZATION_PLAN.md`](CODE_MODULARIZATION_PLAN.md) | 文件列表拆分方案与实施状态；ScrollableListView 描述已更新为当前裁剪方案 |
| [`CODE_QUALITY_REVIEW.md`](CODE_QUALITY_REVIEW.md) | 阶段 7 后代码质量快照；顶部标注 UI 迁移前状态 |

## 用户所有

| 文档 | 说明 |
|---|---|
| [`TODOs.md`](TODOs.md) | 用户维护的待办列表；开发过程不代为覆盖 |
| [`../AGENTS.md`](../AGENTS.md) | 开发约定；已补充“SunnyUI 不再引入”和滚动条裁剪约束 |

## 文档编写约定

- 现行架构/行为变化优先更新 `WHITEPAPER.md`、`CONFIGURATION_FORMAT.md` 和根 `README.md`。
- 迁移过程、试装批次和视觉变化记录在 `SUNNYUI_REMOVAL_IMPACT.md`，不要再向历史阶段文档追加新的“当前状态”。
- 历史文档保留原验收记录；若与当前实现冲突，在文首增加“后续状态”说明并链接到现行文档。
- 新增文档使用中文，并在本索引登记。
