# winDrawer 代码质量审查（阶段 7 完成后）

> 目的：在阶段 7.6 收尾后对 `src/` 做一次整体代码质量审查，记录结论与可执行的优化建议。
> 性质：**本次审查只读代码并运行既有验证手段，未修改任何产品行为**；下列条目均给出来源与证据，供后续按优先级决定是否实施。

## 1. 审查范围与方法

- 对象：`src/` 下全部 31 个 `.cs`（合计 6,846 行，其中 Designer 867 行）与 3 个 `.resx`（MainWindow/DrawerWindow/SettingsForm）及项目文件；清点见附录 A。
- 方法：
  1. 逐文件阅读（含各分部文件与 Designer），核对职责边界与调用关系；
  2. `dotnet build -c Debug` / `-c Release` 检查编译告警；
  3. 针对性检索：未引用成员、异常吞噬（`catch { }`）、`async void`、`MessageBox` 与日志调用密度、重复工具函数；
  4. 运行既有临时 smoke harness（32 项，`%TEMP%`）与离屏渲染探针（构造真实窗体、强制 hover 状态）验证运行时行为；
  5. 用元数据读取器核对程序集嵌入资源、用 git blob 哈希核对文件搬移等价性。
- 限制：**未做**大目录性能/内存压测、**未做**非 96 DPI 实机与多显示器手工回归、**未做**长时运行（托盘常驻数周）观察。

## 2. 总体结论

**质量基线良好**：构建 0 告警 0 错误；可空性全面启用；线程与资源所有权边界清楚；配置写入具备原子性；文件列表的取消/过期结果处理是双保险（token + generation）。近期完成的分部文件拆分与目录分层让最大文件降到 589 行，职责可一句话解释。

**主要短板集中在三类**：① 退出/异常路径过于激进或过于安静（`Process.Kill`、全局异常只写日志）；② 重复的小工具代码（路径比较、色板、按钮样式、键盘激活）与死代码；③ 日志与生命周期收尾（日志无级别/上限、仓储未释放、部分事件未退订）。

分级统计：高 4 项、中 8 项、低 6 项。**高优先级的 H1–H4（第 9 节）与中优先级的 M1–M8（第 10 节）已于 2026-10-16 修复并通过验证**；低优先级 L1–L6 状态不变。

## 3. 问题清单

### 3.1 高优先级（正确性/可诊断性/数据安全）

**H1 应用退出路径使用 `Process.GetCurrentProcess().Kill()`，且同一模式重复三次** ｜ ✅ 已修复（2026-10-16，见第 9.1 节）
- 位置：`src/Forms/MainWindow.Settings.cs:170-192`（设置窗体请求退出时的处理）。
- 现象：
  ```csharp
  try { Process.GetCurrentProcess().Kill(); } catch { try { Environment.Exit(0); } catch { } }
  ```
  三段几乎相同的代码分别用于"刷新超时""刷新完成""出现异常"分支。
- 影响：① 直接 kill 进程会跳过 `finally`、跳过日志缓冲与任何未完成的 UI 清理，且不会触发 `FormClosed` 链路；② 逻辑分散难以核对；③ 一旦未来在退出时需要保存状态，这里会成为静默丢数据的入口。当前靠 `FlushAsync(500ms)` 兜底，属于"先写盘再硬杀"，脆弱。
- 建议：抽出单一 `ShutdownApplication(string reason)` 私有方法（集中日志与兜底顺序），优先走"请求正常关闭 + 主窗体 `FormClosing` 里的 2 秒 flush"；仅在该路径超时后再落到一次 `Environment.Exit`，并把 `Kill` 降级为最后手段且只保留一处。若保留 Kill，至少在 kill 前记录一条 `LogWarning`（当前连日志都可能来不及写入）。

**H2 全局异常只写日志，不向用户提示** ｜ ✅ 已修复（2026-10-16，见第 9.2 节）
- 位置：`src/Program.cs:11-19`（`Application.ThreadException` 与 `AppDomain.UnhandledException`）。
- 证据：阶段 7.3 期间的"设置窗口打不开"就是 `MissingManifestResourceException` 被这里记入日志，UI 上表现为"点了没反应"，用户只能从日志发现。
- 影响：UI 线程异常被吞掉后进程继续运行在不确定状态，用户无任何反馈，问题定位只能靠翻日志。
- 建议：`ThreadException` 分支在记录后弹一次可理解的对话框（例如"发生内部错误，操作未完成；详情见日志 <路径>"，并做去重，避免连环弹窗）；`UnhandledException` 保持只记录（进程即将终止）。可顺带 `Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)` 明确语义。

**H3 `SettingsRepository` 从未被释放** ｜ ✅ 已修复（2026-10-16，见第 9.3 节）
- 位置：`src/Forms/MainWindow.cs`（`FormClosed` 只释放字体与托盘控制器）；检索确认全仓库没有任何 `_settingsRepository.Dispose()` 调用。
- 影响：仓储持有后台写盘 `Task`、`CancellationTokenSource` 与 `SemaphoreSlim`；虽然退出前会 `SavePendingChanges(2s)`（数据安全 OK），但 writer 循环会一直活到进程结束，`Dispose` 中的"最后一次 flush + 取消 + 等待"逻辑实际从未执行。
- 建议：在 `MainWindow` 的 `OnFormClosed`/`Dispose` 中按"先 flush 再 Dispose"顺序调用 `_settingsRepository.Dispose()`（或由 `Program` 在 `Application.Run` 返回后释放），并用日志确认。

