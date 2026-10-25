# 阶段九建议：抽屉文件拖入/拖出与底栏文件夹入口收整

> **后续状态：** 本文是 SunnyUI 迁移前的实施记录；文中 `Sunny.UI.UILabel` 已由原生 `Label`/`EllipsisLabel` 取代，SunnyUI 依赖已完全移除。当前 UI 架构见 [`WHITEPAPER.md`](WHITEPAPER.md)。

我检查了当前实现、工作树和 `docs/TODOs.md`。`git status --short` 只有 `M docs/TODOs.md`（未提交的用户改动，内容正是剩余两条待办）；本文件只新增计划，没有改动源码、`TODOs.md` 或其它文档。

本阶段范围严格收敛为 `TODOs.md` 剩下的两条：

1. **打开抽屉后可拖拽文件出入**，操作语义与系统默认一致：同盘移动、异盘拷贝。
2. **底栏文件夹图标 + 路径合并为一个可点击区域**：对齐、单行显示并按可用宽度截断、左侧留出 padding。

下文用 D1–D9 记录建议决策，其中 **D6（是否开启多选）需要你确认**；其余按“默认方案”执行，若实施中发现与代码现状冲突，先把实测结论回写本文件再处理，不静默偏离。

## 已核实的现状

### 1. 抽屉内容列表与拖放能力

- 列表是 `ScrollableListView`（`src/Controls/ScrollableListView.cs`），Designer 中 `View = View.LargeIcon`、`HideSelection = false`、`ShowItemToolTips = true`、**`MultiSelect = false`**（`src/Forms/DrawerWindow.Designer.cs:87-100`）。它只额外提供 `ViewportChanged` 和隐藏横向滚动条，没有重写任何 OLE 拖放消息，也没有拖放相关代码。
- 全仓库检索 `AllowDrop` / `DragEnter` / `DragOver` / `DragDrop` / `DoDragDrop` / `ItemDrag` / `DataFormats`，命中**只有**主窗口抽屉列表的重排：`src/Forms/MainWindow.cs:57-61`、`src/Forms/MainWindow.DrawerList.cs:195-235`、`:293+`。它用 `DoDragDrop(path, DragDropEffects.Move)` 传一个 `string`，`DragEnter/DragOver` 只接受 `GetDataPresent(typeof(string))`，并绘制插入位置标记。抽屉窗口**完全没有**拖放实现。
- 列表项元数据是 `DrawerItemInfo(Path, Name, IsDirectory, LastWriteTimeUtc)`（`src/Drawers/DrawerItemInfo.cs`），`ListViewItem.Tag` 保存它；双击时 `HitTest(e.Location).Item.Tag` 取出实体再交给 `Process.Start(UseShellExecute = true)`（`src/Forms/DrawerWindow.cs:290-319`）。刷新入口是 `RefreshDrawerAsync()`（`src/Forms/DrawerWindow.FileList.cs:49-53`，F5 与右键菜单共用），刷新会走 `PopulateFileListAsync()` 的取消令牌 + generation 机制并释放旧图像。
- 因此拖放是**全新功能面**：没有既有行为需要兼容；唯一需要回归的是主窗口抽屉列表的重排不能被外部文件拖放破坏。

### 2. 底栏文件夹入口

- 结构为 `drawerHeaderTableLayoutPanel`（65% / 35% 两列）→ 左列 `drawerPathTableLayoutPanel`（2 列：**`Absolute 42F` 图标列 + `Percent 100` 文本列**）→ `openFolderIconPictureBox` + `fullPathLabel`；右列 `drawerButtonTableLayoutPanel` 只放 `closeDrawerButton`（`src/Forms/DrawerWindow.Designer.cs:108-198`）。
- 现在的“可点击区域”是靠三处重复绑定拼出来的：`drawerPathTableLayoutPanel.Click`、`openFolderIconPictureBox.Click`、`fullPathLabel.Click` 全部指向 `OpenFolderButton_Click`（`src/Forms/DrawerWindow.cs:43-45`），三者的 `Cursor = Hand`、`ToolTip`、`AccessibleName` 也各写了一份。
- 具体缺陷：
  - **左侧没有 padding**：`drawerPathTableLayoutPanel` 的 `Padding` 未设置（默认 0），图标列从 x=0 开始；文本列从 x=42 起、`fullPathLabel.Padding = (6,0,6,0)`，文字实际从 x≈48 开始，图标在 42px 列里 `CenterImage` 居中（24px 图标两侧各 9px 空白），视觉上图标与文字之间的空白大于左侧。
  - **DPI 不自洽**：`AutoScaleMode = None`，图标列是固定 `42F` 物理像素，但文件夹图标是 `GetFolderIconSize(dpi)` = 24 逻辑像素按 DPI 缩放（`src/Forms/DrawerWindow.cs:206-211`）——**在 200% DPI 下图标是 48px，却被塞进 42px 宽的 PictureBox**，会被裁切/压缩。底栏行高也是固定 `RowStyle(Absolute, 50F)`，同样不随 DPI 变化。
  - **截断能力未经验证**：`fullPathLabel` 是 `Sunny.UI.UILabel`（基类是 `System.Windows.Forms.Label`），Designer 已设 `AutoEllipsis = true`（`:165`）且 `Dock = Fill`，但在现有单元格宽高与 padding 组合下，是否真的“单行 + 尾部省略号”没有实测结论；`UILabel` 的自绘路径与原生 `Label` 的 `AutoEllipsis` 行为也不保证一致。
  - 三个控件的 `Location/Size` 是 Designer 里写死的物理值（`fullPathLabel.Location = (45,0)` 等），与实际布局无关，容易误导后续维护。
- 主题只设置了 `fullPathLabel.ForeColor` / `BackColor = Transparent`（`src/Forms/DrawerWindow.cs:92-93`），没有 hover 反馈；`UpdateStandardFolderIcon` 在构造与 `DpiChanged` 时被调用（`src/Forms/DrawerWindow.cs:41`、`src/Forms/DrawerWindow.FileList.cs:55-60`），可作为新增底栏布局刷新的挂载点。

### 3. 文件操作与安全边界

