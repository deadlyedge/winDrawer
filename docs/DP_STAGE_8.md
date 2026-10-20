# 阶段八建议：快捷键与显示/隐藏语义、顶部边带、抽屉右键菜单

我检查了当前实现、TODO 和工作树状态。`git status --short` 只有 `M docs/TODOs.md`（未提交的用户改动，内容正是这几条待办）；我保留了该文件原样，没有改动任何源码。

本阶段按你的确认收敛范围：**新增“启用 Win+Shift+D”设置项（D1，被占用时警告但保留用户选择 = D1b，默认 `true` = D1c）**、**托盘与快捷键都同时隐藏/显示主窗口与已打开的抽屉（D2）**、**排序模式持久化到 TOML（D3）**、**右键菜单用 WinForms `ContextMenuStrip` + 主题色（D4）**、**菜单与排序文案用英文、TOML 取值小写（D9）**、**“按名称”保持目录与文件混排（D8）**；**文件拖入/拖出整体推迟到阶段九（D5–D7 暂不实施）**。

## 已核实的现状

### 1. 快捷键、显示/隐藏与设置项

- 全仓库检索 `RegisterHotKey` / `WM_HOTKEY` / `HotKey` / `ModifierKeys` 均无命中：当前没有任何快捷键注册，也没有相关配置字段。
- 显示/隐藏只有托盘一条入口：`TrayController` 的 `Show/Hide` 菜单和双击托盘图标 → `MainWindow.ToggleVisibility()`（`src/Forms/MainWindow.Tray.cs:42`），它只对主窗口 `Hide()` / `Show()+Activate()`。
- 主窗口是抽屉窗口的 owner（`drawerWindow.Show(this)`，`src/Forms/MainWindow.Drawers.cs:137`）。本次实测：owner `Hide()` **不会**隐藏 owned 窗口（`Visible=True`、`IsWindowVisible=True`），因此 D2 必须显式同步隐藏/显示抽屉窗口，不能依赖 owner 语义。
- 主窗口 `FormBorderStyle.None`、正常情况下 `ShowInTaskbar = false`（仅托盘初始化失败时回退为 `true`）。
- 设置面板行为项由 SunnyUI `UICheckBoxGroup` 承载：Designer 中 `Items.AddRange(new object[] { "Start with Windows", "Lock position", "Always on top" })`（`src/Forms/SettingsForm.Designer.cs:185`），索引常量在 `SettingsForm.cs:10-12`（`StartWithWindowsIndex=0`、`LockPositionIndex=1`、`AlwaysOnTopIndex=2`）。新增“启用快捷键”就是追加第 4 项（索引 3），并按既有模式接入 `ApplySnapshotToUi`、`SyncWorkingSnapshotFromUi`、`OnBehaviorValueChanged`、取消还原与预览事件（`WindowLockPreviewChanged`/`AlwaysOnTopPreviewChanged` 是现成范本）。
- 预览/保存链路：`SettingsForm` 抛预览事件 → `MainWindow.AttachSettingsHandlers` 挂接的 `Settings_*` 处理器即时生效；Save 走 `SettingsForm.SaveButton_Click` → `MainWindow.ApplyAcceptedSettings`（重建快照 + `_settingsRepository.Update`）；Cancel 走 `SettingsForm.CancelButton_Click` → `MainWindow.RevertSettingsPreview`；Default 走 `Settings_StartupDefaultsResetRequested` → `ResetStartupToEmbeddedDefaults`（`GetEmbeddedStartupDefaults` 只取内嵌模板的 `[startup]` 值，保留 drawers）。
- 配置写入：`SettingsRepository.Update` 逐键写入 `[startup]`（`src/Configuration/SettingsRepository.cs:54-61`），`GetEmbeddedStartupDefaults`（`:70-83`）与序列化路径（`SettingsRepository.Serialization.cs`，除已废弃的 `icon_size` 外按表逐键拷贝）都会自动带上新键；解析在 `SettingsSnapshot.FromModel`（布尔用 `GetBoolean`，缺失/类型错误回退默认值）。根目录模板 `Drawer-settings.toml` 目前只有 6 个 startup 键，没有快捷键项。

### 2. 抽屉顶部边带

- `DrawerWindow.CreateParams` 移除 `WS_CAPTION`（`0x00C00000`），Designer 仍为 `FormBorderStyle.Sizable`、`ControlBox = false`、`ShowIcon = false`、`AutoScaleMode = None`、`MinimumSize = (530, 320)`。
- 客户区内没有额外留白：`mainTableLayoutPanel`、`drawerContentsHostPanel`、`drawerHeaderTableLayoutPanel` 的 `Margin`/`Padding` 都是 0，第 0 行（内容区）占 `100%`。
- **结论（有实测支撑，见下节）：** 去掉 `WS_CAPTION` 后，窗口矩形到客户区的非客户区内缩**四边对称**（200% DPI 下 13/13/13/13）；但 DWM 只在左/右/下把其中 11px 视作“不可见缩放边框”，于是可见像素带变成**顶部 13px、其他三边各 2px**，并且顶部这段带由 DWM 用窗框颜色（本机为系统强调色）绘制。这才是“顶部明显宽于其他三边”的原因。
- 因此不能靠调 `Padding`/`Margin`/`BackColor`/`Opacity` 修好：这条带位于非客户区，由 DWM 绘制；只能在 `WM_NCCALCSIZE` / 窗口样式层面处理，见阶段 8.3。

### 3. 抽屉刷新与排序入口、排序契约