**H4 日志没有级别开关与大小上限** ｜ ✅ 已修复（2026-10-16，见第 9.4 节）
- 位置：`src/Infrastructure/DrawerLogger.cs`（每次调用都 `File.AppendAllText` + `lock`，`LogDebug` 无过滤）。
- 证据：本机一次开发会话的 `Drawer.log` 已达 **515 KB**（多为 DEBUG 行）。托盘常驻的应用会持续追加。
- 影响：长期运行下日志无限增长（无轮转），磁盘与打开/写入开销随之上升；排查时噪声大。
- 建议：① 增加最小级别（例如 Release 默认 Information，`--verbose` 或环境变量打开 Debug）；② 设置大小上限（如 1 MB 后轮转为 `Drawer.log.1`，只保留一份）；③ 保留 `lock` + 失败不影响主流程的现有语义。

### 3.2 中优先级（结构与可维护性）

**M1 `SettingsButton_Click` 是整个代码库最大的单方法（约 214 行）** ｜ ✅ 已修复（2026-10-16，见第 10.1 节）
- 位置：`src/Forms/MainWindow.Settings.cs:23-234`。内含 4 个本地函数（预览、窗口高度预览、锁定预览、置顶预览、默认值重置）与完整退出序列。
- 影响：读一屏才能确认设置流程语义；本地函数无法被单测；退出序列与 UI 逻辑混在一起（见 H1）。
- 建议：拆为私有方法：`ShowSettingsWindow()`、`WireSettingsPreviewHandlers(SettingsForm)`、`HandleStartupDefaultsReset(SettingsForm)`、`ShutdownApplication(string)`；本地函数改为私有方法后，退订逻辑也能用同一引用（见 M7）。

**M2 重复的小工具代码 4 组** ｜ ✅ 已修复（2026-10-16，见第 10.2 节）
| 重复内容 | 位置 | 建议 |
|---|---|---|
| 路径比较（展开环境变量 + Trim + Windows 忽略大小写） | `src/Configuration/SettingsRepository.cs:214`、`src/Forms/MainWindow.DrawerList.cs:344` | 提取到单一位置（如 `Configuration/PathComparison.cs` 或 `Infrastructure/`），两边复用 |
| 颜色对话框的 16 个自定义色 | `src/Forms/SettingsForm.cs:332`（`GetThemeDialogCustomColors`）、`src/Forms/DrawerSettingsDialog.cs:548`（`GetDrawerDialogCustomColors`） | 合并为一个 `BuildColorDialogCustomColors(Color current, Color? selected)` |
| 按钮三态配色与圆角/光标 | `src/Forms/SettingsForm.cs:149`、`src/Forms/DrawerSettingsDialog.cs:358`（`ConfigureButtonStyle`）、`DrawerWindow.ApplyThemeToButton`、`MainWindow.ApplyTheme` 内联 | 提取 `ApplyButtonPalette(UIButton, fill, hover, pressed, rect)`（DrawerWindow 还需可读文字色，可加参数） |
| 颜色预览的键盘激活（Enter/Space → 执行） | `src/Forms/SettingsForm.cs:320`、`src/Forms/DrawerSettingsDialog.cs:251/502` | 提取 `ActivateOnEnterOrSpace(KeyEventArgs, Action)` |

- 影响：同一视觉/行为规则有 2-4 份实现，修改时容易只改一处（历史上已经出现过主题色相关规则分散的问题）。
- 建议：新增一个约 80-120 行的静态帮助类（例如 `Theming/UiInteropHelpers.cs` 或 `Forms/DialogPresentationHelpers.cs`），只放"无状态、可复用"的部分；**不要**为此引入样式引擎或按控件自绘体系。

**M3 `SettingsSnapshot` 的 8 参数位置构造函数容易写错** ｜ ✅ 已修复（2026-10-16，见第 10.3 节）
- 位置：`src/Configuration/SettingsSnapshot.cs:6-17`；调用方有 4 处，其中 `SettingsRepository.cs`（`GetEmbeddedStartupDefaults`）与 `MainWindow.Settings.cs:199` 使用**位置参数**。
- 影响：新增字段（如 `alwaysOnTop`）时必须人工核对每一处位置参数；顺序写错会静默产生错误设置（类型相同的 bool 尤其危险）。
- 建议：调用处统一改为命名参数（本次 `FromModel` 已经是命名参数，可作范本）；或在保持类型不变的前提下增加一个 `Clone()` 之外的 `With(...)` 风格方法。改动小、收益稳定。

**M4 TOML 键名散落在多个分部文件中** ｜ ✅ 已修复（2026-10-16，见第 10.4 节）
- 位置：`"startup"`、`"drawers"`、`"window_color"` 等字符串出现在 `SettingsRepository.cs`、`.Persistence.cs`、`.Serialization.cs`、`SettingsSnapshot.cs` 中。
- 影响：键名拼写错误不会被编译器发现；新增字段需要跨 4 个文件检索。
- 建议：新增 `Configuration/SettingsKeys.cs`（或 `TomlKeys`）集中常量，序列化/解析/重置三处引用同一组常量。