- 目前应用对用户文件的唯一动作是“请求 Shell 打开”：`ProcessStartInfo { FileName = path, UseShellExecute = true }`（`src/Forms/DrawerWindow.cs:301-319`、`OpenFolderButton_Click` `:163-183`）。`AGENTS.md` 明确规定“不删除、移动、重命名或修改用户文件”；本阶段要**有控制地**放开这一条，见 D1 与文档更新要求。
- 仓库里没有现成的文件操作封装、没有 `IFileOperation`/`SHFileOperation` 互操作、也没有 `Interop` 目录。现有 P/Invoke 内联在 `src/Forms/DrawerWindow.Chrome.cs`（窗口消息）与 `src/Controls/ScrollableListView.cs`（user32 滚动），`src/Infrastructure/` 目前放 `DrawerLogger`、`GlobalHotKey`、`PathComparison`、`StartupRegistration`、`TrayController`。
- `PathComparison` 只有 `Normalize` / `AreSame`（`src/Infrastructure/PathComparison.cs`），没有“同卷”或“父路径包含”判断，需要补齐而不是在窗体里就地写字符串比较。
- 纯逻辑 smoke harness 的既有形态是 `%TEMP%` 下的临时项目、直接链接真实源文件（见 `DP_STAGE_7.md` / `DP_STAGE_8.md` 的实现记录），没有测试工程；本阶段沿用。
- **不需要任何配置字段**：拖放语义是系统默认（同盘移动/异盘拷贝），底栏是纯布局，两者都不写入 TOML，也不改动 `drawers[]` 任何键。

## 本阶段建议决策

| 编号 | 决策 | 默认方案 |
| --- | --- | --- |
| D1 | 拖入的落地方式 | 通过 **`IFileOperation`（Windows Shell）** 执行 Move/Copy，由 Shell 负责进度、冲突确认、错误提示与撤销记录；**禁止**用 `File.Move`/`File.Copy` 自行搬运，也**禁止**拼接命令行或 `Process.Start` |
| D2 | 同盘/异盘语义 | 逐条按“源所在卷 vs 抽屉目录所在卷”判定：同卷 Move、异卷 Copy；在同一次 `IFileOperation` 里分批入队（`MoveItems` 一组、`CopyItems` 一组，最后一次 `PerformOperations`），只弹一个进度窗口；`Ctrl` 强制 Copy、`Shift` 强制 Move（与资源管理器一致）；不提供 Link |
| D3 | 接受拖入的区域 | 只有 `drawerContentsListView`（含列表空白处）。不把整个窗体设为拖放目标，避免误投到路径入口或关闭按钮 |
| D4 | 支持的数据格式 | 只接受 `DataFormats.FileDrop`（真实文件系统路径）。压缩包内部条目、Outlook 附件等虚拟文件、纯文本一律 `None`，不做转换 |
| D5 | 拖出 | `ItemDrag` + `DoDragDrop(FileDrop, Copy \| Move)`，实际 Copy/Move 由 Explorer 决定；拖出结束后**仅当效果包含 Move** 才刷新抽屉（文件可能已离开目录），Copy 不刷新 |
| D6 | 是否开启多选 | **建议 `MultiSelect = true`**，一次拖出多个选中项。当前为 `false`，改成 `true` 会引入 Ctrl+单击、框选等资源管理器式选择行为——**这一项需要你确认**；若保持单选，拖出一次只处理被拖的那一项，拖入仍支持多文件 |
| D7 | 拖放反馈 | 只用系统光标效果（复制/移动/禁止），不自绘拖放提示层、不做自定义拖影；拖入成功后静默刷新列表，不弹“成功”提示；被拒绝或失败走 `UiMessages` |
| D8 | 底栏入口 | 保持“图标 + 文本”两个子控件：`Sunny.UI.UILabel` 虽继承自 `Label` 并提供 `Image`/`ImageAlign`，但没有 `TextImageRelation`，图文只能各自定位、容易重叠，单控件方案不可靠。改为把它们放进**同一个 hit 区域**：容器统一 padding、图标列按 DPI 计算、文本控件改原生 `Label` 并单行尾部省略、三个控件的点击/光标/提示由一处绑定 |
| D9 | 配置与数据 | 不新增 TOML 字段，拖放与底栏都不改动任何设置；Default 按钮、`sort_mode`、抽屉颜色等既有语义完全不动 |

## 阶段划分

### 阶段 9.1：底栏文件夹入口收整（布局 + 单行截断）

1. **改为可计算的布局**：在 `AppConstants.Ui` 增加底栏布局常量（逻辑像素）：左侧 padding `12`、图标与文本间距 `8`、右侧 padding `8`、底栏行高 `50`、文件夹图标 `24`（与 `GetFolderIconSize` 的 `logicalSize` 合并为一个来源，避免两处各写一份）。
2. **新增 `DrawerWindow.UpdateFooterLayout(int dpi)`**，在构造（`InitializeWindow`，紧跟 `UpdateStandardFolderIcon(DeviceDpi)`）与 `DpiChanged`（`DrawerWindow_DpiChanged`，与 `UpdateStandardFolderIcon` 并列）中调用：
   - `drawerPathTableLayoutPanel.Padding = (left, 0, right, 0)`，按 DPI 缩放；
   - `drawerPathTableLayoutPanel.ColumnStyles[0].Width = 图标尺寸 + 间距`（按 DPI 缩放），使图标列宽度恰好容纳图标，消除 42px 死区；
   - `mainTableLayoutPanel.RowStyles[1].Height = 行高`（按 DPI 缩放），使 200% DPI 下不再挤压图标与关闭按钮；
   - `fullPathLabel.Padding = 0`、`Margin = 0`、`AutoSize = false`、`TextAlign = MiddleLeft`、`Dock = Fill`。
