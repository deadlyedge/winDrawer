# 阶段十建议：托盘菜单定位、抽屉容量上限与代码收敛

> 状态：10.1、10.2、10.4–10.6 已完成；10.3 的代码清理按用户要求暂缓，审查清单见 [`CODE_REVIEW_STAGE_10.md`](CODE_REVIEW_STAGE_10.md)。10.1 的特殊任务栏/跨屏场景仍需用户实测。
>
> **后续更新：** SunnyUI 已在独立的 `test/sunnyui-simple-controls` 分支全量移除并合并入主分支；该迁移不属于 10.3 的代码清理，10.3 的 A/B 组清理仍未全部实施。迁移过程见 [`SUNNYUI_REMOVAL_IMPACT.md`](SUNNYUI_REMOVAL_IMPACT.md)。
>
> 本文件是阶段 10 的实施与验收依据。工作树中的 `docs/TODOs.md` 属于用户改动，本阶段只读取、不覆盖、不代为删除条目；完成后由用户自行更新。

## 1. 背景与当前状态

- 阶段 9 已完成并提交；`docs/development-history/DP_STAGE_9.md` 仍记录部分手工 UI 回归未执行，本阶段不声称替代或补完那部分验收。
- 当前阶段 10 的范围来自 `docs/TODOs.md` 的四条待办：
  1. 托盘右键菜单在特定任务栏布局、跨屏显示时出现在另一屏幕；
  2. 限制抽屉最大条目数，超出时停止显示并告知用户使用资源管理器；
  3. 精简 README，并提供工作流程截图；
  4. 审查/优化代码结构、函数命名，尝试减少代码量，并提供白皮书。
- 当前构建目标为 `net10.0-windows`，单个 WinForms 项目；阶段 10 当时使用 BCL、WinForms、SunnyUI、Tomlyn；SunnyUI 已在后续迁移中移除，当前依赖为 BCL、WinForms 和 Tomlyn。

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

> 首次实现曾改为手动 `_trayMenu.Show(...)`，结果既没有解决跨屏定位，又因为菜单缺少 owner 而让 Windows 给菜单创建了任务栏按钮。现改为下面的“系统菜单 + 位置纠正”方案。

1. 恢复 `NotifyIcon.ContextMenuStrip = _trayMenu`，让菜单继续由 `NotifyIcon` 的隐藏窗口拥有；这能避免弹出菜单出现在任务栏。
2. 不再订阅右键 `MouseUp` 手动显示菜单。
3. 给菜单增加 `Opening`/`Opened`：
   - `Opening`：记录 `Cursor.Position`，按任务栏边缘计算目标位置，先尝试设置 `Location`；
   - `Opened`：用菜单实际尺寸重新计算目标位置；如果 WinForms 的 `ShowInTaskbar` 路径仍把菜单放到了错误位置，使用 `SetWindowPos` 移动真实窗口（不改变大小、z-order 或焦点）。
4. 目标位置只使用鼠标所在屏幕的 `WorkingArea`，按底部/顶部/左右任务栏选择展开方向并夹取，避免菜单跨屏。
5. 不新增原生 `TrackPopupMenuEx` 实现，不引入第三方托盘库。

### 3.4 验收

- 右键托盘图标后，任务栏**不新增** winDrawer 图标（菜单仍由 NotifyIcon 拥有）。
- 由用户在特殊任务栏布局/跨屏场景实测：菜单出现在点击图标所在屏幕。
- 常规单屏、底部/顶部/左右任务栏下菜单位置合理。
- 菜单关闭、双击显隐、设置、退出均正常。
- 本阶段**不做特殊布局烟测**；不以自动化测试声称跨屏问题已修复。

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
- ~~评估是否移除 SunnyUI 依赖、改用原生 WinForms 控件（高风险，需单独确认）~~：已在后续迁移中完成，见 [`SUNNYUI_REMOVAL_IMPACT.md`](SUNNYUI_REMOVAL_IMPACT.md)；
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
- 10.3 的代码清理范围仍需讨论确认；SunnyUI 移除已在后续独立迁移中完成，不再作为 10.3 候选。
- 截图抓取依赖交互式桌面与用户退出正在运行的实例。

## 12. 实施记录（10.1–10.2）

### 10.1

- 首次实现（手动 `ContextMenuStrip.Show`）**未解决跨屏定位**，并因菜单缺少 owner 导致右键托盘后出现任务栏按钮。
- 修订为：恢复 `NotifyIcon.ContextMenuStrip`，菜单继续由 NotifyIcon 拥有；增加 `Opening`/`Opened`，用鼠标所在屏幕的 `WorkingArea` 计算位置，必要时在 `Opened` 中用 `SetWindowPos` 纠正 WinForms 内部 `ShowInTaskbar` 的定位。
- **已验证**：Debug/Release 完整构建成功；应用可正常启动、初始化托盘并通过 `WM_CLOSE` 退出。
- **基本托盘菜单验证**：向 NotifyIcon 隐藏窗口发送 `WM_TRAYMOUSEMESSAGE` 模拟右键；菜单窗口的 owner 为 NotifyIcon 隐藏窗口（具备不产生任务栏按钮的条件），在副屏坐标下出现在鼠标所在屏幕范围内。
- **未验证**：用户特殊任务栏布局下的实际位置、菜单外部点击关闭、真实任务栏上是否完全无按钮。按约定不做特殊布局烟测，需用户实测。