**M5 死代码（已确认无引用）** ｜ ✅ 已修复（2026-10-16，见第 10.5 节）
| 成员 | 位置 | 说明 |
|---|---|---|
| `SettingsForm.LaunchOnStartup` | `src/Forms/SettingsForm.cs:67` | 仅声明；启动项实际由 `SettingsSnapshot.StartWithWindows` 驱动 |
| `SettingsForm.SelectedAccentColor` | `src/Forms/SettingsForm.cs:65`（写于 368/379/502） | 只写不读；主题预览走 `ThemePreviewEventArgs` |
| `SettingsRepository.EnqueueSave()` | `src/Configuration/SettingsRepository.Persistence.cs:12` | 无调用方（`Update`/抽屉操作已各自 signal） |
| `SettingsRepository.Save()` | `src/Configuration/SettingsRepository.Persistence.cs`（同步保存） | 无调用方；其注释还建议"生产代码优先用 EnqueueSave/FlushAsync" |
| `AppConstants.Directories.Fonts`、`DrawerUi.ItemHoverBlendAmount`、`ResizeGripWidth`、`ResizeGripHeight`、`ContentHorizontalPadding`、`ContentVerticalPadding` | `src/Configuration/AppConstants.cs:27/66-70` | 自绘卡片列表时期的遗留常量，已无引用 |
- 建议：删除或在删除前确认无外部（脚本/文档）依赖；`Save()` 若打算保留为"运维/测试入口"，请补一条注释说明用途，否则一并删除。

**M6 拖动插入提示的 `DragLeave` 处理器从未订阅（顺带是真实小缺陷）** ｜ ✅ 已修复（2026-10-16，见第 10.5 节）
- 位置：`src/Forms/MainWindow.DrawerList.cs:232` 定义 `DrawerListBox_DragLeave`，但 `MainWindow` 构造函数只订阅了 `DragEnter/DragOver/DragDrop`（`src/Forms/MainWindow.cs:52-54`）。
- 影响：把抽屉拖动到列表之外再放开时，插入位置提示条可能残留到下一次操作（虽然 `DragDrop` 的 `finally` 与 `PopulateDrawerList` 会清理，但"拖出列表"这条路径没有清理点）。
- 建议：订阅 `DragLeave += DrawerListBox_DragLeave`（一行修复），或在构造处明确删除该处理器。

**M7 事件退订不一致** ｜ ✅ 已修复（2026-10-16，见第 10.1 节）
- 位置：`src/Forms/MainWindow.Settings.cs:138-145` 退订了 `ThemePreviewChanged`、`WindowHeightPreviewChanged`、`AlwaysOnTopPreviewChanged`、`StartupDefaultsResetRequested`，但**没有**退订 `WindowLockPreviewChanged`。
- 影响：处理器生命周期不一致，未来若宿主窗体复用会引入隐性泄漏；也让"谁负责退订"的规则不清晰。
- 建议：把 5 个处理器（含锁定预览）改为私有方法并统一退订；或在 `SettingsForm.Dispose` 内统一清空自身事件。

**M8 `MessageBox` 调用密集且标题/按钮组合靠手工重复** ｜ ✅ 已修复（2026-10-16，见第 10.5 节）
- 位置：`src/Forms/MainWindow.Drawers.cs`（200 行内 11 处）、`MainWindow.Settings.cs`（4 处）、`DrawerWindow.cs`（2 处）、`DrawerWindow.FileList.cs`（1 处）、`SettingsForm.cs`（3 处）。
- 影响：提示文案、标题、图标组合靠复制粘贴；后续统一文案或本地化需要逐处修改。
- 建议：增加极薄的 `UiMessages.ShowError(owner, title, message)` / `ShowWarning` / `ShowInfo` 包装（约 30 行），保留 WinForms 原生对话框语义，不做自绘提示层。

### 3.3 低优先级（一致性与可读性）

**L1 窗体生命周期钩子风格不一致**
- `DrawerWindow` 重写 `OnShown`/`OnFormClosing`/`OnFormClosed`，而 `MainWindow` 在构造函数中用 lambda 订阅 `FormClosing`/`FormClosed`（`src/Forms/MainWindow.cs:75-101`，其中释放字体与托盘）。
- 建议：`MainWindow` 也改为 override `OnFormClosed`，让"释放顺序"（字体 → 托盘 → 仓储）集中可读（配合 H3）。

**L2 主题色经过取整 HSL 往返，且强调色是"配置色 -18% 亮度"的派生色**
- 位置：`src/Theming/ThemePalette.cs:48-52`（`FromColor` → `ToHsl` 取整）与 `:98-130`（`accent = FromHslInternal(hue, saturation, lightness - 0.18f, 1f)`）。
- 影响：① 按钮/边框实际使用的颜色与设置面板预览里显示的 `#RRGGBB` 并不完全相同（差一个亮度档）；② H/S/L 取整带来 ≤0.5% 的额外误差。
- 判断：这属于既有主题设计（`borderOffset`/`hoverOffset` 等派生规则），**不建议在无需求时改动**。建议二选一：在代码注释与 README 里写明"强调色为配置色派生的深色变体"；或（若你期望按钮底色就是所选颜色）让 `Accent` 直接使用解析出的原始 `Color`，仅派生 hover/pressed/背景。

