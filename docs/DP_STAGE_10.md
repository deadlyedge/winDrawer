# 阶段十建议：托盘菜单定位、抽屉容量上限与代码收敛

> 状态：10.1、10.2 已实施；10.3 审查清单已出（见 [`CODE_REVIEW_STAGE_10.md`](CODE_REVIEW_STAGE_10.md)），等待逐项确认后继续。10.1 的特殊任务栏/跨屏场景仍需用户实测。
>
> 本文件是阶段 10 的实施与验收依据。工作树中的 `docs/TODOs.md` 属于用户改动，本阶段只读取、不覆盖、不代为删除条目；完成后由用户自行更新。

## 1. 背景与当前状态

- 阶段 9 已完成并提交；`docs/DP_STAGE_9.md` 仍记录部分手工 UI 回归未执行，本阶段不声称替代或补完那部分验收。
- 当前阶段 10 的范围来自 `docs/TODOs.md` 的四条待办：
  1. 托盘右键菜单在特定任务栏布局、跨屏显示时出现在另一屏幕；
  2. 限制抽屉最大条目数，超出时停止显示并告知用户使用资源管理器；
  3. 精简 README，并提供工作流程截图；
  4. 审查/优化代码结构、函数命名，尝试减少代码量，并提供白皮书。
- 当前构建目标为 `net10.0-windows`，单个 WinForms 项目；继续使用 BCL、WinForms、SunnyUI、Tomlyn，不引入新框架。

## 2. 已确认的决策

| 编号 | 决策 |
|---|---|
| D1 | 抽屉最大条目数写入 TOML，但**不加入设置页面**；默认 200。 |
| D2 | 超过上限时**不显示任何文件条目**，只显示“条目过多，请用资源管理器打开”的状态；底栏路径仍是打开资源管理器的入口。 |
| D3 | 托盘菜单采用手动 `Show` + 按任务栏方向定位的轻量方案，不重写原生 `Shell_NotifyIcon`/`TrackPopupMenuEx`。 |
| D4 | README 截图由本阶段抓取：使用 `%TEMP%` 演示目录，临时备份并恢复用户设置，截图不包含真实用户路径。 |
| D5 | 代码优化目标是提高可读性、降低代码量；**10.3 必须先产出审查清单并与用户讨论，确认后再改代码**，不直接大改。 |
| D6 | `docs/TODOs.md` 由用户自行更新；本阶段不修改该文件。 |

## 3. 阶段 10.1：托盘菜单跨屏定位

### 3.1 现状

- `src/Infrastructure/TrayController.cs` 仅把菜单赋给 `NotifyIcon.ContextMenuStrip`，没有自绘或自定义定位。
- WinForms 的 `NotifyIcon` 右键流程会调用内部 `NotifyIcon.ShowContextMenu()`，再调用内部 `ContextMenuStrip.ShowInTaskbar(x, y)`。
- `ShowInTaskbar` 与普通 `ContextMenuStrip.Show(Point)` 不是同一条路径：它关闭 `WorkingAreaConstrained`，固定以 `AboveLeft` 计算矩形，按 `Screen.FromRectangle(bounds).Bounds` 反推屏幕并硬约束到该屏幕。多显示器、任务栏跨屏或混合 DPI 时可能选错屏幕。
- 其他原生托盘程序通常使用 `TrackPopupMenuEx`，由 Windows 按屏幕工作区处理，因此通常没有该问题。

### 3.2 目标行为

- 右键托盘图标时，菜单始终出现在鼠标所在屏幕的工作区内。
- 底部任务栏时菜单在光标上方；顶部任务栏时在下方；左右侧任务栏时向屏幕内侧展开。
- 菜单外部点击能正常关闭；双击托盘图标仍执行显示/隐藏。
- 其他菜单项和退出行为不变。

### 3.3 实现方案

1. `TrayController` 继续创建并释放 `ContextMenuStrip`，但**不再赋值给 `NotifyIcon.ContextMenuStrip`**。
2. 订阅 `NotifyIcon.MouseUp`，只处理 `MouseButtons.Right`，调用私有 `ShowTrayMenu()`。
3. `ShowTrayMenu()`：
   - `var screen = Screen.FromPoint(Cursor.Position)`；
   - 按任务栏边缘选择 `ToolStripDropDownDirection`；
   - 将锚点夹取到该屏幕 `WorkingArea`；
   - 调用 `_trayMenu.Show(anchor, direction)`，走普通 `ToolStripDropDown` 的工作区约束路径。