3. **图标与文本对齐**：`openFolderIconPictureBox` 保持 `Dock = Fill` + `SizeMode = CenterImage`（在自身列内垂直、水平居中）；文本控件 `Dock = Fill` + `TextAlign = MiddleLeft`，从而图标中心与文字中线在同一水平线，且文本左边界固定为“左侧 padding + 图标列 + 间距”。
4. **单行截断**：把 `fullPathLabel` 从 `Sunny.UI.UILabel` 换成原生 `System.Windows.Forms.Label`，保留 `AutoEllipsis = true`、`UseMnemonic = false`、`AutoSize = false`（原生 `Label` 在 `AutoEllipsis = true` 时强制单行并以 `…` 截断）。若实测仍有换行/裁切，再退到 `TextRenderer` 显式截断的小函数（见 9.4 的可测项），不在 Designer 里堆参数。
5. **合并 hit 区域**：把三处 `Click += OpenFolderButton_Click` 收敛到一个 `InitializeFolderLink()`，统一为容器 + 图标 + 文本三者绑定同一处理器，并统一 `Cursor = Cursors.Hand`、`AccessibleName`、`AccessibleDescription` 与 ToolTip（ToolTip 始终显示**完整路径**，不随截断变化）。可选（需确认）：鼠标进入整块区域时给容器一个轻微主题色高亮——子控件会各发 `MouseEnter/Leave`，必须用计数器或 `ClientRectangle` 命中判断避免闪烁；若不做，明确记录“仅光标反馈”。
6. **清理 Designer 误导值**：删除 `fullPathLabel.Location/Size`、`openFolderIconPictureBox.Size` 等与 Dock 冲突的硬编码，避免后续误读；`AccessibleName` 只保留一份语义（以容器为准）。

**验收：** 100% / 150% / 200% DPI 下图标完整不被裁切、垂直居中且与文字对齐；最左侧有稳定的 12 DIP 视觉留白；窗口缩到 `MinimumSize (530, 320)` 时路径保持单行并以 `…` 截断，不换行、不溢出到关闭按钮；点击图标、文字或二者之间空白都打开同一目录且只触发一次；ToolTip 显示完整路径；主题切换后文字颜色仍正确；`DpiChanged` 后布局立即重算且不重复加载列表。

### 阶段 9.2：拖出（抽屉 → 资源管理器/桌面）

1. 新增 `src/Forms/DrawerWindow.DragDrop.cs`（partial，与 `.Chrome.cs` / `.ContextMenu.cs` 风格一致），由 `InitializeWindow()` 调用一次 `InitializeDragDrop()` 完成全部绑定；样式/主题相关既有调用点不动。
2. `drawerContentsListView.ItemDrag`：从 `Tag` 取 `DrawerItemInfo`；按 D6 决定取“被拖动项”还是“选中项集合”，生成去重后的路径列表；用 `DataObject.SetFileDropList(StringCollection)` 写入 `DataFormats.FileDrop`，**不要**用 `DataObject(typeof(string[]))` 之类猜测格式。
3. `DoDragDrop(dataObject, DragDropEffects.Copy | DragDropEffects.Move)`：不包含 `Link`，因此不会在目标处生成快捷方式；实际移动/拷贝、进度窗口、冲突处理全部由 Explorer 完成（符合“委托 Windows Shell”的既有约定）。
4. 返回后处理：
   - `effect` 含 `Move` → `_ = RefreshDrawerAsync()`（文件可能已离开抽屉目录）；
   - `effect` 为 `Copy` 或 `None`（用户取消）→ **不刷新**，保持列表、滚动位置、选中项与已加载缩略图不变；
   - 刷新前检查 `IsDisposed` / `Disposing`，避免拖放期间窗口被关闭时抛异常。
5. 日志：一次拖出只写一条 `LogInformation`（项数 + 目录 + 效果），不逐项写日志；`DoDragDrop` 抛出的异常记录 `LogError` 并提示，不让窗体退出。
6. 若 D6 维持 `MultiSelect = false`：确认 `ItemDrag` 仍正常触发（拖动手势与选择无关），只是拖的永远是当前项。

**验收：** 把抽屉里的文件/文件夹拖到资源管理器同一磁盘目录 → 文件被移动且抽屉刷新；拖到其它磁盘目录 → 被拷贝且抽屉不刷新、缩略图不重载；拖到桌面/回收站等 Shell 目标行为与资源管理器内拖放一致；拖出过程中按 Esc 取消 → 列表完全不变；`Ctrl`/`Shift` 修饰键行为与资源管理器一致；拖出多个（若 D6 开启多选）文件都能工作；拖出后双击打开、右键菜单、F5 均不回归。

### 阶段 9.3：拖入（资源管理器 → 抽屉）

1. **纯逻辑判定**：新增 `src/Drawers/DrawerDropPlanner.cs`（不接触文件系统），提供：
   - `GetVolumeRoot(string path)`：`Path.GetFullPath` + `Path.GetPathRoot`，`OrdinalIgnoreCase` 比较；UNC 按 `\\server\share` 视为同一卷；无法取得根时按“异卷”处理（保守选择 Copy）；
   - `IsSameOrChildPath(string candidate, string parent)`：判断目标是否等于或位于某个被拖动目录内部（拒绝“把文件夹拖进它自己的子目录”）；
   - `Plan(sourcePaths, destinationFolder, allowedEffect, modifierKeys)`：返回 `Action`（None / Copy / Move / 拒绝原因）与分组后的 `MovePaths` / `CopyPaths`。规则：`Ctrl` → Copy、`Shift` → Move；否则逐条按同卷 Move / 异卷 Copy；若期望效果不在 `allowedEffect` 内则回退到另一种，两者都不允许 → None；
   - `IsSameParentAs(string sourcePath, string destinationFolder)`：源已在抽屉目录内则跳过（同目录内拖放是 no-op，不能变成自我覆盖）。