**L3 可见区间查询为 O(n) 原生调用**
- 位置：`src/Controls/ScrollableListView.cs:163-191`；每次防抖都会对全部条目各发一次 `LVM_GETITEMRECT`。
- 现状可接受（已在注释中说明取舍，且只在滚动停止后执行一次）。若未来出现上万条目的卡顿，可改为"按行列几何保守估算区间"（允许少量多取，但不得漏掉可见项）。

**L4 无障碍名称/描述覆盖不均衡**
- 已覆盖：`DrawerWindow`（路径入口、文件夹图标、刷新/关闭按钮、滚动条）、`DrawerSettingsDialog`（路径显示、颜色预览）、`SettingsForm`（颜色预览）。
- 未覆盖：`MainWindow` 的 `settingsButton`、`drawerListBox`、`addDrawerButton`（`src/Forms/MainWindow.Designer.cs:38-40`）；`SettingsForm` 的 `windowHeightTrackBar`、`opacityTrackBar`、`behaviorCheckBoxGroup`（`src/Forms/SettingsForm.Designer.cs:42-44`）。
- 建议：按钮补 `AccessibleName` + 动作描述；滑块补名称与单位/范围（如 "Window opacity, 0 to 100 percent"）。

**L5 少量逻辑尺寸仍是方法内字面量**
- `DrawerWindow.GetImageSize` 的 `48`、`GetFolderIconSize` 的 `24`（已有局部 `const` 命名，可读性可接受）。若后续还要增加同类尺寸，建议并入 `AppConstants.DrawerUi` 统一管理。

**L6 没有自动化测试工程**
- 纯逻辑（排序、颜色解析/格式化、快照解析、路径比较）目前依赖 `%TEMP%` 下的临时 harness。若希望回归可持续，可评估新增只引用纯逻辑源文件的测试项目（不含 UI 自动化）；这与既有"不新增项目"约束冲突，**需你确认后再做**。

## 4. 建议的处理顺序

| 顺序 | 项 | 预估工作量 | 风险与注意点 |
|---|---|---|---|
| 1 | M5 死代码清理 + M6 `DragLeave` 订阅 | ~30 分钟 | 极低；删除前确认无脚本/文档引用 |
| 2 | H2 全局异常提示 + H3 仓储释放 | ~1 小时 | 低；只补提示与释放，不改设置语义 |
| 3 | H1 统一退出路径 | 1–2 小时 | 中；需手工验证"托盘退出""设置内退出""正常关闭"三条路径 |
| 4 | M2 抽取重复工具 + M8 统一提示入口 | 2–3 小时 | 低–中；纯搬移与替换，建议分两次提交 |
| 5 | M3 命名参数 + M4 TOML 键名常量 | ~1 小时 | 低；改动面广但机械 |
| 6 | M1 拆分 `SettingsButton_Click`（含 M7 统一退订） | ~2 小时 | 中；设置窗体高频，需 Save/Cancel/Default 回归 |
| 7 | H4 日志级别与轮转 | 1–2 小时 | 低；注意保持错误可见性 |
| 8 | L1/L2/L4/L5 一致性收尾 | 1–2 小时 | 低 |

> 2026-10-16 更新：上表第 1–7 步对应的 M5/M6、H2/H3、H1、M2/M8、M3/M4、M1/M7、H4 均已完成并通过验证（见第 9、10 节）；仅第 8 步的低优先级一致性收尾尚未处理。

## 5. 建议保持不动的部分

- 单个 WinForms 项目 + BCL/SunnyUI + Tomlyn 的技术选型；不引入 DI、MVVM、多项目分层。
- `ScrollableListView` 的原生滚动/视口实现，以及"全量轻量元数据 + 视口图像延迟加载"的列表管线（token + generation 双保险、图像所有权清晰）。
- 配置层行为：原子写入（临时文件 + 覆盖移动）、损坏 TOML 不覆盖原文、未知键保留、Default 重置语义。
- 已完成的分部文件与目录分层（当前最大文件 589 行；`SettingsRepository` 5 文件、`MainWindow` 5 文件、`DrawerWindow` 2 文件）。
- Shell 图标 + 缩略图回退、且不锁定源文件的实现方式。

## 6. 附录 A：代码清点（行数，含 Designer）

| 目录 | 文件 | 行数 |
|---|---|---:|
| `src/` | `Program.cs` | 55 |
| `Configuration/` | `AppConstants.cs` / `DrawerDefinition.cs` | 72 / 12 |
| | `SettingsRepository.cs` / `.Drawers.cs` / `.Persistence.cs` / `.Serialization.cs` | 225 / 312 / 368 / 129 |
| | `SettingsSnapshot.cs` | 185 |
| `Controls/` | `ScrollableListView.cs` | 284 |
| `Drawers/` | `DrawerItemInfo.cs` / `DrawerItemEnumerator.cs` / `DrawerItemImageLoader.cs` | 7 / 101 / 121 |
| `Forms/` | `MainWindow.cs` / `.Designer.cs` / `.DrawerList.cs` / `.Drawers.cs` / `.Settings.cs` / `.Tray.cs` | 418 / 192 / 378 / 200 / 236 / 64 |
| | `DrawerWindow.cs` / `.Designer.cs` / `.FileList.cs` | 312 / 255 / 589 |
| | `SettingsForm.cs` / `.Designer.cs` | 573 / 420 |
| | `DrawerSettingsDialog.cs` | 582 |
| `Infrastructure/` | `DrawerLogger.cs` / `StartupRegistration.cs` / `TrayController.cs` | 72 / 73 / 195 |
| `Theming/` | `ThemePalette.cs` / `BrandingFont.cs` / `ShellIcon.cs` / `ThemePreviewEventArgs.cs` | 229 / 123 / 58 / 6 |
| 合计 | 31 个 `.cs`（其中 Designer 867 行） | **6,846** |

