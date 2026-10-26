# 开发历史

> 本目录归档已完成的阶段计划、代码审查、迁移记录和待办快照。它们用于追溯当时的决策与验收，**不代表当前实现或开发约定**。
>
> 当前实现请优先阅读：[`../WHITEPAPER.md`](../WHITEPAPER.md)、[`../CONFIGURATION_FORMAT.md`](../CONFIGURATION_FORMAT.md)、[`../../README.md`](../../README.md)、[`../../AGENTS.md`](../../AGENTS.md)。当前文档索引见 [`../INDEX.md`](../INDEX.md)。

| 文档 | 内容 |
|---|---|
| [`DEVELOPMENT_PLAN.md`](DEVELOPMENT_PLAN.md) | 阶段 0–5 历史计划；含早期 HSLA 需求和 SunnyUI 决策 |
| [`NEXT_DEVELOPMENT_PLAN.md`](NEXT_DEVELOPMENT_PLAN.md) | 阶段 6+ 历史计划；后续原生控件/主题/托盘等要求 |
| [`DP_STAGE_7.md`](DP_STAGE_7.md) | 阶段 7 实施与验收记录 |
| [`DP_STAGE_8.md`](DP_STAGE_8.md) | 阶段 8 实施记录（排序、快捷键、边带、右键菜单） |
| [`DP_STAGE_9.md`](DP_STAGE_9.md) | 阶段 9 实施记录（拖放、底栏入口、路径截断） |
| [`DP_STAGE_10.md`](DP_STAGE_10.md) | 阶段 10 计划与实施记录；含 UI 迁移收尾 |
| [`CODE_REVIEW_STAGE_10.md`](CODE_REVIEW_STAGE_10.md) | 阶段 10 代码审查与 A/B 组清理记录 |
| [`CODE_QUALITY_REVIEW.md`](CODE_QUALITY_REVIEW.md) | 阶段 7 后代码质量快照；部分结论已被后续迁移取代 |
| [`CODE_MODULARIZATION_PLAN.md`](CODE_MODULARIZATION_PLAN.md) | 文件列表与抽屉窗口拆分方案及实施状态 |
| [`SUNNYUI_REMOVAL_IMPACT.md`](SUNNYUI_REMOVAL_IMPACT.md) | SunnyUI 移除的影响评估、逐批试装记录和回归清单 |

## 使用约定

- 历史文档只读；不要向其中追加新的“当前状态”。如需说明后续变化，在文首加一行并链接到当前文档。
- 当前架构、配置、构建或行为发生变化时，更新 `WHITEPAPER.md`、`CONFIGURATION_FORMAT.md`、根 `README.md` 或 `AGENTS.md`，并在 `../INDEX.md` 登记。
- 历史文档之间的相对链接已按本目录重写；引用当前文档时使用 `../`，引用仓库根文件时使用 `../../`。