2. **UI 绑定**：`drawerContentsListView.AllowDrop = true`；`DragEnter` / `DragOver` 计算计划并设置 `e.Effect`（`DragOver` 每次重算，以反映拖动过程中按下的 Ctrl/Shift）；`DragLeave` 只清理内部状态；`DragDrop` 重新计算（不信任 `DragEnter` 的缓存）后执行。
3. **过滤与提示**：仅接受 `DataFormats.FileDrop`；用 `File.Exists`/`Directory.Exists` 过滤失效路径，全部无效 → `None` 且不弹窗；目标目录不存在 → `None` + `UiMessages.ShowError`（文件夹不可用）；目标位于被拖动目录内部 → `None` + `UiMessages.ShowWarning`（说明不能把文件夹移动进自身子目录）；其余失败交给 Shell 的原生错误 UI；当前拖动内容不支持（虚拟文件/文本）→ 光标为禁止，不提示。
4. **Shell 执行**：新增 `src/Infrastructure/ShellFileOperation.cs`（`internal static`），签名大致为
   `ShellOperationResult Perform(string destinationFolder, IReadOnlyCollection<string> movePaths, IReadOnlyCollection<string> copyPaths, IntPtr ownerWindow)`：
   - `CoCreateInstance(CLSID_FileOperation = 3ad05575-8857-4850-9277-11b85bdb8e09)` → `IFileOperation`；
   - `SetOwnerWindow(ownerWindow)`（让进度/冲突对话框归属抽屉窗口）+ `SetOperationFlags(FOF_ALLOWUNDO)`；
   - `SHCreateItemFromParsingName`（`IShellItem` GUID `43826d1e-e718-42ee-bc55-a1e261c37bfe`）创建目标目录与每个源的 `IShellItem`；
   - 先对 `movePaths` 依次 `MoveItems(items, destination, null)`，再对 `copyPaths` 依次 `CopyItems(...)`，最后**一次** `PerformOperations()`，随后 `GetAnyOperationsAborted()`；
   - `IFileOperation` 的方法必须按 `shobjidl_core.h` 的**原始顺序**声明到 `PerformOperations` / `GetAnyOperationsAborted` 为止（包括 `Advise`/`Unadvise`/`SetProgressMessage` 等未使用方法的占位），否则 vtable 偏移错误；用 `[PreserveSig]` 直取 `HRESULT`；
   - **必须在 STA 线程调用**：WinForms UI 线程即 STA，直接同步调用，不要 `Task.Run`；Shell 显示自己的进度与冲突对话框，调用期间界面不可交互（与资源管理器一致，本阶段接受）；
   - `finally` 中释放全部 `IShellItem` 与 `IFileOperation`（`Marshal.FinalReleaseComObject`，包一层容错 helper）；
   - 结果：`HResult == 0` 且未 abort → 成功；`HResult == 0x800704C7`（`ERROR_CANCELLED`）→ 用户取消（**不弹错误**）；其它 `HRESULT` → `LogError` + `UiMessages.ShowError`（含 `Marshal.GetExceptionForHR(hr)?.Message`）。
5. **执行后**：只要 `PerformOperations` 返回（成功、部分完成或取消后可能已有部分改动），都调用一次 `RefreshDrawerAsync()`；新增条目按当前 `sort_mode` 出现在正确位置。
6. **日志**：一条 `LogInformation` 记录目标目录、Move/Copy 各自数量、修饰键与结果；`LogError` 记录 HRESULT 与异常；不逐项写日志。
7. **主窗口回归**：确认把外部文件拖到主窗口抽屉列表时行为与改动前一致（不新增抽屉、不误触发重排）；若实测 `GetDataPresent(typeof(string))` 会因 OLE 格式转换而误判，则把主窗口拖放收紧为“只认内部重排标记格式”，并在本文件记录。

**验收：** 从资源管理器把文件/文件夹拖进抽屉：同盘为移动、异盘为拷贝，进度与冲突对话框来自 Windows；`Ctrl` 强制拷贝、`Shift` 强制移动；在到达前取消 → 无任何改动；被拖入文件夹自身子目录 → 明确拒绝且不改动文件；目标目录被删除 → 可理解提示且不崩溃；压缩包内条目/Outlook 附件/纯文本拖入 → 光标为禁止且无操作；拖入后列表按当前排序刷新、缩略图与滚动状态正常、无图像/COM 泄漏；拖放只作用于抽屉列表，路径入口与关闭按钮区域不受影响；主窗口抽屉列表重排无回归。

### 阶段 9.4：文档与回归

1. **`AGENTS.md`（产品行为约定）**：把“不删除、移动、重命名或修改用户文件”改写为“本工具自身不主动改动用户文件；唯一例外是用户在抽屉列表上**显式拖放**触发、且完全由 Windows Shell 执行的移动/拷贝”，并写明允许范围（只 Move/Copy、只经 `IFileOperation`、不做删除/重命名/新建文件夹/粘贴）。
2. **`README.md`**：新增拖入/拖出说明（同盘移动、异盘拷贝、Ctrl/Shift、取消、冲突由 Windows 处理、`FOF_ALLOWUNDO` 尽量保留撤销信息、仅支持真实文件系统条目）与底栏入口的新外观（单行省略、整块可点、ToolTip 显示完整路径），并更新 `src/` 文件清单（`Drawers/DrawerDropPlanner.cs`、`Infrastructure/ShellFileOperation.cs`、`Forms/DrawerWindow.DragDrop.cs`）。
3. **`DP_STAGE_9.md` 实施记录**：按阶段 7/8 的格式追加每项的实际实现与“已验证/未验证”清单；**不得**把计划、纯逻辑 harness 或未执行的 UI 回归写成已验收。
4. **纯逻辑 smoke checks**（`%TEMP%` 临时 harness、链接真实源文件、不新增测试工程）：
   - `GetVolumeRoot`：盘符大小写、带/不带尾随分隔符、UNC 同共享/不同共享、相对路径回退；
   - `IsSameOrChildPath`：自身、子目录、`..` 形式、大小写差异、前缀相似的假兄弟目录（`C:\Foo` vs `C:\Foobar`）；
   - `Plan`：同卷 Move、异卷 Copy、混合多源分组、Ctrl/Shift 覆盖、`allowedEffect` 只允许 Copy 时的回退、只允许 Move 时的回退、两者都不允许 → None、源已在目标目录 → 跳过、目标位于源内部 → 拒绝；
   - 若 9.1 落地了 `TextRenderer` 截断函数，补 ellipsis 边界用例（刚好容纳、少 1px、空串、超长）。