4. 显示失败时记录日志，并回退到 `_trayMenu.Show(Cursor.Position)`。
5. 若实际使用中出现“点击菜单外部第一次只关闭、不激活目标窗口”的焦点问题，再增加隐藏 `NativeWindow` 作为 owner 并 `SetForegroundWindow`，不得激活主窗口。
6. 不新增 P/Invoke 菜单实现，不引入第三方托盘库。

### 3.4 验收

- 由用户在特殊任务栏布局/跨屏场景实测：菜单出现在点击图标所在屏幕。
- 常规单屏、底部/顶部/左右任务栏下菜单位置合理。
- 菜单关闭、双击显隐、设置、退出均正常。
- 本阶段**不做烟测**；不以自动化测试声称跨屏问题已修复。

## 4. 阶段 10.2：抽屉容量上限

### 4.1 TOML 配置

在现有 `[startup]` 分区增加：

```toml
max_drawer_items = 200
```

| 字段 | 类型 | 默认值 | 语义 |
|---|---|---:|---|
| `startup.max_drawer_items` | Integer | `200` | 单个抽屉最多显示的条目数，支持 1–10000。超出范围时夹取；缺失或类型无效时使用 200。不提供设置页面，用户直接编辑 TOML。 |

实现位置：

- `SettingsKeys` 增加 `MaxDrawerItems = "max_drawer_items"`。
- `AppConstants.Defaults` 增加默认值 200、最小值 1、最大值 10000。
- `SettingsSnapshot` 增加 `MaxDrawerItems` 属性、构造参数、`Clone()` 传递，以及 `FromModel` 解析。
- `SettingsRepository.Update` 写入该键；`GetEmbeddedStartupDefaults` 从模板读取；`Default` 按钮因复制 `[startup]` 键而自动重置为模板值。
- 根目录 `Drawer-settings.toml` 模板增加该键。
- `docs/CONFIGURATION_FORMAT.md` 增加字段说明、示例和校验规则。
- 不新增设置页面控件，不改 `SettingsForm` 布局。

### 4.2 显示行为

- 条目数 `<= 上限`：正常显示全部直接子项。
- 条目数 `> 上限`：**不创建任何文件条目**，调用现有 `ShowListStatus(...)` 只显示一行状态，例如：
  - `Too many items (more than 200). Open the folder in File Explorer.`
- 底栏文件夹图标和路径保持可点击，继续通过 Windows Shell 打开文件夹。
- 状态行是列表中的唯一项；双击不打开文件。ToolTip 提示用户使用底栏路径打开资源管理器。
- 正好等于上限时正常显示；超过一个即进入状态模式。
- 刷新、排序切换、DPI 变化、拖放完成后的重新加载都重新判定上限。
- 运行时修改 TOML 后，在下一次打开抽屉或刷新时生效；不实现文件监控或实时重载。
- 超限不弹出模态对话框，不新增窗口提示层。

### 4.3 枚举策略

- 保持单次枚举，发现第 `上限 + 1` 个条目时立即停止，不再读取剩余条目的属性/时间，也不排序。
- 超限时不需要精确总数，日志记录 `more than N items` 即可。
- `DrawerItemEnumerator` 当前用 `null` 表示目录不存在，无法区分“条目过多”；改为小结果类型：

```csharp
internal enum DrawerEnumerationOutcome
{
    Success,
    FolderMissing,
    TooManyItems
}

internal readonly record struct DrawerEnumerationResult(
    DrawerEnumerationOutcome Outcome,
    IReadOnlyList<DrawerItemInfo> Items);
```

- `Success` 时 `Items` 已排序；`FolderMissing` 与 `TooManyItems` 时为空。
- `DrawerWindow.FileList.cs` 根据结果映射到现有的 `Folder not found or inaccessible`、`Too many items` 或正常列表。
- 上限值从 `_settingsRepository.GetSnapshot().MaxDrawerItems` 读取；不缓存到窗口字段，避免设置重载后使用旧值。

### 4.4 验收

使用 `%TEMP%` 下的临时目录验证：

- 0 项：显示 `Folder is empty`；
- 200 项：全部显示；
- 201 项：只显示超限状态，不创建文件条目；
- 大量条目：快速返回状态，不产生大量 `ListViewItem`；
- 目录不存在：仍显示原有缺失状态；
- 刷新、排序切换、DPI 变化、拖放后行为一致；
- 状态行双击不打开文件；底栏路径可打开资源管理器。

纯逻辑（上限解析、201 项提前停止、结果分类）使用 `%TEMP%` 下的临时 smoke harness 验证，不新增测试工程。

## 5. 阶段 10.3：代码审查与精简（交互检查点）

本阶段**先审查、后讨论、再实施**。未与用户确认前，不进行大范围重命名或结构重写。详细审查清单见 [`CODE_REVIEW_STAGE_10.md`](CODE_REVIEW_STAGE_10.md)。

