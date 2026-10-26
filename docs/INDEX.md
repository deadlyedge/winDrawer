# winDrawer 文档索引

> 当前实现与开发约定以“当前文档”部分为准。阶段计划、代码审查、迁移记录和已完成的待办快照已归档到 [`development-history/`](development-history/README.md)，仅用于追溯，不作为当前开发依据。

## 当前文档

| 文档 | 内容 |
|---|---|
| [`WHITEPAPER.md`](WHITEPAPER.md) | 当前架构、运行模型、模块边界、抽屉管线、主题/DPI、不变量和技术债 |
| [`CONFIGURATION_FORMAT.md`](CONFIGURATION_FORMAT.md) | `Drawer-settings.toml` 当前 schema、默认值、夹取和错误处理 |
| [`../README.md`](../README.md) | 用户功能、快速开始、构建/发布和项目结构 |
| [`../AGENTS.md`](../AGENTS.md) | 开发约定；SunnyUI 不再引入、滚动条裁剪等约束 |
| [`TODOs.md`](TODOs.md) | 用户维护的待办列表；开发过程不代为覆盖 |

### 当前 UI 栈摘要

- 单个 WinForms 项目，无第三方 UI 框架；依赖 BCL/WinForms 与 Tomlyn。
- 项目内控件：`DrawerListBox`、`ThemedScrollBar`、`ScrollableListView`、`ColorPreviewPanel`、`BehaviorCheckBoxGroup`、`EllipsisLabel`。
- 按钮为原生扁平 `Button`；`Cancel`/`Save` 使用 10F 品牌字体，其他按钮使用 9.5F。
- 设置窗体/抽屉设置对话框使用原生 `Form`、`GroupBox`、`TrackBar`、`CheckBox`、`Label`。
- 抽屉内容区保留原生 ListView 垂直滚动条用于滚轮/`GetScrollInfo`，但通过加宽 ListView 裁剪到宿主外侧；`ThemedScrollBar` 覆盖可见区域，轨道约 3px。

## 开发历史

| 文档 | 内容 |
|---|---|
| [`development-history/README.md`](development-history/README.md) | 历史文档索引与使用说明 |
| [`DEVELOPMENT_PLAN.md`](development-history/DEVELOPMENT_PLAN.md) | 阶段 0–5 历史计划；含早期 HSLA 需求和 SunnyUI 决策 |
| [`NEXT_DEVELOPMENT_PLAN.md`](development-history/NEXT_DEVELOPMENT_PLAN.md) | 阶段 6+ 历史计划；后续原生控件/主题/托盘等要求 |
| [`DP_STAGE_7.md`](development-history/DP_STAGE_7.md) | 阶段 7 实施与验收记录 |
| [`DP_STAGE_8.md`](development-history/DP_STAGE_8.md) | 阶段 8 实施记录（排序、快捷键、边带、右键菜单） |
| [`DP_STAGE_9.md`](development-history/DP_STAGE_9.md) | 阶段 9 实施记录（拖放、底栏入口、路径截断） |
| [`DP_STAGE_10.md`](development-history/DP_STAGE_10.md) | 阶段 10 计划与实施记录；含 UI 迁移收尾 |
| [`CODE_REVIEW_STAGE_10.md`](development-history/CODE_REVIEW_STAGE_10.md) | 阶段 10 代码审查与 A/B 组清理记录 |
| [`CODE_QUALITY_REVIEW.md`](development-history/CODE_QUALITY_REVIEW.md) | 阶段 7 后代码质量快照；部分结论已被后续迁移取代 |
| [`CODE_MODULARIZATION_PLAN.md`](development-history/CODE_MODULARIZATION_PLAN.md) | 文件列表与抽屉窗口拆分方案及实施状态 |
| [`SUNNYUI_REMOVAL_IMPACT.md`](development-history/SUNNYUI_REMOVAL_IMPACT.md) | SunnyUI 移除的影响评估、逐批试装记录和回归清单 |

## 文档编写约定

- 现行架构/行为变化优先更新 `WHITEPAPER.md`、`CONFIGURATION_FORMAT.md`、根 `README.md` 和 `AGENTS.md`。
- 历史阶段记录只归档到 `development-history/`；不要向历史文档追加新的“当前状态”。如需说明后续变化，在文首加一行并链接到当前文档。
- 若历史文档与当前实现冲突，以当前实现和用户最新要求为准，并先同步更新当前文档。
- 新增文档使用中文，并在本索引登记；历史文档在 `development-history/README.md` 登记。