5. **构建验证**：Debug/Release 走完整 `dotnet build`（**不要**用 `-t:Compile`）；`bin\<配置>` 被运行实例锁住时改用 `-o <临时目录>`；框架依赖单文件发布检查嵌入资源仍完整。
6. **手工回归（必须实际执行并如实记录）**：
   - 拖放：单/多文件拖出到同盘、异盘、桌面；拖出取消；拖入同盘移动、异盘拷贝、Ctrl/Shift；重名冲突（替换/跳过/取消）；只读/被占用文件；长路径；移动后源 Explorer 与抽屉的刷新；拖入后排序、缩略图、滚动位置；
   - 底栏：最小宽度 `530`、超长路径、100%/150%/200% DPI、`MinimumSize` 与窗口尺寸持久化（`drawers[].size` 往返）、主题与抽屉专属蒙版下的文字对比度、ToolTip；
   - 既有项：双击打开、F5/右键菜单与三种排序、托盘与热键显隐、`DrawerWindow` 顶部缩放条、`AlwaysOnTop`、设置保存/取消/Default。
7. **工作保护**：每个子阶段开始前检查 `git status --short` 与目标文件 diff；测试只用 `%TEMP%` 临时目录与临时文件，不碰真实用户文件与配置；不清理 `bin/`、`obj/`、发布产物、日志与 `.vs/`。
8. **TODO 收尾**：两条待办全部按验收完成后，再从 `docs/TODOs.md` 移除对应条目——需要你确认，且不得覆盖该文件当前的未提交改动。

## 建议顺序

先做 **9.1 底栏收整**：纯 UI、无数据风险，能先把 DPI 下图标被裁切的既存缺陷一并修掉，并立即用肉眼验收。随后做 **9.2 拖出**：只依赖 `DoDragDrop`，是验证 `FileDrop` 格式与刷新时机的最小闭环。再做 **9.3 拖入**：引入 `IFileOperation` 与“同盘/异盘”判定，改动面与回归面最大，放在前两项稳定之后。最后以 **9.4 文档与回归** 收尾。9.2 与 9.3 共用 `DrawerWindow.DragDrop.cs`，可以合并提交，但必须分开验证（拖出不会因拖入的 Shell 执行逻辑回归）。

## 风险与已知取舍

- **`IFileOperation` 互操作**是唯一技术风险点：接口 vtable 顺序、COM 释放、STA 线程、`HRESULT` 退出码都容易出错。实施时先写一个只做“单文件同盘移动”的最小闭环并用 `%TEMP%` 目录验证，再扩展到 Copy 与多源分组。若实测出现无法解释的稳定性问题，回退方案是 `SHFileOperationW` + `FOF_ALLOWUNDO`（同样由 Shell 执行），但必须在本文件记录取舍，不得两套并行。
- **操作期间界面不可交互**：`PerformOperations()` 是同步调用，Shell 进度/冲突对话框是模态的。这与资源管理器一致，也比“自建后台线程 + 自建进度 UI”更符合“委托 Windows”的约定；若你要求长拷贝期间抽屉仍可滚动，需要另做异步化设计，不在本阶段。
- **`MultiSelect` 变更**（D6）会改变现有选择交互，是本阶段唯一影响日常操作手感的改动，因此单独列为待确认项。
- **不做自定义拖放视觉**：没有拖放高亮/拖影，只有系统光标效果。若你希望“拖到列表上时整块高亮”，需作为 9.3 的附加项（要处理子控件 `MouseEnter/Leave` 抖动）。
- **`FOF_ALLOWUNDO` 只是“尽可能保留撤销信息”**：移动/拷贝是否出现在资源管理器的撤销栈由 Shell 决定，本阶段不自建撤销 UI，也不承诺一定能 Ctrl+Z 撤销。
- **200% DPI 的图标裁切是既存缺陷**（42px 列 vs 48px 图标），9.1 一并修复；修复后底栏行高变化会轻微改变列表可用高度，需验证 `drawers[].size` 往返不漂移。

## 本阶段不做

- 不做删除、重命名、新建文件夹、复制/剪切/粘贴（Ctrl+C/V）、Shell 右键菜单或“在资源管理器中显示”。
- 不做抽屉内拖放排序、跨抽屉拖拽（当前模型同时只打开一个抽屉，主窗口列表不接受文件投放）、把文件拖到主窗口来添加/创建抽屉。
- 不支持虚拟文件（压缩包内条目、Outlook 附件）与网络流，不做格式转换或临时落地文件。
- 不做自定义拖影、拖放高亮层、自绘进度条，不自建撤销/重做。
- 不新增任何 TOML 字段，不改 `sort_mode`、`overlay_color`、`size`、`always_on_top` 等既有语义；不改 `docs/TODOs.md` 的当前未提交内容（收尾移除条目需你确认）。
- 不引入新的 UI 框架、拖放库（如第三方 WinForms 拖放组件）、UI 自动化测试框架或测试工程。

## 待你确认

1. **D6 多选**：是否同意把抽屉列表改为 `MultiSelect = true`，以支持一次拖出多个选中项？（建议同意；这会让 Ctrl+单击与框选行为更接近资源管理器。）
2. **底栏 hover 反馈**（9.1 第 5 步的可选部分）：是否需要在鼠标进入整块路径区域时加轻微主题高亮？默认只保留手型光标与 ToolTip。
3. **拖入视觉提示**（9.3）：是否需要在拖动经过列表时显示整块高亮？默认只用系统光标效果。

**当前状态：** 9.1–9.4 已按“默认方案”实施——第 1 项（D6）已开启 `MultiSelect = true`（要回到单选，只需把 `DrawerWindow.Designer.cs` 中该属性改回 `false`）；第 2、3 项（hover 高亮/拖入高亮）仍未实现，保持手型光标、ToolTip 与系统拖放光标。

## 实施状态

### 9.1 已落地（底栏文件夹入口）