### 5.1 审查产出

先提供一份简明的审查清单，至少包含：

- 死代码、未使用常量/事件、重复 `using`；
- 重复逻辑：DPI 缩放、窗口位置夹取、颜色对话框预设、按钮三态配色、底栏链接挂接；
- 命名一致性：`Populate*` / `Refresh*` / `Apply*` / `Update*` 的语义边界；
- 大文件/大方法：`DrawerWindow.FileList.cs`、`SettingsForm.cs`、`DrawerSettingsDialog.cs`、`SettingsRepository.Persistence.cs`；
- 事件退订、`async void`、取消/代际、`Image`/`Icon`/`ImageList` 释放；
- 无障碍名称缺口（旧审查 L4）；
- 依赖与框架成本：SunnyUI 控件使用面、`ThemePalette` 派生颜色复杂度、设置仓储后台写入复杂度。

### 5.2 讨论规则

- 按“保留 / 合并 / 提取 / 删除 / 暂缓”逐项讨论；
- 优先选择能明显降低阅读成本、且行为不变的改动；
- 不以行数作为唯一目标，不为了抽象而抽象；
- 每次只实施已确认项，单独 diff，构建通过后再继续。

### 5.3 候选方向（待讨论，不预先承诺）

- 提取 DPI 缩放 helper，替换散落的 `dpi / 96d`；
- 提取窗口工作区夹取 helper，统一主窗口/设置窗口/抽屉设置窗口；
- 合并设置窗体与抽屉设置窗体重复的颜色预览、按钮样式、浏览路径逻辑；
- 精简 `DrawerWindow.FileList.cs` 的图像缓存/状态流程；
- 简化 `SettingsRepository` 中不必要的分层或重复表操作；
- 评估是否移除 SunnyUI 依赖、改用原生 WinForms 控件（高风险，需单独确认）；
- 评估 `ThemePalette` 是否可以减少派生色层级；
- 统一必要的注释语言与命名，不重写无关代码。

### 5.4 验收

- Debug/Release 完整构建 0 警告 0 错误；
- 现有临时 harness 通过；
- 行为不变：打开/刷新/排序/缩略图/拖放/设置保存取消/托盘显隐/退出；
- 不引入新项目、DI、MVVM、测试工程或 UI 自动化框架。

## 6. 阶段 10.4：白皮书

新增 `docs/WHITEPAPER.md`（中文），建议包含：

1. 产品定位与非目标；
2. 运行模型：UI 单线程、托盘、热键、显示/隐藏、退出顺序；
3. 模块职责与依赖方向；
4. 配置模型：TOML、默认值、未知键、原子写入、首次模板、`max_drawer_items`；
5. 抽屉内容管线：枚举 → 上限判定 → 排序 → ListView → 可视区图像加载 → 取消/代际 → 图像所有权；
6. 窗口生命周期、DPI、主题与无标题栏边框；
7. 拖放与 Shell 委托；
8. 错误处理、日志、用户提示；
9. 关键不变量：不递归、不主动改文件、Shell 打开、退出单一入口、资源释放顺序；
10. 已知取舍与技术债：WinForms 托盘菜单限制、延迟加载、无测试工程、70% 固定蒙版、容量上限策略。

白皮书必须与最终代码一致；不把计划中的候选改动写成已实现。

## 7. 阶段 10.5：README 精简与截图

### 7.1 README 目标

控制在约 70–90 行：

1. 一句话介绍 + 主窗口截图；
2. `What it does`：最多 5 条，每条一行；
3. `Quick start`：restore/build/run；
4. 抽屉截图、设置页截图及简短工作流；
5. `Configuration`：设置文件路径、`max_drawer_items`、指向 `CONFIGURATION_FORMAT.md`；
6. `Project layout`：简短目录说明；
7. 发布命令移到单独文档或折叠；
8. License。

较长的行为细节移到 `docs/USAGE.md` 或现有配置文档，README 只做入口。

### 7.2 截图抓取流程

截图保存到 `docs/images/`：

- `main-window.png`
- `drawer.png`
- `settings.png`

安全流程：

1. 先请用户退出正在运行的 winDrawer 实例，避免配置写入竞争。
2. 备份 `%USERPROFILE%\.config\winDrawer\Drawer-settings.toml` 到 `%TEMP%`，记录哈希。
3. 在 `%TEMP%` 创建演示目录，例如 `Documents`、`Pictures`、`Projects`，放入通用示例文件和图片。
4. 写入仅含演示抽屉的临时配置，使用中性名称，不包含真实用户名/路径；可设置 `always_on_top = true` 便于截图。
5. 启动应用，等待 UI 稳定，抓取主窗口、抽屉、设置页。
6. 关闭应用，原子恢复原配置，校验哈希与备份一致。
7. 删除演示配置和演示目录；把图片放入 `docs/images/` 并检查无隐私路径。
8. 若抓取失败，不伪造截图；改为请用户提供。