### 10.2

- 新增 `startup.max_drawer_items`（Integer，默认 200，1–10000，缺失/类型无效回退 200）。
- 修改：`AppConstants`、`SettingsKeys`、`SettingsSnapshot`、`SettingsRepository`（写入与默认值）、`SettingsRepository.Serialization` 注释、根目录 `Drawer-settings.toml`、`docs/CONFIGURATION_FORMAT.md`。
- 枚举改为返回 `DrawerEnumerationResult`；超过上限时在第 `limit + 1` 项处立即停止，不读取其元数据、不排序。
- `DrawerWindow.FileList.cs`：超限时不创建文件条目，只显示 `Too many items (more than N). Open the folder in File Explorer.` 状态；底栏路径仍可打开资源管理器。
- **已验证**：`%TEMP%` 临时 harness 16 项全部通过，覆盖 0/200/201/500 项、缺失目录、limit 0、名称排序、TOML 解析/夹取/类型错误、写回与重开读取。
- Debug/Release 完整构建成功；未执行真实 UI 手工回归。

## 13. 实施记录（10.4–10.6）

### 10.4 白皮书

- 新增 `docs/WHITEPAPER.md`：产品定位、运行模型、模块边界、配置与持久化、抽屉管线、窗口/DPI/主题、拖放、错误/日志、行为不变量、已知技术债和修改指南。
- 内容与 10.1/10.2 后的代码一致；明确记录 `max_drawer_items` 的提前停止与“不显示部分条目”策略。

### 10.5 README 与截图

- README 由约 129 行精简到约 76 行；保留快速开始、抽屉/设置截图、`max_drawer_items`、配置路径、单文件发布和项目结构。
- 新增截图：
  - `docs/images/main-window.png`（400×640）
  - `docs/images/drawer.png`（620×400，含 6 张演示图片缩略图）
  - `docs/images/settings.png`（920×860；为截取完整内容临时加宽了设置窗口）
- 截图使用 `C:\Users\Public\Documents\winDrawer-demo` 演示目录和演示 TOML；未包含真实用户名。
- 用户配置备份/恢复校验：备份与恢复后的 SHA-256 均为 `99278D6D7306BC009537F658DB5DC8F8468B04662D35B16C1E4424C77F1B9BA4`；演示目录和临时配置已删除。
- 截图过程中实际启动了应用，主窗口、抽屉、缩略图和设置页均正常显示，`WM_CLOSE` 后正常退出。

### 10.6 回归与收尾

- Debug 完整构建：0 警告 0 错误。
- Release 完整构建：0 警告 0 错误。
- `dotnet publish -c Release -o Z:\Temp\windrawer-publish-stage10`：退出码 0；产物为 `winDrawer.exe`（120,723,522 字节）、`winDrawer.pdb` 和 `assets/`。
- README、白皮书、审查清单、阶段计划中的相对链接检查通过（missing=0）。
- 10.2 后的临时 harness 16 项检查全部通过；此后仅修改文档和 README，未再改源码。
- 未执行：托盘特殊任务栏/跨屏布局、抽屉右键排序/F5、拖入拖出、真实 >200 项目录的 UI 状态、DPI 切换、登录启动/Default 的完整手工回归。以上仍需在真实使用环境中验证，不能视为已通过。
- `docs/TODOs.md` 未修改，由用户自行更新。


## 14. 后续 UI 迁移记录（不属于 10.3）

- SunnyUI 3.9.8 已从项目文件和源码中移除；当前 UI 仅使用 WinForms 原生控件与项目内轻量控件。
- 新增/保留的 UI 控件：`DrawerListBox`（抽屉列表 owner-draw）、`ThemedScrollBar`（细自绘滚动条）、`ColorPreviewPanel`（颜色预览）、`BehaviorCheckBoxGroup`（行为复选框组）、`ScrollableListView`（列表滚动/视口通知）。
- 按钮改为原生扁平 `Button`；主操作使用 `ButtonFontSize`（10F），其他按钮使用 `CompactButtonFontSize`（9.5F）。
- 设置窗体/抽屉设置对话框改为原生 `Form`、`GroupBox`、`TrackBar`、`CheckBox`、`Label`。
- 抽屉内容区通过“加宽 ListView + 裁剪原生垂直条 + `ThemedScrollBar` 覆盖”隐藏系统滚动条，同时保留原生滚轮和 `GetScrollInfo`。
- 迁移过程的逐批记录、视觉变化、构建验证和待人工回归项见 [`SUNNYUI_REMOVAL_IMPACT.md`](SUNNYUI_REMOVAL_IMPACT.md)。
- 10.3 的 A/B 组代码清理仍未全部实施；不要把本次迁移等同于 10.3 完成。