结构观察：最大文件 589 行；最大的单方法是 `MainWindow.Settings.cs` 的 `SettingsButton_Click`（约 214 行，见 M1）；`Forms/` 集中了全部窗口与其分部文件，`Configuration/` 只放设置数据与持久化。

## 7. 附录 B：本次审查实际执行过的验证与证据

| 手段 | 命令/工具 | 结果 |
|---|---|---|
| 编译告警 | `dotnet build src/winDrawer.csproj -c Debug` / `-c Release` | 0 警告 0 错误（重组目录后再次确认） |
| 单元级 smoke | `%TEMP%\winDrawer.SmokeHarness`（链接真实源文件） | **32/32 PASS**，exitCode 0：排序契约（时间降序/同时间名称与路径/无时间置后）、枚举降级与取消、缩略图回退与源文件解锁、`always_on_top` 缺失/真假/类型错误解析与写盘往返与 Default 重置、真实 `ListView` 可见区间 |
| 离屏渲染探针 | `%TEMP%\winDrawer.AppProbe`（引用构建产物 `winDrawer.dll`） | `SettingsForm` 可构造（设计器资源可达）、标题字体 `Lobster`、`ItemSize 170×34`/`RowInterval 12`、抽屉设置对话框只读路径 + `Browse 436×67` 独立行、无关闭图标 |
| 单文件发布 | `dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o %TEMP%\...` | 成功；产物 `winDrawer.exe`（4,773,850 字节）内含 6 项嵌入资源（模板/字体/图标 + 3 个 WinForms 资源集） |
| 嵌入资源名 | `System.Reflection.Metadata` 枚举清单资源 | `winDrawer.SettingsForm.resources`、`MainWindow.resources`、`DrawerWindow.resources`、`Branding.Lobster-Regular.ttf`、`DefaultSettings.toml`、`drawer.icon.4.ico` |
| 目录整理等价性 | `git hash-object` 对比 `HEAD:<旧路径>` | 20 个搬移文件 blob 哈希全部一致（纯搬移） |
| 静态检索 | 未引用成员、`catch { }`、`async void`、`MessageBox`/日志密度、`AccessibleName` 覆盖 | 见各条目"位置/证据"；未引用成员与无障碍缺口均为检索确认，非估计 |
| 日志体量 | `%USERPROFILE%\.config\winDrawer\Drawer.log` | 515 KB（H4 依据） |

## 8. 说明与后续

- 本文件只记录审查结论与建议，**未改动任何产品代码**；是否实施、以及实施顺序请按第 4 节挑选。
- 审查时间：2026-10-16（阶段 7.6 收尾后）。
- 覆盖范围内的已知空白（第 1 节"限制"）：性能/内存压测、非 96 DPI 实机、多显示器位置、长时间托盘常驻观察、真实大目录（≥1 万项）滚动体验。
- 若决定实施某几项，建议逐项单独提交并附上对应的验证方式（纯逻辑改动走 smoke harness；UI 相关改动走离屏探针 + 一次手工回归）。

## 9. 高优先级问题修复记录（2026-10-16）

改动涉及 6 个源文件（+314 / −79 行）+ 3 个文档，未新增项目或依赖。

### 9.1 H1 统一退出路径（`MainWindow.ShutdownApplication`）

新增 `MainWindow.ShutdownApplication(string reason)` 作为**唯一**退出入口，托盘菜单与设置面板都调用它：

1. 立即释放托盘图标（避免图标在关闭过程中残留）；
2. 以 500 ms 上限做一次预 flush（`SavePendingChanges`），使随后的 `FormClosing` flush 成为快速空操作；
3. 调用 `Application.Exit()`，走 WinForms 常规关闭流程：`FormClosing`（再次 flush；失败则弹提示并**取消关闭**）→ `FormClosed`（释放字体、托盘、仓储）→ 消息循环退出 → `Program.Main` 返回并记录 `Application shutdown completed.`；
4. 仅当上述路径抛异常时，才由 `ForceProcessExit()` 依次尝试 `Environment.Exit(0)`、`Process.Kill()`，每一步都写日志。

同时删除 `MainWindow.Settings.cs` 中 3 段重复的 `try { Kill(); } catch { try { Environment.Exit(0); } catch { } }` 与 `Application.DoEvents()`。用 `_shutdownInProgress` 只防**重入**（拥有窗体在关闭时会二次触发 `FormClosed`），方法结束即复位，因此"因写盘失败被取消的退出"之后仍可再次请求退出。

行为变化：设置面板中请求退出不再是"先写盘再硬杀进程"，而是关闭所有窗口的正常退出，因此字体、托盘图标与设置仓储都会正常释放；退出耗时实测 299–452 ms。

**探测更正（重要）**：审查初稿曾假设 `Application.Exit()` 不触发 `FormClosing/FormClosed`。实测（`%TEMP%\winDrawer.ExitProbe`）结论相反：