- **新增 `AppConstants.Ui.DrawerFooter`**：`PaddingLeft = 12`、`PaddingRight = 8`、`IconTextGap = 8`、`FolderIconSize = 24`（均为 96 DPI 逻辑像素）；`DrawerWindow.GetFolderIconSize` 改为复用 `FolderIconSize`，不再保留第二份 `24` 字面量。
- **新增 `DrawerWindow.UpdateFooterLayout(int dpi)`**：在 `InitializeWindow`（`UpdateStandardFolderIcon` 之后）与 `DrawerWindow_DpiChanged` 中调用，按当前 DPI 重算
  - `drawerPathTableLayoutPanel.Padding`（左 12 / 右 8 DIP），
  - 图标列宽 = 当前图标实际像素尺寸（原来固定 `42F`，200% DPI 下 48px 图标会被裁切），
  - `fullPathLabel.Padding.Left` = 8 DIP，作为图标与文本的间距（仍在可点击区域内）。
- **文本控件改为原生 `System.Windows.Forms.Label`**：`AutoSize = false`、`AutoEllipsis = true`、`UseMnemonic = false`、`Dock = Fill`、`TextAlign = MiddleLeft`、`Padding = (8,0,0,0)`，实现单行 + 尾部 `…` 截断；`Sunny.UI.UILabel` 不再用于路径文本。`AutoScaleMode.None` 下的 Designer 硬编码 `Location/Size` 已删除。
- **合并 hit 区域**：`InitializeFolderLink()` 统一绑定容器、图标、文本的 `Click` → `OpenFolderButton_Click`，统一 `Cursor.Hand` 与提示；路径文本的 ToolTip 继续显示完整路径（`InitializeWindow` / `ApplyDrawerDefinition` 维护），截断时仍可查看全路径。
- **无障碍**：容器保留 `AccessibleName`/`AccessibleDescription`；图标设为装饰性（`AccessibleRole.None`）并移除重复名称；路径文本保留自身名称与描述。
- **偏离计划（已记录）**：**未按 DPI 缩放底栏行高**。关闭按钮的尺寸与字体仍是固定物理像素，若把行高从 50 放大到 100（200% DPI），按钮会贴顶留出大片空白；保持 50 物理像素时 48px 图标仍可完整容纳，因此移除了 `RowHeight` 常量与行高缩放。若后续要整体按 DPI 缩放底栏，应同时缩放关闭按钮尺寸/字体，再单独验收。
- **已验证（构建）**：Debug 与 Release 完整 `dotnet build` 均 **0 警告 0 错误**（未使用 `-t:Compile`，`bin\Debug`/`bin\Release` 未被运行实例锁定）。
- **已验证（临时探针，`%TEMP%` 下反射实例化真实 `DrawerWindow`）**：探针用临时 `Drawer-settings.toml` 与临时长路径文件夹创建 `SettingsRepository` / `DrawerDefinition` / `ThemePalette`，反射构造真实抽屉窗口（不触碰用户配置与用户文件），随后分别以 96 / 144 / 192 调用 `UpdateFooterLayout` 并 `PerformLayout`：
  - 容器 padding：`12/8` → `18/12` → `24/16`，图标列 `24` → `36` → `48`，标签左 inset `8` → `12` → `16`，全部与 `dpi/96` 换算一致；
  - 图标 `PictureBox` 尺寸等于图标列宽（24/36/48），且图片本身保持正方形并完整显示——修复了改动前 42px 固定列裁切 48px 图标的问题；
  - 标签左边界 = 容器左 padding + 图标列宽，标签与图标 `Top`/`Height` 相同（同一行垂直居中），标签右边界不越过右 padding；
  - 长路径文本测得的首选宽度 2371px 远大于可用宽度（386/360/334px），即确实走 `AutoEllipsis` 单行截断；
  - 截图为 `Z:\Temp\windrawer-stage91-footer-{96,144,192}.png`，肉眼可见：文件夹图标完整、路径单行并以 `…` 结尾、最左侧留白、图标与文字同列居中、关闭按钮位置未变。
  - 说明：探针中唯一的 FAIL 项是**探针自身**写错的断言（要求 `PictureBox` 必须正方形）；实际是 `Dock = Fill` 填充整行、正方形图片 `CenterImage` 居中，属预期行为。探针只强制切换了底栏度量，窗口本身仍在 192 DPI 显示器上，因此不等于真实的跨显示器 DPI 切换验证。
  - 探针工程位于 `C:\Users\Public\windrawer-stage91-harness`，验证后已删除；`Z:\Temp` 下的截图与 `windrawer-stage91-probe.log` 保留供查看。
- **修正（9.1.1，路径仍会折行）**：用户反馈窄窗口下路径出现换行，经复现确认——原生 `Label` 的 `AutoEllipsis` **不是**“强制单行”：当控件高度容得下多行时，它仍按词/中文字符折行，省略号只落在最后一行（GDI 与 GDI+ 两条绘制路径、`Sunny.UI.UILabel` 表现相同）。此前的探针在 192 DPI 下运行，50px 行高只容得下 1 行文字（字体行高 31.7px），因此没有暴露该行为；100% DPI 下行高可容 3 行，就会折行。
  - **实现**：新增 `src/Controls/EllipsisLabel.cs`（`Label` 子类），在 `OnPaint` 中自行用 `TextRenderer.DrawText` 绘制，固定 `SingleLine | EndEllipsis | VerticalCenter | NoPadding | NoPrefix`，并遵循 `Padding`、`TextAlign`（左/中/右）与 `RightToLeft`；背景仍由 `Control.OnPaintBackground` 绘制，因此 `Color.Transparent` 正常工作。`DrawerWindow.Designer.cs` 的 `fullPathLabel` 改为 `EllipsisLabel`，移除 `AutoEllipsis` / `UseMnemonic` 设置。
  - **已验证**：对照探针在同一 180px 宽、150px 高（可容 4 行）的标签上渲染同一条带空格的长路径——原生 `Label`（GDI）、原生 `Label`（GDI+）、`Sunny.UI.UILabel` 全部折成 4 行；`EllipsisLabel` 始终单行并显示 `C:\Users\Exa…`，透明背景下的前景色也正常。真实 `DrawerWindow` 在 700 / 600 / 530 三种宽度下重新渲染，路径均为单行 `…`，图标、主题色与关闭按钮无回归。