- 底部按钮行 `drawerButtonTableLayoutPanel`（右对齐 `FlowLayoutPanel`）当前放 `closeDrawerButton` 与 `refreshDrawerButton`；刷新按钮是 Segoe MDL2 Assets 字形 `\uE72C`，在 `DrawerWindow.InitializeWindow` 订阅 `refreshDrawerButton.Click += RefreshDrawerButton_Click`，实现为 `await PopulateFileListAsync()`（`src/Forms/DrawerWindow.FileList.cs:49`）。
- 与主题/无障碍耦合：`ApplyTheme` 会调用 `ApplyThemeToButton(refreshDrawerButton, isCloseButton: false)`；Designer 中为刷新按钮设置了 `AccessibleName`/`AccessibleDescription`/`ToolTip`（`src/Forms/DrawerWindow.Designer.cs:187-199`）。删除按钮时必须一并清理。
- 全项目当前没有任何窗体使用 `ContextMenuStrip`（唯一的 `ContextMenuStrip` 在 `TrayController` 中用于托盘菜单），也没有 `UIContextMenuStrip` 使用处；没有抽屉级快捷键（F5/Ctrl+R）。
- 排序目前是固定契约：`DrawerItemEnumerator.CompareItems` 只有“`LastWriteTimeUtc` 降序”一种，`Enumerate` 不接受模式参数；`DP_STAGE_7.md` 的“排序语义（已确认）”是“目录与文件全体混排”，`NEXT_DEVELOPMENT_PLAN.md` 记录“排序不是配置项，不新增 TOML 字段”——这两处口径都要在本阶段按 D3/D8 改写。
- 持久化管线：`DrawerDefinition` 是不可变记录式类（`src/Configuration/DrawerDefinition.cs`），条目解析在 `SettingsSnapshot.ParseDrawers`（读取 `name`/`path`/`size`/`overlay_color`），写回单个抽屉字段的现成范本是 `SettingsRepository.UpdateDrawerSize`（按 `PathComparison.AreSame` 定位条目、写值、标脏并唤醒后台 writer，`src/Configuration/SettingsRepository.Drawers.cs:7-43`）。新增 `sort_mode` 应完全照此模式实现。

### 4. 文件拖放（本阶段推迟）

- `drawerContentsListView` 没有 `AllowDrop`；仓库内唯一的拖放代码是主窗口抽屉列表的顺序重排（与文件拖放无关）。相关分析与待定问题保留在文末“下一阶段候补”，本阶段不实现任何拖放代码。

## 实测证据（本次实际执行）

探针位于 `%TEMP%\windrawer-border-probe`（本机为 `z:\Temp\windrawer-border-probe`），它复刻了 `DrawerWindow` 的窗口样式（`FormBorderStyle.Sizable` + 去掉 `WS_CAPTION` + `ControlBox=false` + `ShowIcon=false` + `AutoScaleMode=None`），测量窗口矩形 → 客户区内缩、DWM 可见窗框边界，并对屏幕上四条边做像素扫描（客户区填 `#00FF00`）。

命令与产物：`dotnet build -c Release` 后运行 `bin\Release\net10.0-windows\probe.exe`（WinExe，结果写入同目录 `probe.log`）。

```text
metrics: SM_CXSIZEFRAME=5 SM_CYSIZEFRAME=5 SM_CXPADDEDBORDER=8 SM_CYCAPTION=45    (dpi=192，即 200% 缩放)
1) Sizable + caption (baseline):                        window=788x654 client=762x583 insets L=13 T=58 R=13 B=13
2) Sizable, WS_CAPTION stripped (mimics DrawerWindow):   window=788x609 client=762x583 insets L=13 T=13 R=13 B=13
3) None (mimics MainWindow):                             window=762x583 client=762x583 insets L=0  T=0  R=0  B=0

B) Sizable + WS_CAPTION stripped 的可见像素扫描（窗口 326x326，客户区 300x300）：
   visibleFrameBounds=304x315，deltaVsWindowRect L=11 T=0 R=11 B=11
   topEdgeColors(center column)  y0..y12 = #F7630C（系统窗框/强调色），y13 起 = #00FF00（客户区）
   leftEdgeColors(center row)    x11..x12 = #F7630C，x13 起 = #00FF00（x0..x10 落在可见窗框之外）
A) 有标题栏基线：顶部 58px 同样由 DWM 用该强调色绘制（13px 边框 + 45px 标题栏）

owned visibility: owner shown → owned Visible=True, IsWindowVisible=True；
                  owner.Hide() → ownerVisible=False, ownedVisible=True, IsWindowVisible=True；
                  owner.Show() → owned 仍 Visible=True
```

结论：

1. 非客户区内缩四边对称，**不是**布局或 `WS_CAPTION` 造成的非对称内缩。
2. 视觉上的非对称来自 DWM：左/右/下各有 11px 属于不可见缩放边框（不显示），顶部没有这段不可见边框，于是 13px 全部以窗框色呈现。200% DPI 下顶部 ≈13px、其他三边 ≈2px；100% DPI 时按 DPI 缩放约为 8px vs 1–2px，**实施时必须复测**。
3. owned 窗口不会随 owner 隐藏而隐藏 → D2 需要显式 `Hide()`/`Show()` 抽屉窗口，并在重新显示时重新贴合主窗口位置。

**明确未验证：** 没有在真实应用窗口上做像素级截图复核（探针只复刻窗口样式，未运行真实 `DrawerWindow`）；未在 100%/150% DPI 下测量；未测多显示器、贴靠（Aero Snap）与阴影/圆角表现。

## 本阶段已确认的决策与落地方案