- `Application.Exit()` 会以 `CloseReason.ApplicationExitCall` 触发所有打开窗体的 `FormClosing` 与 `FormClosed`，且 `e.Cancel = true` **会被尊重**（应用继续运行）——所以托盘退出原本就会 flush，取消关闭也是有意义的保护；
- 拥有窗体在关闭过程中会**二次**触发 `FormClosed`（先自身 reason，再 `FormOwnerClosing`）；
- 在拥有窗体的 `FormClosed` 处理器里调用属主窗口的 `Close()` 会**栈溢出**（`SendMessage` 递归）——这正是最终实现选择"守卫 + `Application.Exit()`、绝不重入 `Close()`"的原因。

### 9.2 H2 全局异常对用户可见

- `Program.Main` 显式声明 `Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)` 并挂接 `Application.ThreadException`。
- 每次 UI 线程异常都写日志；**仅首次**额外弹出对话框（`Interlocked` 去重，避免反复失败时刷屏）。文案由 `Program.BuildUnexpectedErrorDialogText(Exception)` 生成，包含：发生了什么、可以继续使用、日志文件绝对路径、异常消息；对话框自身抛异常时只记录。
- `AppDomain.UnhandledException` 仍只记录（进程即将终止）；设置文件加载失败的启动提示保持不变。
- 注意：`SetUnhandledExceptionMode` 在该线程已创建任何控件后调用会抛 `InvalidOperationException`，因此它必须留在 `Main` 顶部、任何控件创建之前（当前顺序已由"真实入口点"探针验证通过）。

### 9.3 H3 设置仓储被正确释放

- `MainWindow` 的生命周期钩子从构造函数内 lambda 改为 `OnFormClosing`/`OnFormClosed` 重写（与 `DrawerWindow` 风格一致）；`OnFormClosed` 依次释放：品牌字体 → 托盘控制器 → **`SettingsRepository.Dispose()`**（内部完成最终 flush、取消后台 writer、释放 CTS 与信号量）→ `base.OnFormClosed(e)`。
- 抽屉设置窗体的 `FormClosed` 处理器新增一次性守卫：拥有窗体二次 `FormClosed` 时不再重复执行设置结算（探针确认结算恰好执行一次）。

### 9.4 H4 日志级别与轮转

- `DrawerLogger` 新增 `DrawerLogLevel` 过滤：默认 `Information`（挂接调试器时为 `Debug`），可用环境变量 `WINDRAWER_LOG_LEVEL`（`Debug`/`Information`/`Warning`/`Error`/`Off`，大小写不敏感，非法值回退默认）覆盖；被过滤的消息不产生任何字符串拼接或磁盘开销。
- 活动日志达到 `AppConstants.Logging.MaxLogFileBytes`（1 MB）时轮转为 `Drawer.log.1`（覆盖旧文件）；进程首次写入时按现有文件长度初始化体积预算（因此"上次退出时已接近 1 MB"的日志也能被轮转）；轮转失败不丢日志，只继续追加并重置预算，避免每次写入都重试 `File.Move`。
- 新增 `DrawerLogger.LogFilePath`（internal 只读属性）供 H2 的对话框文案与探针使用。

### 9.5 验证（本节所有结论均来自实际执行）

| 验证 | 方式 | 结果 |
|---|---|---|
| 构建 | `dotnet build src/winDrawer.csproj -c Debug` / `-c Release` | 0 警告 0 错误 |
| 单文件发布 | `dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o %TEMP%\wD-publish-check` | 成功（`winDrawer.exe` 4,777,946 字节） |
| 既有回归 harness | `%TEMP%\winDrawer.SmokeHarness` | `SMOKE OK`，exit 0（32 项：排序契约、枚举降级、缩略图回退与解锁、`always_on_top` 全链路、真实可见区间） |
| 既有 UI 探针 | `%TEMP%\winDrawer.AppProbe` | `PROBE OK`（设置窗体可构造、抽屉设置对话框布局与无障碍属性不变） |
| 日志级别 | 新探针 `%TEMP%\winDrawer.ShutdownProbe --logger`（默认、`=Debug`、`=bogus` 三次运行） | 14/14 PASS ×3：Debug 默认被过滤、`=Debug` 时写出、非法值回退 `Information` |
| 日志轮转 | 同上（反射把日志路径指向临时文件并预置 1 MB+ 内容） | PASS：生成 `<log>.1`、活动文件重新变小、触发轮转的条目仍保留 |
| H2 文案 | 同上（反射调用 `BuildUnexpectedErrorDialogText`） | PASS：含异常消息、日志绝对路径与应用名 |
| H1/H3 直接退出 | `--exit`（构造真实 `MainWindow`，反射调用 `ShutdownApplication`） | 6/6 PASS：请求被记录、`Application.Exit()` 完成、仓储已释放、托盘已释放、窗体已销毁、**未发生强制杀进程** |
| H1/H2/H3 设置面板场景 | `--exit-with-settings`（先打开设置面板再退出） | 8/8 PASS，含"设置结算只执行一次" |
| 真实入口点 + 托盘退出 | `--tray-exit`（反射调用 `Program.Main`，在 `Application.Idle` 中触发真实托盘退出动作） | 6/6 PASS：入口点建窗成功（证明 `SetUnhandledExceptionMode` 调用顺序安全）、托盘退出走到统一路径、仓储释放、`Application shutdown completed.`、未强制杀进程 |
| 退出语义实验 | `%TEMP%\winDrawer.ExitProbe`（独立 WinForms 小工程） | 四种场景实测，结论见 9.1 的"探测更正" |