- **仍未验证**：真实 100% DPI 机器上的应用运行（折行行为用“加高标签”等效验证，未在 96 DPI 显示器上实跑）、点击图标/文字/中间空隙是否只触发一次打开、ToolTip 在截断后是否显示完整路径、主题与抽屉专属蒙版下的文字对比度、`MinimumSize (530, 320)` 与 `drawers[].size` 缩放往返、真实跨显示器 DPI 切换（`DpiChanged` 路径）的观感。
- **未实现**：整块路径区域的 hover 高亮（属于待确认项，默认仅手型光标 + ToolTip）。

### 9.2 已落地（拖出）

- **新增 `src/Forms/DrawerWindow.DragDrop.cs`**（partial），由 `InitializeWindow()` 在 `InitializeDrawerContextMenu()` 之后调用 `InitializeDragDrop()`；只订阅 `drawerContentsListView.ItemDrag`，不额外引入拖放库、不注册 `AllowDrop`。
- **拖动内容**：仅处理鼠标左键拖动；`CollectDragOutPaths(draggedItem, selectedItems)` 决定拖出集合——被拖项在选区中且选区多于 1 项时拖出**整个选区**（资源管理器习惯），否则只拖被拖的那一项；忽略没有 `DrawerItemInfo` 的项，按 `OrdinalIgnoreCase` 去重。
- **交给 Windows**：用 `DataObject.SetFileDropList`（`CF_HDROP`）发布路径，`DoDragDrop(..., DragDropEffects.Copy | DragDropEffects.Move)`，**不包含 `Link`**，因此目标不会创建快捷方式；移动/拷贝、进度窗口、冲突处理完全由 Explorer 完成，应用不调用 `File.Move`/`File.Copy`。
- **刷新策略**：拖动返回后仅当效果含 `Move` 才 `RefreshDrawerAsync()`（文件可能已离开目录）；`Copy` 或取消（`None`）不刷新，保持滚动位置与已加载缩略图。异常记录 `LogError` 并 `UiMessages.ShowError`，不退出应用；一次拖出只写一条 `LogInformation`（项数 + 抽屉目录 + 效果）。
- **多选（D6 按建议默认实施）**：`drawerContentsListView.MultiSelect = true`（原为 `false`）。这是计划中标注“需要确认”的项；若不希望改变现有选择手感，把该属性改回 `false` 即可，`CollectDragOutPaths` 在单选状态下会自动退化为只拖被拖项。
- **已验证（构建）**：Debug 与 Release 完整 `dotnet build` 均 **0 警告 0 错误**。
- **已验证（临时探针，真实 `DrawerWindow` + `%TEMP%` 临时目录）**：探针写入 3 个条目（含带空格与中文的文件名）后读取真实列表：
  - `MultiSelect = True`，列表正常列出 3 项；
  - 无被拖项 → 0 条路径；
  - 单选拖动 → 1 条、且路径在磁盘上存在；
  - 选中 2 项后拖动其中一项 → 2 条、全部存在；
  - 已选中 2 项时拖动**未选中**的第 3 项 → 只返回第 3 项；
  - 两个条目共享同一个 `DrawerItemInfo`（同路径）→ 去重后 1 条。
  - 探针位于 `C:\Users\Public\windrawer-drag-probe`，验证后已删除；日志保留在 `Z:\Temp\windrawer-drag-probe.log`。
- **未验证**：真实的 OLE 拖动过程无法自动化——把条目拖到资源管理器同盘/异盘的移动/拷贝效果、`Ctrl`/`Shift` 修饰键、Esc 取消后列表不变、Move 后自动刷新与缩略图重新加载、多选的手感、右键拖动（当前显式忽略）均需人工验收。`ItemDrag` 的事件订阅也未通过反射确认（`ListView` 的 `ItemDrag` 事件键不在其静态字段中，探针无法定位），只能由 `InitializeWindow → InitializeDragDrop()` 的代码路径保证；请在实际应用中拖动验证。
- **未实现**：自定义拖影/拖动提示层、`Link`（快捷方式）、从抽屉拖到主窗口或另一个抽屉（当前同时只打开一个抽屉）。

### 9.3 已落地（拖入）

- **新增 `src/Drawers/DrawerDropPlanner.cs`**（纯逻辑，不访问文件系统）：
  - `GetNormalizedPath`（`PathComparison.Normalize` + `Path.GetFullPath` + `Path.TrimEndingDirectorySeparator`，无效输入返回 `null`）、`GetVolumeRoot`、`IsSameVolume`、`IsSameOrChildPath`；
  - `Plan(sourcePaths, destinationFolder, allowedEffects, modifierKeys)` 返回 `DrawerDropPlan(MovePaths, CopyPaths, Rejection)` 与 `AllowedEffect`（用于拖动中的光标）；
  - 规则：同卷 Move / 异卷 Copy；`Ctrl` 强制 Copy、`Shift` 强制 Move（两者同时按下或无修饰键时回到卷默认）；期望效果不在 `allowedEffects` 中则改用另一种，两者都不允许 → `NoAllowedEffect`；源已在目标目录内 → 跳过（no-op）；目标等于或位于某个被拖动目录内部 → 整个 drop 拒绝（`DestinationInsideSource`）；按 `OrdinalIgnoreCase` 去重。
- **新增 `src/Infrastructure/ShellFileOperation.cs`**：`IFileOperation` COM 互操作。
  - 按 `shobjidl_core.h` 顺序声明全部成员（含未使用的占位），避免 vtable 偏移；`SetOperationFlags(FOF_ALLOWUNDO)` + `SetOwnerWindow`；
  - 用 `SHCreateItemFromParsingName` 取 `IShellItem`，逐个 `MoveItem` / `CopyItem` 入队，最后**一次** `PerformOperations()`，再 `GetAnyOperationsAborted()`；
  - `ERROR_CANCELLED` / `COPYENGINE_E_USER_CANCELLED` 识别为用户取消，其余负 HRESULT 作为失败返回；所有 `IShellItem` 与 `IFileOperation` 在 `finally` 中 `FinalReleaseComObject`（容错）。