## 8. 阶段 10.6：回归与收尾

### 8.1 构建与自动化

- `dotnet build .\src\winDrawer.csproj`（Debug/Release），必须走完整构建，不使用 `-t:Compile`；
- 若 `bin` 被运行实例锁定，使用 `-o <临时目录>`，不强行终止进程；
- 单文件发布验证输出到临时目录，不覆盖现有 `publish/`；
- `%TEMP%` 临时 harness 覆盖配置解析、上限判定、排序等纯逻辑；
- 不新增测试工程。

### 8.2 手工回归清单

- 托盘菜单：单屏、跨屏、特殊任务栏布局、菜单外部关闭、双击显隐；
- 抽屉容量：0/200/201/大量条目、刷新、排序、DPI、拖放；
- 既有功能：双击打开、右键排序/F5、缩略图、拖入拖出、底栏路径、置顶、热键、设置保存/取消/Default、退出；
- README 截图与链接检查。

### 8.3 收尾

- 不修改 `docs/TODOs.md`；由用户自行更新。
- 不清理 `bin/`、`obj/`、`publish/`、日志或 `.vs/`。
- 如实汇报：改了哪些文件、执行了哪些构建/测试、哪些手工回归未执行。


## 9. 建议顺序

1. 10.1 托盘菜单定位；
2. 10.2 抽屉容量上限（含 TOML/模板/配置文档）；
3. 10.3 审查清单 → 与用户讨论 → 分项重构；
4. 10.4 白皮书；
5. 10.5 README 与截图；
6. 10.6 回归与收尾。

## 10. 本阶段不做

- 不重写原生托盘菜单，不引入第三方托盘/UI 框架；
- 不把 `max_drawer_items` 加入设置页面；
- 不在超限时显示部分条目或弹模态提示；
- 不实现文件系统监控或配置热重载；
- 不删除用户文件、不实现删除/重命名/新建等文件操作；
- 不新增测试工程、DI、MVVM 或多项目分层；
- 不覆盖用户配置、`.vs/`、日志或现有构建产物；
- 不修改 `docs/TODOs.md`。

## 11. 风险与未决项

- 托盘菜单的跨屏修复只能在用户特殊布局中最终确认；本阶段按源码和定位规则实现，不做烟测。
- 手动 `Show` 可能出现焦点/关闭行为差异；预留隐藏 `NativeWindow` 的方案，但不在首次实现中提前加入。
- 严格超限不显示条目会让 201 项的文件夹只能通过底栏路径打开；这是用户确认的行为。
- 10.3 的重构范围需讨论确认；SunnyUI 移除属于高风险候选，不默认实施。
- 截图抓取依赖交互式桌面与用户退出正在运行的实例。

## 12. 实施记录（10.1–10.2）

### 10.1

- `src/Infrastructure/TrayController.cs`：不再把菜单赋给 `NotifyIcon.ContextMenuStrip`；改为右键 `MouseUp` 手动 `Show`，按光标所在屏幕和任务栏方向选择定位，并夹取到工作区。失败时回退到 `Show(Cursor.Position)` 并记录日志。
- **已验证**：Debug/Release 完整构建成功。
- **未验证**：用户特殊任务栏布局下的实际位置、菜单外部点击关闭、隐藏状态下打开菜单。按约定不做烟测。

### 10.2

- 新增 `startup.max_drawer_items`（Integer，默认 200，1–10000，缺失/类型无效回退 200）。
- 修改：`AppConstants`、`SettingsKeys`、`SettingsSnapshot`、`SettingsRepository`（写入与默认值）、`SettingsRepository.Serialization` 注释、根目录 `Drawer-settings.toml`、`docs/CONFIGURATION_FORMAT.md`。
- 枚举改为返回 `DrawerEnumerationResult`；超过上限时在第 `limit + 1` 项处立即停止，不读取其元数据、不排序。
- `DrawerWindow.FileList.cs`：超限时不创建文件条目，只显示 `Too many items (more than N). Open the folder in File Explorer.` 状态；底栏路径仍可打开资源管理器。
- **已验证**：`%TEMP%` 临时 harness 16 项全部通过，覆盖 0/200/201/500 项、缺失目录、limit 0、名称排序、TOML 解析/夹取/类型错误、写回与重开读取。
- Debug/Release 完整构建成功；未执行真实 UI 手工回归。