探针一律把应用日志反射重定向到 `%TEMP%\winDrawer.ShutdownProbe\`，不触碰用户的日志与设置文件；`--exit*` 场景只读用户设置（无 pending 变更时不会写盘）。

### 9.6 仍未验证 / 后续

- H4 只验证了"按体积轮转"，**未做**长时运行（数周托盘常驻）观察；日志内容的可读性也未做人工审阅。
- 既有的未覆盖项（真实大目录滚动、非 96 DPI、多显示器、任务栏/托盘外观、设置面板 Save/Cancel/Default 实机回归）仍然待手工回归，见第 1 节限制。
- 中优先级 M1–M8 与低优先级 L1–L6 未改动。其中建议下一步先做 M5（死代码清理）与 M6（`DragLeave` 一行修复），再考虑 M2（抽取重复工具）与 M8（统一提示入口）。
- `docs/TODOs.md` 仍是空占位文件；是否恢复 7.1–7.5 的历史条目仍待你确认。

## 10. 中优先级问题修复记录（2026-10-16）

改动涉及 12 个源文件与 5 个新增文件（`Controls/UiButtonTheming.cs`、`Controls/UiDialogHelpers.cs`、`Controls/UiMessages.cs`、`Infrastructure/PathComparison.cs`、`Configuration/SettingsKeys.cs`），未新增项目或依赖。

### 10.1 M1 + M7 `SettingsButton_Click` 拆分与统一挂接/解绑

- 原 214 行方法拆为：`SettingsButton_Click`（28 行，只负责打开面板）+ `AttachSettingsHandlers`/`DetachSettingsHandlers` + 5 个具名事件处理器（`Settings_ThemePreviewChanged`、`Settings_WindowHeightPreviewChanged`、`Settings_WindowLockPreviewChanged`、`Settings_AlwaysOnTopPreviewChanged`、`Settings_StartupDefaultsResetRequested`）+ `Settings_FormClosed` + `ApplyAcceptedSettings`（保存分支）+ `RevertSettingsPreview`（取消分支）+ `ResetStartupToEmbeddedDefaults` + `RestoreStartupRegistration`（两处重复的注册表回滚逻辑合并）。
- 4 个本地函数改为私有方法后，**5 个事件全部可统一退订**（M7 中 `WindowLockPreviewChanged` 的遗漏随之消除）。需要面板引用的两个处理器改用 `sender is SettingsForm`（`SettingsForm` 触发这些事件时以 `this` 作 sender），因此不再需要闭包捕获。
- `settingsCloseHandled` 局部变量改为 `_settingsCloseHandled` 字段，语义不变（拥有窗体二次 `FormClosed` 时只结算一次）。
- 保存/取消分支的行为等价性由 10.6 的两个新探针模式验证。

### 10.2 M2 提取 4 组重复工具

| 组 | 新位置 | 调用点变化 |
|---|---|---|
| 路径比较 | `Infrastructure/PathComparison.cs`（`Normalize` / `AreSame`） | 删除 `SettingsRepository.PathsEqual` 与 `MainWindow.PathsEqual` 两份实现；`SettingsRepository.Drawers.cs`（6 处）、`MainWindow.DrawerList.cs`（3 处）、`MainWindow.Drawers.cs`（5 处）改用共享实现；5 处 `Environment.ExpandEnvironmentVariables(...).Trim()` 改用 `PathComparison.Normalize`。`UpdateSelectedDrawer` 里原先未展开环境变量的内联比较也统一为同一规则（与仓储的相等语义一致） |
| 颜色对话框预设色 | `Controls/UiDialogHelpers.BuildColorDialogCustomColors(primary, secondary?)` | 删除 `SettingsForm.GetThemeDialogCustomColors` 与 `DrawerSettingsDialog.GetDrawerDialogCustomColors`；两者生成的 16 项自定义色完全一致，"第二个色或默认主题色"的差异由参数表达 |
| 按钮三态配色 | `Controls/UiButtonTheming.ApplyAccentPalette` / `ApplyColorPalette` / `ApplyBorderPalette` | `MainWindow.ApplyTheme` 的 20 行内联改为 2 次 `ApplyAccentPalette`；`SettingsForm.ConfigureButton` 与 `DrawerSettingsDialog.ConfigureButtonStyle` 的 9 行赋值改为 `ApplyColorPalette`（两个对话框各自的 `Radius`/`Cursor` 保持原样）；`DrawerWindow.ApplyThemeToButton` 的 5 行边框赋值改为 `ApplyBorderPalette` |
| Enter/Space 激活 | `Controls/UiDialogHelpers.HandleActivationKey` | `SettingsForm.OnThemeColorPreviewKeyDown`、`DrawerSettingsDialog.PathDisplay_KeyDown`、`DrawerSettingsDialog.ColorPreview_KeyDown` 三处改为一行调用，`Handled`/`SuppressKeyPress` 语义集中定义 |

### 10.3 M3 位置参数改为命名参数

`SettingsRepository.GetEmbeddedStartupDefaults`、`MainWindow.ApplyAcceptedSettings`（M1 拆出的保存分支）、`SettingsSnapshot.Clone` 三处 `new SettingsSnapshot(...)` 全部改为命名参数；以后新增字段不再需要人工核对参数位置。

### 10.4 M4 TOML 键名集中到 `SettingsKeys`

新增 `Configuration/SettingsKeys.cs`（13 个常量），替换 `SettingsRepository.cs`、`SettingsRepository.Drawers.cs`、`SettingsRepository.Serialization.cs`、`SettingsSnapshot.cs` 中约 45 处字符串字面量。检索确认键名现在只出现在该文件与既有的私有常量 `ObsoleteIconSizeSettingName`（`icon_size`）中。`key is not SettingsKeys.Drawers and not SettingsKeys.Startup` 使用常量模式，拼写错误会在编译期报错。

### 10.5 M5 死代码 + M6 `DragLeave` + M8 提示收敛

- **M5 删除**：`SettingsForm.SelectedAccentColor`（含 3 处只写不读的赋值）、`SettingsForm.LaunchOnStartup`、`SettingsSnapshot.GetAccentColor()`（随前一项成为死代码）、`SettingsRepository.EnqueueSave()`、`SettingsRepository.Save()`、`AppConstants.Directories.Fonts` 与整个 `AppConstants.DrawerUi` 类（5 个自绘卡片时代的常量）。
- **M6**：`MainWindow` 构造函数补上 `drawerListBox.DragLeave += DrawerListBox_DragLeave;`，把抽屉拖出列表后插入提示条会立即清除（原先只在 `DragDrop` 的 `finally` 与重新填充列表时清除）。
- **M8**：新增 `Controls/UiMessages.cs`（`ShowError`/`ShowWarning`/`ShowInformation`/`Confirm`），共替换 29 处调用：`MainWindow.*` 19 处、`DrawerWindow.*` 3 处、`DrawerSettingsDialog` 2 处、`SettingsForm` 3 处、`Program` 2 处。现在 `MessageBox.Show` 只出现在 `UiMessages.cs` 内（可用一次检索强制）。两处原先无属主的提示（`Drawer Missing`、`Drawer Error`）改为以主窗口为属主，与其他提示的归属/z 序一致。

### 10.6 验证（本节所有结论均来自实际执行）

| 验证 | 方式 | 结果 |
|---|---|---|
| 构建 | `dotnet build -c Debug` / `-c Release` | 0 警告 0 错误 |
| 单文件发布 | `dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o %TEMP%\wD-publish-check` | 成功（`winDrawer.exe` 4,773,850 字节，比改动前少 4,096 字节，与删除的死代码一致） |
| 配置层回归 | `%TEMP%\winDrawer.SmokeHarness`（已把 `SettingsKeys.cs`、`PathComparison.cs` 加入其编译项） | `SMOKE OK`，exit 0（32 项：排序契约、枚举降级、缩略图回退与解锁、`always_on_top` 缺失/真假/类型错误解析与写盘往返与 Default 重置、真实可见区间）——正好覆盖 M3/M4 改动的读写路径 |
| 退出与设置关闭回归 | `%TEMP%\winDrawer.ShutdownProbe` 六种模式 | 全部 PASS（第 9.5 节表格 + 下面两行） |
| **保存语义（M1 重点）** | 新探针 `--settings-save`：构造真实 `MainWindow`，反射把 `_settingsRepository` 指向 `%TEMP%` 下的一次性设置文件，打开面板、用真实高度滑块触发预览、点击 Save | 8/8 PASS：预览生效；写入"Applying settings changes from dialog."；新的 `window_height` 确实进入设置文件；设置结算只执行一次；面板已释放；主窗口随后正常关闭 |
| **取消语义（M1 重点）** | 新探针 `--settings-cancel`：同样隔离仓储，改高度后点击 Cancel | 8/8 PASS：写入"Settings dialog cancelled; reverting preview."；设置文件**未**被写入；**主窗口高度回到保存值**（断言实际 `ClientSize.Height` 与 DPI 缩放后的期望值一致） |
| UI 外观回归 | `%TEMP%\winDrawer.AppProbe` | `PROBE OK`（设置窗体可构造；抽屉设置对话框客户区 600×500、按钮尺寸/无障碍属性/控件类型不变） |
| 检索不变量 | `MessageBox.Show`、TOML 键字面量 | `MessageBox.Show` 仅存在于 `UiMessages.cs`；键名仅存在于 `SettingsKeys.cs`（加既有的 `ObsoleteIconSizeSettingName`） |

两个设置探针在保存前会把登录启动选项设为注册表当前状态，因此不会改动用户的启动项；日志与设置写入都落在 `%TEMP%\winDrawer.ShutdownProbe\` 下，不触碰用户文件。

### 10.7 仍未验证 / 后续

- **未做实机手工回归**：颜色对话框预设色、按钮 hover/pressed 观感、`DragLeave` 的真实拖拽体验、非 96 DPI 与多显示器行为（与第 9.6 节同一批待办）。
- M2 的按钮配色提取是"逐属性等价"的机械搬移，但**观感**仍建议人工确认一次（尤其抽屉窗口关闭按钮的红色与设置对话框的语义色）。
- 低优先级 L1–L6 未改动。其中 L4（无障碍覆盖）与 L1（`MainWindow` 生命周期风格）现在更容易实施，因为相关代码已经拆分（`OnFormClosed` 已在 H3 中改为 override，L1 实际已顺带完成，可在下一轮核对后关闭该条）。