- **`src/Forms/DrawerWindow.DragDrop.cs` 拖入绑定**：仅 `drawerContentsListView.AllowDrop = true`（不把整个窗体设为投放目标）；`DragEnter` 检查目标目录并解析一次 `CF_HDROP` 路径（`File.Exists`/`Directory.Exists` 过滤，虚拟条目丢弃），`DragOver` 只在数据对象变化时重新解析、但每次都按当前修饰键重算计划（避免每次鼠标移动都做文件系统 I/O）；`DragDrop` 不信任缓存，重新读取并校验；目标目录不可用或拒绝原因需要提示时走 `UiMessages`；执行完成后一律 `RefreshDrawerAsync()`（取消也可能留下部分改动）；一次投放写一条 `LogInformation`，失败写 `LogError`。
- **`AGENTS.md` 已同步**：产品约定改为“应用自身不主动改动用户文件；唯一例外是用户在抽屉列表上显式拖放、且完全由 Windows Shell 执行的移动/拷贝”。
- **已验证（构建）**：Debug 与 Release 完整 `dotnet build` 均 **0 警告 0 错误**。
- **已验证（临时探针，26 项全部 PASS，关联真实源文件/真实 COM）**：
  - 规划器：`C:\Foo\Bar` → 根 `C:\`；盘符大小写同一卷；`C:` vs `D:` 不同卷；UNC 同共享同卷、不同共享不同卷；自身/子目录判定正确，`C:\Foobar` 不被当作 `C:\Foo` 的子目录，父目录也不是自身子目录的子目录；同卷默认 Move、异卷默认 Copy；Ctrl 强制 Copy、Shift 强制 Move；仅允许 Copy / 仅允许 Move 时正确回退；无允许效果 → `NoAllowedEffect`；源已在目标目录 → `NoSources`；目标位于被拖动目录内 → `DestinationInsideSource`；重复源合并；混合批次拆成 1 个 Move + 1 个 Copy。
  - `IFileOperation`：真实临时目录中一次调用同时执行 1 个 Move + 1 个 Copy，返回 `HRESULT=0x00000000`、未取消，文件确实被移动/拷贝（验证了 vtable 顺序与 COM 释放）。
  - 窗口级端到端：反射调用真实 `DrawerWindow` 的 `DragDrop` 处理器（`DataObject` + `FileDrop`），同卷投放回报 `DragDropEffects.Move`，文件被移入抽屉目录，随后列表自动刷新并显示该条目。
  - 探针位于 `C:\Users\Public\windrawer-drop-probe`，验证后已删除；日志保留在 `Z:\Temp\windrawer-drop-probe.log`。
- **未验证（需人工）**：真实鼠标拖入（OLE 层的光标效果与 `DragEnter/DragOver` 节奏）、异盘拷贝、重名冲突对话框（替换/跳过/取消）、只读或被占用文件、超长路径、拖入后缩略图按当前排序出现、Shell 进度对话框期间的界面阻塞观感、把文件夹拖进自身子目录时的警告提示。
- **已知取舍**：一次投放若同时包含 Move 与 Copy，`DragEventArgs.Effect` 只能回报单一效果（有 Move 就回报 `Move`），这是 WinForms 的限制；实际文件操作仍按分组执行。

### 9.4 已落地（文档与回归）

- **`README.md`**：简介、Highlights、Run 章节与项目结构均已同步——新增“Drag files in and out”“Clickable folder link”两条亮点；Run 章节说明拖出、拖入、同盘/异盘语义、Ctrl/Shift、自身子目录拒绝、仅接受真实文件系统条目，以及底栏入口的单行省略与完整路径 ToolTip；文件清单补充 `DrawerWindow.DragDrop.cs`、`EllipsisLabel.cs`、`DrawerDropPlanner.cs`、`ShellFileOperation.cs`。
- **`docs/CONFIGURATION_FORMAT.md`**：本阶段**没有新增或修改任何 TOML 字段**；在第 5 节写入约束中补充说明拖入/拖出、底栏入口和排序菜单都是运行时交互，不写配置。
- **`AGENTS.md`**：已在 9.3 同步产品约定（唯一例外：用户显式拖放、完全由 Windows Shell 执行的移动/拷贝）。
- **构建验证**：Debug 与 Release 均走完整 `dotnet build`（未使用 `-t:Compile`），**0 警告 0 错误**。
- **发布验证**：`dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true` 成功（退出码 0），产物写入 `Z:\Temp\windrawer-publish-94`（`winDrawer.exe` 约 4.8 MB + `winDrawer.pdb`）；本阶段未新增嵌入资源，模板/字体/图标仍由既有构建管线打包。
- **自动化验证汇总**（临时探针，`%TEMP%`/`C:\Users\Public`，均已删除）：9.1 底栏度量与渲染（96/144/192 三档 + 真实窗口 700/600/530 宽度）；9.1.1 `EllipsisLabel` 与 `Label`/`UILabel` 的折行对照；9.2 拖出路径收集（多选、未选中项、去重、路径存在性）；9.3 规划器规则、真实 `IFileOperation` 移动+拷贝、窗口级拖入端到端。日志仍保留在 `Z:\Temp\windrawer-*.log` 与截图 `Z:\Temp\windrawer-stage91-footer-*.png`。
- **手工 UI 回归：未执行。** 以下项目必须由你在真实应用里验收，本文档不声称已通过：真实鼠标拖出/拖入（含 OLE 光标）、异盘拷贝、重名冲突（替换/跳过/取消）、Esc 取消、只读/被占用文件、超长路径与长中文路径、拖放后缩略图与排序、多选手感；底栏在 100%/150%/200% DPI 下的对齐与截断、点击区域只触发一次、ToolTip、深浅主题与抽屉蒙版对比度、`MinimumSize (530, 320)`、`drawers[].size` 缩放往返；以及既有项（双击打开、F5/右键菜单三种排序、托盘与 Win+Shift+D 显隐、顶部缩放条、AlwaysOnTop、设置保存/取消/Default）。
- **`docs/TODOs.md` 未改动**：两条待办（拖放、底栏入口）在人工验收通过并经你确认后再移除，不覆盖该文件当前的未提交改动。