| 决策 | 结论 | 落地要点 |
|---|---|---|
| D1 | 设置面板新增“启用快捷键 Win+Shift+D”选项 | 新 TOML 字段 `startup.toggle_hotkey_enabled`（Boolean，默认 `true`），设置项追加为 Behavior 第 4 项（英文标签 `Enable Win+Shift+D`） |
| D1b | 热键被占用时：**警告但保留并持久化用户的选择** | 启动与句柄重建时只写 `Warning` 日志；只有在设置面板显式打开该选项时用 `UiMessages.ShowWarning` 告知被占用；不自动把复选框改回关闭、不阻止保存，下次启动或句柄重建时重试注册 |
| D1c | `toggle_hotkey_enabled` 默认 `true` | 内嵌模板与解析回退值都取 `true`；缺失或类型错误同样按 `true` 处理，保持“启动后即可用” |
| D2 | 托盘与快捷键都同时隐藏/显示主窗口与已打开的抽屉 | 统一 `ToggleVisibility`：隐藏时同时 `Hide()` 抽屉，显示时重新贴合主窗口位置并 `Show()`；托盘 `Show/Hide` 菜单与 Win+Shift+D 共用这一条语义 |
| D3 | 排序模式持久化到 TOML | 新字段 `drawers[].sort_mode = "newest" \| "oldest" \| "name"`，缺失/未知/类型错误按 `newest` 处理；切换时经仓储写回 |
| D4 | 右键菜单用 WinForms `ContextMenuStrip` + `ThemePalette` 着色 | 不使用 SunnyUI `UIContextMenuStrip`；与 `TrayController` 现有 `ContextMenuStrip` 用法一致 |
| D5–D7 | 拖放功能本阶段不做 | 移到文末“下一阶段候补”，本阶段不新增拖放代码、不新增相关测试 |
| D8 | “按名称”排序保持目录与文件混排 | 三种模式都沿用“目录与文件混排”，只改变主比较键（时间或名称） |
| D9 | 菜单与排序文案用英文，TOML 取值小写 | 菜单项 `Refresh` / `Sort by name` / `Newest first` / `Oldest first`；`sort_mode` 取值 `newest` / `oldest` / `name`；不使用中文标签 |

本阶段全部决策已确认（D1、D1b、D1c、D2、D3、D4、D8、D9）；只剩 D5–D7 的拖放问题留待阶段九，见文末“下一阶段候补”。


## 建议分阶段实施

### 阶段 8.1：全局快捷键 Win+Shift+D 与“启用快捷键”设置项

1. **配置字段（D1）**：`SettingsKeys` 新增 `ToggleHotkeyEnabled = "toggle_hotkey_enabled"`；`SettingsSnapshot` 增加 `ToggleHotkeyEnabled` 属性（构造参数给默认值 `true`，`Clone()`/`FromModel` 一并处理，解析沿用 `GetBoolean(startup, SettingsKeys.ToggleHotkeyEnabled, fallback: true)`）；`SettingsRepository.Update` 写入该键，`GetEmbeddedStartupDefaults` 传递该值；根目录模板 `Drawer-settings.toml` 增加 `toggle_hotkey_enabled = true`（Default 重置依赖模板中的 `[startup]` 键，缺失才不会被视为默认）。
2. **设置面板（D1）**：Designer 中 `behaviorCheckBoxGroup.Items.AddRange` 追加英文项 `Enable Win+Shift+D`（第 4 项），`SettingsForm` 增加索引常量 `ToggleHotkeyIndex = 3`，并接入现有五处：`ApplySnapshotToUi`（勾选状态）、`SyncWorkingSnapshotFromUi`（写入工作快照）、`OnBehaviorValueChanged`（变更时抛新预览事件）、`CancelButton_Click`（回滚时还原）、无障碍名称（`behaviorCheckBoxGroup` 的 L4 待办可顺带补 `AccessibleName`）。
3. **热键封装**：新增 `src/Infrastructure/GlobalHotKey.cs`，封装 `RegisterHotKey`/`UnregisterHotKey`，只暴露 `TryRegister(IntPtr windowHandle, int id, HotKeyModifiers modifiers, Keys key, out int errorCode)` 与 `Unregister(IntPtr windowHandle, int id)`；修饰键用 `MOD_WIN | MOD_SHIFT | MOD_NOREPEAT`（`MOD_NOREPEAT = 0x4000`，避免长按自动重复反复开关）；热键 id 与按键集中到 `AppConstants`（新增 `AppConstants.HotKeys`），不要散落字面量。
4. **注册与消息**：`MainWindow` 覆盖 `WndProc` 处理 `WM_HOTKEY`（`0x0312`），`wParam` 等于我们的 id 时调用统一后的显示/隐藏逻辑（8.2）并写 `Information` 日志；注册放在 `OnHandleCreated`（句柄重建后自动重新注册），注销放在 `OnFormClosed`，与现有“字体 → 托盘 → 仓储”释放顺序并列，不新增退出路径。
5. **开关语义**：新增 `MainWindow.ApplyToggleHotkey(bool enabled)`（或等价的幂等 `SyncHotkeyRegistration`）：`enabled=false` 时注销，`enabled=true` 且句柄已创建时注册；`ApplySettings(snapshot)` 调用它，因此 Save 后立即生效、Cancel 经 `RevertSettingsPreview` 还原、Default 经 `ResetStartupToEmbeddedDefaults` 按模板值还原。预览事件用 `_toggleHotkeyPreview`（`bool?`）记录，`Settings_FormClosed` 里与 `_windowLockedPreview`/`_alwaysOnTopPreview` 一起清空。
6. **失败处理（D1b 已确认）**：注册失败（例如 1409 `ERROR_HOTKEY_ALREADY_REGISTERED`）一律写 `Warning` + `Marshal.GetLastWin32Error()`，不阻塞启动、不新增模态弹窗；只有在用户**在设置面板显式打开**该选项时用 `UiMessages.ShowWarning` 提示“该组合键已被其它程序占用，本次未能生效”，**保留并持久化用户的选择**（不把复选框改回关闭），下次启动或句柄重建时重试注册。

**验收：** 默认配置下启动即可用 Win+Shift+D 切换显示/隐藏；设置面板取消勾选后热键立即失效、重新勾选后立即恢复；Save 后重启仍按配置生效；Cancel 能还原勾选状态与热键注册状态；Default 按模板值（`true`）重置并同步注册；长按不产生抖动；热键被占用时应用正常启动且有可诊断日志；窗口句柄重建后热键仍生效；退出应用后该组合键不再由本项目响应；`toggle_hotkey_enabled` 缺失/类型错误按 `true` 处理且不被写坏。

### 阶段 8.2：统一显示/隐藏语义（主窗口 + 已打开的抽屉）

1. **统一入口（D2）**：把 `MainWindow.ToggleVisibility()` 扩展为“同时作用于主窗口与 `_currentDrawerWindow`”，托盘 `Show/Hide` 菜单、托盘双击、Win+Shift+D 全部走这一条路径；不要在 `DrawerWindow` 或其它分部文件里另加隐藏逻辑。
2. **隐藏**：先 `_currentDrawerWindow?.Hide()`，再 `Hide()` 主窗口（已打开且未释放的抽屉才处理）；日志文案改为同时说明主窗口与抽屉。
3. **显示**：`Show()` 主窗口 → `WindowState = FormWindowState.Normal` → 重新贴合抽屉位置（复用 `MainWindow_LocationChanged` 中的 `_currentDrawerWindow.Location = new Point(Right, Top)` 逻辑，抽成小的定位方法，避免两处重复）→ `_currentDrawerWindow?.Show()` → `Activate()`。若抽屉处于最小化或已关闭状态，则不做额外处理，不要让切换逻辑顺手新建抽屉窗口。
4. **必须验证的行为细节**：
   - 抽屉隐藏再显示后**不应**重新枚举目录或重新生成缩略图（`Form.Shown` 只在首次显示时触发；实现后需实测确认没有重复的 `PopulateFileListAsync`/日志），滚动位置、选中项与已加载图像保持有效；
   - 显示后抽屉回到主窗口右侧，不会停留在旧位置（主窗口可能在隐藏期间被其它方式移动过，例如设置面板的窗口高度预览）；
   - 反复切换不产生额外窗口、额外任务栏按钮或孤立窗口；抽屉已关闭时切换只影响主窗口；
   - `AlwaysOnTop` 开启时，隐藏/显示后的置顶状态与 `ApplyAlwaysOnTop` 保持一致。
5. **文档口径**：托盘菜单文案（`Show/Hide`）与 README 的托盘/快捷键说明都要写成“隐藏/显示主窗口与已打开的抽屉”，不要再描述成仅主窗口。

**验收：** 托盘菜单、托盘双击与 Win+Shift+D 三种入口行为一致，都会把主窗口与已打开的抽屉一起隐藏、一起显示；显示后抽屉位置正确、内容与滚动状态保持、不发生重复加载；抽屉已关闭或从未打开时只切换主窗口；重复切换不产生额外窗口或任务栏按钮；退出仍走 `ShutdownApplication` 单一路径。

### 阶段 8.3：收窄抽屉顶部可见边带

1. **先复测**：用同一探针（`%TEMP%\windrawer-border-probe`）记录你当前 DPI（本次为 200%：顶部 13px / 其他 2px）以及 100% 下的数值，并保存“修复前”截图；真实的 `DrawerWindow` 至少做一次肉眼截图对照。
2. **首选方案**：在 `DrawerWindow` 覆盖 `WndProc`：
   - `WM_NCCALCSIZE`（`0x0083`）：把 `NCCALCSIZE_PARAMS.rgrc[0].Top` 调整为 0（其余边保持系统默认），让客户区顶到可见上边缘，从而消除 DWM 绘制的顶部色带；
   - `WM_NCHITTEST`（`0x0084`）：在客户区顶部约 8 逻辑像素（按 `DeviceDpi` 缩放）内返回 `HTTOP`/`HTTOPLEFT`/`HTTOPRIGHT`，保证顶部仍可缩放，其余位置保持默认命中结果；
   - 两个分支都必须继续走 `base.WndProc` 的正常路径（或返回文档化的结果），不要顺手改动无关消息处理，也不要破坏 `ScrollableListView` 的原生滚动逻辑。
3. **必须专门验证的副作用**（这些比“看起来对不对”更容易出问题）：
   - 尺寸往返是否漂移：`PersistWindowSize()` 把 `Size` 写入 `drawers[].size`，`OnShown` 又用 `PreferredSize` 恢复；改动非客户区后 WinForms 计算的 `ClientSize` 可能与真实客户区相差一个顶部内缩，需保证“打开 → 拖动缩放 → 关闭 → 重开”不会逐次长高/变矮；
   - `MinimumSize = (530, 320)` 仍生效；
   - `PositionDrawerScrollBar()` 基于 `ClientSize` 的滚动条定位与叠放仍正确；
   - `DpiChanged` → `RefreshFileListForDpi` 后不闪烁、不错位、不重复加载；
   - 贴靠（Aero Snap）、阴影与圆角是否仍可用，是否出现拖到屏幕边缘后无法还原等问题。
4. **回退方案**（若第 2 步有不可接受的回归）：把抽屉改为 `FormBorderStyle.None` + 自绘边缘缩放（与主窗口一致的无边框路线），并在文档与 README 中明确“失去原生边框/阴影/圆角”的取舍；或明确记录“保留现状，并在 README 说明这是 Windows 对无标题栏可缩放窗口的渲染表现”，由你决定是否接受。

**验收：** 在 100% 与你当前 DPI 下，顶部不再出现明显更宽的色带，四边可见边带基本等宽；缩放、最小尺寸、尺寸持久化、滚动条与 DPI 切换不回归；如果最终选择保留现状，必须在 README/文档中写清原因与实测数值，而不是当成“已修复”。

### 阶段 8.4：抽屉右键菜单（刷新 + 三种排序）与移除右下角刷新按钮

1. **移除刷新按钮**：`DrawerWindow.Designer.cs` 删除 `refreshDrawerButton` 字段、初始化块、`drawerToolTip` 提示与无障碍文本；`DrawerWindow.InitializeWindow` 删除 `refreshDrawerButton.Click += ...`；`DrawerWindow.ApplyTheme` 删除 `ApplyThemeToButton(refreshDrawerButton, ...)`；`drawerButtonTableLayoutPanel` 只保留关闭按钮，仍保持右对齐与主题配色。把 `RefreshDrawerButton_Click` 重命名为菜单与快捷键共用的 `RefreshDrawerAsync()`，避免留下“按钮处理器”式命名。
2. **排序模式与比较器（D8）**：新增 `src/Drawers/DrawerSortMode.cs`（`NewestFirst` / `OldestFirst` / `NameAscending`，TOML 取值 `newest` / `oldest` / `name`），`DrawerItemEnumerator.Enumerate(folderPath, sortMode, cancellationToken)` 与 `CompareItems(left, right, sortMode)` 按模式分派：
   - `NewestFirst`：`LastWriteTimeUtc` 降序（当前行为，默认值）；
   - `OldestFirst`：`LastWriteTimeUtc` 升序；
   - `NameAscending`：名称 `OrdinalIgnoreCase` 升序；
   - 三种模式都**保持目录与文件混排**（D8）；时间模式的 tie-break 仍为“名称 `OrdinalIgnoreCase` 升序 → 完整路径 `Ordinal` 升序”，名称模式的 tie-break 为“完整路径 `Ordinal` 升序”；时间不可读的条目在时间排序中排最后（名称排序中按名称正常参与）。
   - 比较器保持 `internal static` 纯逻辑、排序过程中不做文件系统 I/O，便于 smoke harness 覆盖。
3. **持久化（D3）**：
   - `SettingsKeys` 新增 `DrawerSortMode = "sort_mode"`；
   - `DrawerDefinition` 增加 `SortMode` 属性（默认 `NewestFirst`，并提供不可变更新方式，例如 `WithSortMode(...)`），`SettingsSnapshot.ParseDrawers` 读取该键，取值 `"newest"`/`"oldest"`/`"name"`，缺失/未知/类型错误一律按 `newest`，不报错、不迁移；
   - `SettingsRepository` 新增 `UpdateDrawerSortMode(string drawerPath, DrawerSortMode mode)`，完全照 `UpdateDrawerSize` 的模式（`_writeLock` + `PathComparison.AreSame` 定位 + 写字符串值 + `_dirty` + `_writeSignal.Release()` + 日志）；
   - 只有在用户明确切换排序时才写入该键（缺失等价于 `newest`），不打扰未使用该功能的用户；`AddDrawer` 不主动写该键；
   - `DrawerWindow` 切换模式时更新自身 `_drawer` 并调用仓储写回，同时抛出 `SortModeChanged` 事件，`MainWindow` 收到后走现有 `ReloadSettingsFromStorage()`，保证重新打开或编辑抽屉时使用最新模式（现有 `FormClosed` 已走这条路）；
   - `Default` 按钮只重置 `[startup]`，**不得**改动任何抽屉的 `sort_mode`；这一点要在 `CONFIGURATION_FORMAT.md` 与回归清单里写明。
4. **右键菜单（D4，文案按 D9）**：给 `drawerContentsListView` 挂 WinForms `ContextMenuStrip`，项为 `Refresh`（`ShortcutKeyDisplayString = "F5"`）、分隔符、`Sort by name`、`Newest first`、`Oldest first`；三种排序互斥勾选（`Checked`，`CheckOnClick = false`），菜单打开前按当前模式刷新勾选；`ApplyTheme` 用 `ThemePalette` 着色（背景/前景/悬停/选中/边框，必要时配 `ToolStripProfessionalRenderer` + 自定义 `ProfessionalColorTable`），确保勾选与悬停状态在深浅主题与抽屉专属蒙版下都清晰；菜单在窗体 Dispose 路径中释放。
5. **快捷键与排序状态保持**：覆盖 `ProcessCmdKey` 处理 `F5` → `RefreshDrawerAsync()`（补上被移除按钮的能力，且只在抽屉窗口内生效）；当前模式在刷新、DPI 变化、`ApplyDrawerDefinition`、隐藏/显示（8.2）之后都保持不变。
6. **文档口径**：`DP_STAGE_7.md` 与 `NEXT_DEVELOPMENT_PLAN.md` 中“排序不是配置项、固定最新优先”的表述改为“默认 `newest`，可在抽屉右键菜单切换到 `oldest`/`name`，并持久化为 `drawers[].sort_mode`”。

**验收：** 右下角不再有刷新按钮，关闭按钮仍右对齐、主题与无障碍信息正常；列表任意位置右键都能弹出菜单；三种排序立即生效、刷新与 DPI 切换后保持、重启应用后按持久化值恢复；`sort_mode` 缺失/未知/类型错误的配置按 `newest` 读取且文件不被损坏，其它抽屉字段与未知键保持不变；F5 可刷新；菜单在浅色、深色与专属蒙版下可读；全仓库检索 `refreshDrawerButton` 无任何残留引用。

### 阶段 8.5：文档与回归

- **配置文档**：`CONFIGURATION_FORMAT.md` 第 2 节示例、第 3 节字段表（新增 `startup.toggle_hotkey_enabled` 与 `drawers[].sort_mode` 两行，写清默认值与无效值回退）、第 4/5 节规则、第 7 节测试清单（新增“快捷键开关缺失/真假/类型错误”“`sort_mode` 缺失/未知/类型错误”“Default 不改动 `sort_mode`”等条目）。
- **README**：全局快捷键与开关、隐藏/显示同时作用于主窗口与抽屉、右键菜单与三种排序及其持久化、F5 刷新、右下角刷新按钮已移除、顶部边带的最终结论（含实测数值）；`Drawers/`、`Infrastructure/` 文件清单补充新文件。
- **计划文档**：`NEXT_DEVELOPMENT_PLAN.md` 增加“阶段 8 收尾记录”；`DP_STAGE_7.md` 与 `NEXT_DEVELOPMENT_PLAN.md` 的排序契约改为“默认 `newest`，可切换并持久化”。
- **纯逻辑 smoke checks**（`%TEMP%` 下临时 harness、链接真实源文件、不新增测试工程）：三种排序模式（混排、方向、tie-break、时间不可读、空目录）、`sort_mode` 字符串 ↔ 枚举映射（含缺失/未知/大小写/非字符串）、`toggle_hotkey_enabled` 缺失/真假/类型错误解析、`UpdateDrawerSortMode` 写盘内容与未知键保留。
- **构建验证**：Debug/Release 走完整 `dotnet build`（**不要**用 `-t:Compile` 绕过资源生成）；`bin\<配置>` 被占用时改用 `-o <临时目录>`；框架依赖单文件发布检查嵌入资源（模板、字体、图标、WinForms 资源集）。
- **手工回归（必须实际执行并如实记录，不得把计划或临时 harness 说成已完成的 UI 验证）**：快捷键（注册/注销/被占用/句柄重建/退出后失效）、设置面板勾选与 Save/Cancel/Default 对热键的影响、托盘与热键隐藏/显示主窗口+抽屉（位置、滚动状态、无重复加载）、顶部边带（多 DPI 截图 + 缩放/最小尺寸/尺寸持久化/贴靠）、右键菜单与三种排序（含重启恢复）、F5、以及既有项（大目录滚动与缩略图释放、托盘/任务栏、设置窗口位置）。
- **工作保护**：每个子阶段开始前检查 `git status --short` 与目标文件 diff；测试只用 `%TEMP%` 临时目录；不写真实用户配置；不清理 `bin/`、`obj/`、发布产物、日志与 `.vs/`。
- 3 条 TODO（快捷键、顶部边带、右键菜单）全部验收后，再按约定从 `docs/TODOs.md` 移除对应条目——需要你确认，且不得覆盖该文件当前的未提交内容；拖放条目保留。

## 实施前提与偏离处理

- 本阶段的决策已全部确认（见“本阶段已确认的决策与落地方案”），实施时不需要再回到决策确认环节；D5–D7 的拖放问题留待阶段九。
- 若实施过程中发现某条约定与代码现状冲突（例如 `Form.Shown` 在隐藏后重新显示时确实会再次触发、或 `WM_NCCALCSIZE` 方案带来不可接受回归），先把结论与实测记录回写本文件，再按“8.3 回退方案”处理，或把新问题补回本文件重新确认，不要静默偏离。

## 建议顺序

先做 **8.1 快捷键与设置项**（含配置字段，独立且可立即验证），再做 **8.2 统一显示/隐藏语义**（托盘与热键共用，验证面集中在窗口显隐），随后做 **8.4 右键菜单、排序与移除刷新按钮**（改动面最大，含 TOML 字段与 Designer），然后做 **8.3 顶部边带**（涉及非客户区与窗口样式，回归面最大，放在其它改动稳定后），最后以 **8.5 文档与回归** 收尾。8.1 与 8.2 可以合并提交，但建议分开验证；8.3 不要与 8.4 的列表布局改动并行提交。

## 本阶段不做

- 不实现任何文件拖入/拖出（按 D5–D7 推迟到阶段九），不新增 `AllowDrop`、`DoDragDrop` 或拖放相关测试。
- 不引入 `IExplorerBrowser`/Shell 文件夹视图（`docs/TODOs.md` 已明确排除）。
- 不在抽屉内做子目录导航、递归扫描、`FileSystemWatcher` 实时监控、拖放排序或文件重命名。
- 不让应用自身删除/移动/重命名用户文件（现有产品约定：只列出条目并请求 Windows Shell 打开）。
- 不为排序实现自绘菜单体系，不引入新的 UI 框架或测试工程，也不把 SunnyUI 主题色与本项目 `ThemePalette` 混用。
- 不改动主窗口抽屉列表的拖动重排（那是抽屉顺序，与文件拖放无关），不新增退出路径。
- 不覆盖 `docs/TODOs.md` 的未提交内容，不清理现有 `bin/`、`obj/`、发布产物、日志与 `.vs/`。

## 下一阶段候补：抽屉文件拖放（阶段九草案）

保留本次核实的分析，供下一阶段直接使用：

- **现状**：`drawerContentsListView` 未开 `AllowDrop`，`DrawerWindow` 没有 `DragEnter`/`DragOver`/`DragDrop` 或 `DoDragDrop`；列表 `MultiSelect = false`、`View = LargeIcon`；条目元数据在 `DrawerItemEnumerator`，列表编排与视口延迟加载在 `DrawerWindow.FileList.cs`（带取消令牌与 generation，拖放改动必须复用而不是绕过）。
- **目标行为**：打开抽屉后可拖拽文件出入，同盘移动、异盘拷贝——由 Windows Shell 按系统默认执行，应用只转发拖放数据。
- **待确认项（本次未实施）**：
  - D5 是否支持跨抽屉（本应用 → 本应用）拖放：两端都不是资源管理器时没有任何一方会执行文件操作，要实现必须由应用自己调用 Shell 文件操作 API，这与 `AGENTS.md` 的“不移动/修改用户文件”约定冲突，需先确认改约定；
  - D6 是否允许一次拖出多个条目（当前单选取，改多选会影响选择/双击/主题绘制）；
  - D7 拖入/拖出后的刷新时机（建议：拖入后一次性延迟刷新 + 拖出仅在返回 `Move` 时刷新；不使用 `FileSystemWatcher`）。
- **实现要点（草案）**：拖入侧设 `AllowDrop`，`DragEnter`/`DragOver` 仅在 `DataFormats.FileDrop` 存在时声明 `Copy | Move`（不声明 `Link`），`DragDrop` 只记录并安排刷新；拖出侧按 `SystemInformation.DragSize` 阈值启动 `DoDragDrop`，`DataObject` 提供 `DataFormats.FileDrop`；刷新兜底使用阶段 8.4 已实现的右键菜单与 F5。

## 实施状态

本文件最初只包含阶段八计划；以下是已落地阶段的结论（D1、D1b、D1c、D2、D3、D4、D8、D9 已确认；D5–D7 的拖放工作推迟到阶段九）。

- **8.1 已实施（全局快捷键 + 启用选项）**
  - 新增 `src/Infrastructure/GlobalHotKey.cs`（`RegisterHotKey`/`UnregisterHotKey` 封装、`MOD_WIN|MOD_SHIFT|MOD_NOREPEAT`、错误码说明）；`AppConstants.HotKeys` 存放 id（`0x5744`）与显示名；新增 `src/Forms/MainWindow.HotKey.cs`（`OnHandleCreated` 注册、`OnHandleDestroyed`/`OnFormClosed` 释放、`WndProc` 处理 `WM_HOTKEY`、幂等的 `SyncHotkeyRegistration`，注册句柄单独保存以避免在句柄销毁后访问 `Handle` 重建窗口）。
  - 新增配置字段 `startup.toggle_hotkey_enabled`（Boolean，默认 `true`）：`SettingsKeys`、`SettingsSnapshot`（属性 + `Clone` + `FromModel` 回退 `true`）、`SettingsRepository.Update`/`GetEmbeddedStartupDefaults`、根目录模板 `Drawer-settings.toml` 与序列化段落说明均已同步。
  - 设置面板：Behavior 组新增第 4 项 `Enable Win+Shift+D`（索引 3），接入勾选状态、工作快照、预览事件（`ToggleHotkeyPreviewChanged`）、取消还原；`MainWindow` 的 `ApplyAcceptedSettings`/`RevertSettingsPreview`/`Settings_FormClosed`/Default 重置路径都会让注册状态与配置保持一致；注册失败时仅在面板显式打开该项时用 `UiMessages.ShowWarning` 提示一次，并保留用户选择（D1b）。
  - **已验证**：Debug 构建 0 警告 0 错误；`%TEMP%\windrawer-hotkey-harness` 临时 harness（链接真实源文件 + 嵌入真实模板）34 项通过——`toggle_hotkey_enabled` 缺失/真/假/字符串/整数的解析、模板默认 `true`、写盘内容与重载保持、Default 重置为 `true` 且保留 drawers/未知键并删除 `icon_size`、其它 startup 字段回归，以及真实 Win32 注册（成功、重复注册返回 1409、释放后可重新注册、零句柄拒绝）。
  - **真实运行证据（只读核对你在本机启动的 Debug 构建日志）**：`18:28:24.253 SyncHotkeyRegistration: Registered global shortcut Win+Shift+D.` —— `OnHandleCreated` 之后的注册在真实应用中成功；设置面板打开期间又出现两次同一注册日志（与“取消勾选 → 释放、重新勾选 → 注册”的预览路径一致）；`18:28:41` 的保存流程没有出现注册失败警告。
  - **仍未验证**：真实按键（前台为其它应用时按 Win+Shift+D）的显示/隐藏效果、被占用时的提示观感、句柄重建后的重注册、退出后组合键失效，以及 Behavior 组新增第 4 项的实际排版与高 DPI 显示——本文件不声称这些项已验收。
  - 端到端脚本 `%TEMP%\windrawer-hotkey-e2e.ps1`（启动 Debug 版应用 → 通过抽屉列表点击打开一个抽屉 → 投递 `WM_HOTKEY` 校验主窗口与抽屉同时隐藏/显示 → 用 `WM_CLOSE` 优雅退出）：执行时本机一直有 winDrawer 实例在运行（先是 `Z:\userScripts\winDrawer.exe`，之后是你启动的 `src\bin\Debug` 构建），为避免启动第二个实例而**未执行**；关闭正在运行的实例后可直接运行该脚本。

- **8.2 已实施（统一显示/隐藏语义）**
  - `src/Forms/MainWindow.Tray.cs`：`ToggleVisibility()` 改用 `IsMainWindowShown` 判定并分派到 `HideMainWindowAndDrawer()` / `ShowMainWindowAndDrawer()`；隐藏时先 `Hide()` 抽屉窗口再隐藏主窗口；显示时 `Show()` → `WindowState = Normal` → `PositionDrawerWindowNextToMainWindow()` → `drawerWindow.Show(this)` → `Activate()`。托盘 `Show/Hide` 菜单、托盘双击与 Win+Shift+D 共用这一条路径。
  - 顺带补齐 D2 的一致性缺口：托盘的 **Settings** 入口改用 `EnsureMainWindowAndDrawerShown()`（已显示时只 `Activate()`，隐藏状态则先恢复主窗口与抽屉），否则从隐藏状态打开设置会让抽屉窗口“落单”。
  - `src/Forms/MainWindow.Drawers.cs` 抽出 `PositionDrawerWindowNextToMainWindow()`（供“显示时复位”和“主窗口移动时跟随”共用）；`MainWindow.cs` 的 `MainWindow_LocationChanged` 改为只跟随可见抽屉，行为与改动前等价。
  - `src/Infrastructure/TrayController.cs` 菜单文案 `Show/Hide` → `Show/Hide window and drawer`；`README.md` 的托盘、快捷键与运行说明同步为“主窗口 + 已打开的抽屉”。
  - **已验证**：Debug（`-o %TEMP%\windrawer-build-82-debug`，因为运行中的实例锁定了 `src\bin\Debug`）与 Release 构建均 0 警告 0 错误；`%TEMP%\windrawer-border-probe` 探针确认 `Show() → Hide() → Show()` 只触发一次 `Shown`（因此重新显示不会重新枚举目录或重建缩略图，正是 8.2 验收的前提假设）。
  - **未验证**：真实 UI 下的组合隐藏/显示（主窗口与抽屉一起隐藏、重新显示后位置是否重新吸附在右侧、滚动位置/选中项/缩略图是否保持、快速反复切换是否产生额外窗口或任务栏按钮、`AlwaysOnTop` 开启时的表现、托盘 Settings 从隐藏状态恢复抽屉、抽屉已关闭时只切换主窗口）——这些需要手工回归，或在你关闭当前实例后运行上述端到端脚本。

- **8.3 已实施（收窄抽屉顶部可见边带）**
  - **实测基线（探针，本机 200% DPI）**：`Sizable` 去掉 `WS_CAPTION` 后，窗口矩形到客户区的非客户区内缩四边都是 13px，但 DWM 只在左/右/下把其中 11px 当作不可见缩放边框，于是可见带为“顶部 13px、其它三边 2px”，且顶部这 13px 由 DWM 用窗框色（本机系统强调色）绘制；`PrintWindow(PW_RENDERFULLCONTENT)` 复核确认这 13px 属于非客户区。
  - **实现**：新增 `src/Forms/DrawerWindow.Chrome.cs`——`WM_NCCALCSIZE` 把 `NCCALCSIZE_PARAMS.rgrc[0].top` 改回窗口矩形的顶边（客户区顶到可见上边缘），左/右/下保留系统计算值；`WM_NCHITTEST` 仅把顶部 8 逻辑像素内、系统判定为 `HTCLIENT` 的命中改判为 `HTTOP`/`HTTOPLEFT`/`HTTOPRIGHT`。`InitializeWindow` 与 `DpiChanged` 调用 `UpdateTopResizeGrip(dpi)` 设置该缩放条厚度。
  - **与计划的偏差（已记录）**：计划设想让窗体自身的 `WM_NCHITTEST` 直接覆盖顶部客户区，但客户区被 `drawerContentsListView` 覆盖，命中测试会先到子控件、窗体收不到该消息。因此改为在客户区顶部保留一段 8 逻辑像素的**不可见缩放条**（`Form.Padding.Top`），并把 `DrawerWindow.ApplyTheme` 里的窗体 `BackColor` 由 `windowBackground` 改为 `contentBackground`（客户区其余部分仍由 `mainTableLayoutPanel` 覆盖，因此只影响这条缩放条），使它与列表内容同色、肉眼无法分辨，同时顶部与两个上角仍可拖动缩放。
  - **已验证**：Debug（`-o %TEMP%\windrawer-build-83-debug`）与 Release 构建均 0 警告 0 错误；探针实测新行为客户区内缩为 `L=13 T=0 R=13 B=13`（改动前 `T=13`），窗口外框尺寸与改动前完全一致（788x609），`PrintWindow` 中顶部第 0 行起已是客户区像素（改动前顶部 13px 为非客户区），并且 WinForms 的 `ClientSize`（762x596）与真实客户区一致——不存在“`ClientSize` 与实际客户区相差一个顶部内缩”的漂移风险（尺寸持久化与 `OnShown` 使用的是窗口 `Size`，其值未变）。
  - **未验证**：真实应用中的观感与交互——顶部色带是否已完全不可见、缩放条光标是否为上边缘样式、从顶部/左上/右上拖动缩放是否生效、左/右/下边缘与 `MinimumSize` 是否照旧、贴靠/阴影/圆角是否正常、DPI 切换后缩放条厚度是否正确，以及“打开 → 缩放 → 关闭 → 重开”是否无尺寸漂移。本机为 200% DPI，100%/150% 未实测（非客户区度量随 DPI 等比缩放，本次只验证了几何变化本身）。

- **8.4 已实施（右键菜单 + 三种排序 + 移除刷新按钮）**
  - 新增 `src/Drawers/DrawerSortMode.cs`：`NewestFirst`/`OldestFirst`/`NameAscending` 与 TOML 文本映射（`newest`/`oldest`/`name`，缺失、大小写变体、未知取值或错误类型一律按 `newest`）。
  - `DrawerItemEnumerator.Enumerate(folderPath, sortMode, token)` 与 `CompareItems(left, right, sortMode)` 按模式分派：三种模式都保持目录与文件混排；时间模式先比时间（不可读的排最后）、相同再比名称 `OrdinalIgnoreCase`、最后比完整路径 `Ordinal`，名称模式直接比名称再比路径。比较器仍是纯逻辑、不做文件系统 I/O。
  - 持久化（D3）：`DrawerDefinition.SortMode` + `WithSortMode`；`SettingsKeys.DrawerSortMode`；`SettingsSnapshot.ParseDrawers` 读取该键；`SettingsRepository.UpdateDrawerSortMode` 完全照 `UpdateDrawerSize` 的模式（`_writeLock` + `PathComparison.AreSame` 定位 + 写字符串值 + 标脏 + 唤醒后台 writer + 日志）。新增抽屉不写该键（缺失即 `newest`），只有用户在菜单里明确切换才写入。
  - 新 `src/Forms/DrawerWindow.ContextMenu.cs`：`Refresh`（`ShortcutKeyDisplayString = "F5"`）、分隔符、`Sort by name`/`Newest first`/`Oldest first` 三项互斥勾选（菜单打开前按当前模式刷新勾选）、`ProcessCmdKey` 处理 F5、`SortModeChanged` 事件，并在窗体 Dispose 中释放菜单。
  - 新 `src/Controls/UiContextMenuTheming.cs`：按 `ThemePalette` 提供菜单配色表，并让文字与勾选标记在“面板底色”和“强调色选中态”下都保持可读（勾选标记由自定义渲染绘制）。
  - 移除右下角刷新按钮：`DrawerWindow.Designer.cs` 删除控件、初始化、提示与无障碍文本并释放上下文菜单；`DrawerWindow.InitializeWindow` 去掉点击订阅、`ApplyTheme` 去掉按钮主题调用并改为重染菜单；`DrawerWindow.FileList.cs` 把 `RefreshDrawerButton_Click` 改为 `RefreshDrawerAsync()`，`Enumerate` 调用改传 `_drawer.SortMode`；`ApplyDrawerDefinition` 在模式变化时重新排序。
  - `MainWindow.OpenDrawer` 订阅 `drawerWindow.SortModeChanged` → 走现有 `ReloadSettingsFromStorage()`，保证重新打开或编辑抽屉时使用最新模式。
  - **已验证**：Debug（`-o %TEMP%\windrawer-build-84-debug`）与 Release 构建均 0 警告 0 错误；`%TEMP%\windrawer-hotkey-harness` 扩展后 **65/65 通过**，新增覆盖：三种模式的真实目录排序（混排、方向、同名按名称 tie-break）、时间不可读在时间模式排最后而在名称模式按名称参与、同名不同路径按路径 `Ordinal`、`sort_mode` 缺失/大小写/未知/错误类型的解析、`UpdateDrawerSortMode` 的写盘内容与其它抽屉字段保留、Default 重置不改动 `sort_mode`、新增抽屉不写该键。
  - **未验证**：真实 UI 中的菜单观感（深浅主题与抽屉专属蒙版下勾选/悬停对比度、菜单边框与分隔线）、列表任意位置右键是否都能弹出、切换后是否立即重排、F5 是否刷新、`sort_mode` 重启后是否恢复、切换后 `ReloadSettingsFromStorage()` 是否引起抽屉列表闪烁或选中态异常，以及移除刷新按钮后底部按钮行的布局。
