# winDrawer 白皮书

> 本文描述当前实现（SunnyUI 移除后），用于快速理解代码边界、数据流和必须保持的行为不变量。
>
> 这不是需求文档；具体配置字段见 [`CONFIGURATION_FORMAT.md`](CONFIGURATION_FORMAT.md)，历史阶段计划见 [`development-history/DP_STAGE_10.md`](development-history/DP_STAGE_10.md)，UI 栈迁移记录见 [`development-history/SUNNYUI_REMOVAL_IMPACT.md`](development-history/SUNNYUI_REMOVAL_IMPACT.md)。

## 1. 产品定位

winDrawer 是一个 Windows 桌面小工具：维护一组常用文件夹，选中后在独立抽屉窗口中显示该文件夹的**直接子项**。单击只选择，双击把条目交给 Windows Shell 执行默认 `open` 行为。它不尝试复刻资源管理器，也不在抽屉内递归或导航。

明确的非目标：

- 不实现文件删除、重命名、新建、复制/剪切/粘贴；
- 不实现递归扫描、实时文件监控、搜索或子目录导航；
- 不自建图片预览窗格、缩略图缓存服务或虚拟列表框架；
- 不引入多项目分层、DI、MVVM 或新的 UI 框架。

唯一会修改用户磁盘内容的行为，是用户在抽屉列表上显式拖放，且实际传输完全由 Windows Shell 执行。

## 2. 运行模型

- 单个 `net10.0-windows` WinForms 进程，STA UI 线程；`Program.Main` 调用 `Application.Run(MainWindow)`。
- UI 只依赖 WinForms 与项目内轻量控件，不再引用 SunnyUI；当前自有控件为 `DrawerListBox`、`ThemedScrollBar`、`ScrollableListView`、`ColorPreviewPanel`、`BehaviorCheckBoxGroup` 和 `EllipsisLabel`。
- 主窗口是应用主窗体；正常运行时主窗口、抽屉窗口和设置窗口都不创建任务栏按钮，托盘是常驻入口。
- `MainWindow` 同一时间只维护一个打开中的抽屉窗口；切换抽屉会关闭旧窗口。
- 托盘菜单和设置面板的退出入口最终都走 `MainWindow.ShutdownApplication(reason)`，再通过 `Application.Exit()` 走常规 `FormClosing/FormClosed` 路径。
- `MainWindow.OnFormClosed` 的释放顺序为：热键、品牌字体、托盘控制器、`SettingsRepository`；`ForceProcessExit()` 只在常规退出抛异常时作为最后兜底。
- 托盘菜单继续由 `NotifyIcon.ContextMenuStrip` 拥有，避免弹出菜单出现在任务栏。WinForms 的 `NotifyIcon` 内部会走 `ContextMenuStrip.ShowInTaskbar`，在多显示器/跨屏任务栏/混合 DPI 下可能选错屏幕；`TrayController` 在 `Opening` 中按鼠标所在屏幕的工作区计算位置，并在 `Opened` 中使用 `SetWindowPos` 做最终纠正。

## 3. 模块边界

| 位置 | 职责 |
|---|---|
| `Program.cs` | 启动、全局异常钩子、单次错误提示 |
| `Forms/MainWindow.*` | 主窗口生命周期、抽屉列表、拖动重排、设置协调、托盘/热键、退出 |
| `Forms/DrawerWindow.*` | 抽屉窗体、无标题栏边框、文件列表编排、右键排序、拖放、主题 |
| `Forms/SettingsForm.*` | 主设置面板：行为、外观、透明度、高度、默认值 |
| `Forms/DrawerSettingsDialog.cs` | 单个抽屉的名称、路径、专属颜色、删除 |
| `Drawers/` | 直接子项枚举/排序/上限、条目元数据、Shell 图标与缩略图、拖放规划 |
| `Configuration/` | TOML 模型、快照解析、抽屉增删改、后台写盘、原子写入 |
| `Controls/` | `DrawerListBox`、`ThemedScrollBar`、`ScrollableListView`、`ColorPreviewPanel`、`BehaviorCheckBoxGroup`、省略号标签、`IconGlyphs` 图标字形、按钮/菜单主题、颜色对话框 helper、消息框 |
| `Infrastructure/` | 日志、托盘、登录启动、路径比较、全局热键、`IFileOperation`、窗口过渡动画 |
| `Theming/` | `#RRGGBB` 解析、主题派生色、品牌字体、Shell 图标 |

依赖方向应保持为：窗体 → 业务/配置/基础设施；`Drawers/` 与 `Configuration/` 不反向依赖具体窗体。不要为了形式上的分层新增接口或工厂。

## 4. 配置与持久化

- 活动配置：`%USERPROFILE%\.config\winDrawer\Drawer-settings.toml`；日志同目录 `Drawer.log`。
- 首次启动时，如果活动配置不存在，从程序集内嵌的根目录 `Drawer-settings.toml` 初始化；已存在的配置绝不被模板覆盖。
- TOML 解析先验证类型，再生成 `SettingsSnapshot`；缺失字段使用默认值，未知字段在写回时保留。
- 写入由 `SettingsRepository` 的后台 writer 合并/防抖；退出前通过 `SavePendingChanges`/`Dispose` flush。写入使用同目录临时文件再原子替换，失败不覆盖旧文件。
- **Default** 按钮只重置 `[startup]`，保留 `drawers` 和未知键，并同步当前用户的登录启动状态；Save/Cancel 管理 Default 之后的草稿。
- `startup.max_drawer_items`（Integer，默认 200，范围 1–10000）控制单个抽屉的最大条目数；不提供设置页面，直接编辑 TOML，下次打开或刷新抽屉时生效。
- 抽屉字段：`name`、`path`、可选 `size`、`overlay_color`、`sort_mode`。抽屉顺序即 `[[drawers]]` 顺序。

## 5. 抽屉内容管线

1. `DrawerWindow` 显示后异步调用 `PopulateFileListAsync`，先显示“Loading...”状态。
2. `DrawerItemEnumerator.Enumerate` 只枚举直接子项，读取名称、是否为目录、最后修改时间；使用 `DrawerEnumerationResult` 区分：
   - `Success`：已按当前 `sort_mode` 排序；
   - `FolderMissing`：显示缺失状态；
   - `TooManyItems`：在发现第 `limit + 1` 项时立即停止，不读取其元数据、不排序。
3. 超过 `max_drawer_items` 时不创建任何文件条目，只显示“Too many items...”状态；底栏路径仍可打开资源管理器。
4. 未超限时，`ListView` 使用 `LargeIcon` 和 `ImageList`；目录与文件混排，排序由 `DrawerSortMode` 决定。
5. 图标/缩略图按可视区加一屏预取范围延迟加载；滚动防抖，离开预取范围释放图像。加载批次用 generation + `CancellationTokenSource` 丢弃过期结果，避免旧目录/旧视口覆盖新状态。
6. 图片缩略图失败时回退为 Shell 图标，不影响列表和双击打开；生成过程不得锁住源文件。
7. 双击条目通过 `ProcessStartInfo { UseShellExecute = true }` 交给 Windows Shell。

## 6. 窗口、DPI 与主题

- 主窗口无系统边框，支持拖动；`window_locked = true` 时禁止拖动并隐藏移动光标。位置保存到 `window_position`，高度以 96 DPI 逻辑像素保存到 `window_height`，运行时按当前 DPI 换算并夹取到所在屏幕工作区。
- 主窗口宽度是固定常量 `AppConstants.Defaults.MainWindowWidth`（96 DPI 逻辑像素，当前 150），不写入配置，也不提供设置项。标题行左侧是 `winDrawer` 品牌文字（`LogoFontSize`，11pt），右侧依次是“添加抽屉”和“设置”两个图标按钮：按钮只显示字形（`IconGlyphs`，Segoe MDL2 Assets 的 `E710`/`E713`，缺少该字体时退回 Segoe UI Symbol 的 `+`/`⚙`），用途由 ToolTip 说明。抽屉列表占满标题行以下的全部空间，底部没有独立的添加区域。
- 抽屉窗口通过移除 `WS_CAPTION` 隐藏标题栏，保留 `FormBorderStyle.Sizable` 的边缘缩放；底部提供文件夹路径入口、刷新和关闭按钮。窗口尺寸在调整结束/关闭时写回 `drawers[].size`。
- 设置窗体与抽屉设置对话框每次打开时按主窗口位置偏移 `(100, 100)` 并夹取到主窗口所在屏幕工作区；它们的位置不持久化。
- 主题颜色以 `#RRGGBB` 保存，透明度单独保存为 `startup.window_opacity`（0–100）。`ThemePalette` 由基础颜色派生背景、卡片、强调色和可读文字色；按钮/菜单/列表共用这些派生色。
- 抽屉专属颜色以 `overlay_color` 保存，固定以 70% 不透明度混合到抽屉背景，不提供单独 Alpha。未设置时不叠加。
- 按钮使用原生 `Button` 的 `FlatStyle.Flat`：`Cancel`/`Save` 使用 `ButtonFontSize`（10F），其他按钮使用 `CompactButtonFontSize`（9.5F）。不再使用 SunnyUI 的圆角、`TipsFont` 或独立状态前景色。
- 设置窗体和抽屉设置对话框使用原生 `Form`/`GroupBox`/`TrackBar`/`CheckBox`/`Label`；颜色预览由 `ColorPreviewPanel` 自绘，行为选项由 `BehaviorCheckBoxGroup` 提供两列布局。
- 抽屉内容区保留原生 `ListView` 垂直滚动条用于 `GetScrollInfo`、滚轮和原生滚动平滑度，但把 `ListView` 加宽到宿主客户区之外，让原生条被父面板裁掉；`ThemedScrollBar` 只在可见侧绘制约 3px 轨道，并覆盖原生条的约 18px 区域。该裁剪关系是刻意的，不要恢复可见的系统滚动条。
- 过渡动画统一由 `Infrastructure/WindowAnimation.cs` 提供：`AnimationClock` 用 `Stopwatch` 计时并驱动单个 15ms `System.Windows.Forms.Timer`，`WindowFade` 以"主题透明度为基准值"做淡入，`WindowSlide` 只做视觉位移且终点始终由调用方（`MainWindow` 的抽屉定位）给出。当前效果：抽屉打开时从主窗口右边缘滑出并淡入（160ms）、从隐藏恢复和启动时淡入（120ms）、滚动条悬停或拖动时轨道加宽（3→6 逻辑像素，120ms）、抽屉列表行悬停高亮渐变（160ms）。Windows 关闭"动画效果"（`SPI_GETCLIENTAREAANIMATION`）时，所有过渡直接落到终态。
- 主窗口和设置页使用 WinForms 自动缩放；抽屉窗口使用 `AutoScaleMode.None`，在 `DpiChanged` 时显式重算图标尺寸、底栏布局、滚动条和图像列表。修改这些尺寸时必须同时验证 100%/150%/200% DPI。

## 7. 拖放

- 拖出：从 `ListView` 选中项收集路径，使用 `CF_HDROP` 交给资源管理器；用户可拖到资源管理器、桌面或其它文件夹。
- 拖入：接受文件/文件夹拖放，使用 `IFileOperation` 执行；同盘移动、异盘拷贝，Ctrl 强制拷贝、Shift 强制移动。重名冲突、进度和撤销信息由 Windows Shell 提供。
- 应用不自行实现文件传输、进度条、撤销或拖放视觉层；拖放完成后刷新抽屉，使排序/缩略图与磁盘一致。
- 拖放是“显式用户操作 + Shell 执行”的唯一文件修改例外；不得扩展到删除、重命名、新建或粘贴。

## 8. 错误处理与日志

- 用户提示统一走 `UiMessages`；不要在窗体里直接调用 `MessageBox.Show`。
- 预期错误（目录缺失、拒绝访问、Shell 打开失败、配置保存失败）应显示可理解提示并保持应用运行；单个条目读取失败可跳过并记录 Debug 日志。
- 全局 UI 线程异常由 `Program` 记录并只显示一次带日志路径的提示；`AppDomain.UnhandledException` 只记录。
- 日志统一走 `DrawerLogger`：默认 Information（挂调试器时 Debug），可由 `WINDRAWER_LOG_LEVEL` 覆盖；超过 1 MB 轮转为 `Drawer.log.1`。不要在逐项/逐帧热路径写 Debug，也不要在日志内部调用日志方法。

## 9. 必须保持的不变量

1. 抽屉只显示**直接子项**，不递归。
2. 单击只选择；双击通过 Windows Shell 打开，不自行解析关联程序。
3. 除用户显式拖放且由 Shell 执行外，不删除、移动、重命名或修改用户文件。
4. 超过 `max_drawer_items` 时不显示部分条目，只显示状态；底栏路径仍可打开资源管理器。
5. 缩略图生成不得锁住源文件；刷新、关闭、DPI 变化后图像/图标/`ImageList` 必须正确释放。
6. 过期枚举/图像结果不得覆盖新结果；控件访问必须回到 UI 线程。
7. 退出请求统一走 `ShutdownApplication`；不在其它位置直接 `Application.Exit()`、`Environment.Exit()` 或 `Process.Kill()`。
8. 已存在的用户配置不被模板覆盖；保存失败不静默丢失旧文件；未知键尽可能保留。
9. 主题颜色只接受 `#RRGGBB`，透明度单独保存；不恢复 HSLA 字段。
10. 共用逻辑只保留一份：用户提示 `UiMessages`、按钮主题 `UiButtonTheming`、颜色对话框 `UiDialogHelpers`、路径比较 `PathComparison`、TOML 键名 `SettingsKeys`。
11. 过渡动画必须可取消且不改变配置：结束时 `Opacity` 必须精确回到 `ThemePalette.WindowOpacity`，位置必须回到 `MainWindow` 计算的位置；动画不得阻塞隐藏、关闭或退出，逐帧回调里不写日志。

## 10. 已知取舍与技术债

- **托盘菜单位置**：WinForms `NotifyIcon` 的内部 `ShowInTaskbar` 路径在多显示器下不可靠；当前保留系统菜单所有权，并用 `Opening`/`Opened` 纠正位置，仍需在特殊任务栏布局中人工验证。
- **文件列表**：`DrawerWindow.FileList.cs` 约 390 行，负责列表编排、视口图像请求和 ImageList 绑定；`DrawerItemImageCache` 独立管理占位图、图像所有权、取消和释放。拆分后仍必须保持现有生命周期语义。
- **配置写入**：后台 writer 的状态机偏重，但承担防抖、退出 flush、失败保留和原子写入；不建议为了行数牺牲可靠性。
- **主题**：`ThemePalette` 使用直接 RGB 混色和按亮度灰去饱和派生颜色，不再做 HSL 往返；修改派生系数会改变实际配色，需接受视觉变化。
- **抽屉列表**：主窗口使用 owner-draw 和拖动重排，已拆为 `MainWindow.DrawerList.cs`、`MainWindow.DrawerList.Paint.cs`、`MainWindow.DrawerReorder.cs`；没有 UI 自动化测试。
- **滚动条**：`DrawerWindow.FileList.PositionDrawerScrollBar` 通过加宽 ListView 裁剪原生垂直条，`ThemedScrollBar` 负责可见轨道和拖动。这个组合依赖原生滚动信息、图标视图和 DPI，布局或滚动逻辑改动后必须做 100%/150%/200% 手工回归。
- **测试**：仓库没有测试工程；纯逻辑使用 `%TEMP%` 下的临时 harness 验证，UI 行为依赖人工回归。
- 更细的代码审查项和历史收益记录见 [`development-history/CODE_REVIEW_STAGE_10.md`](development-history/CODE_REVIEW_STAGE_10.md)。

## 11. 修改指南

- 新增配置字段：先更新 `SettingsKeys`、`SettingsSnapshot`、`SettingsRepository.Update`、根目录模板、`CONFIGURATION_FORMAT.md`，再决定是否需要设置页控件；保存其他设置时必须保留未在设置页暴露的字段。
- 新增抽屉行为：优先改 `Drawers/` 中的纯逻辑，再接到 `DrawerWindow`；保持枚举/排序/上限结果可区分，不要在窗体里复制文件系统逻辑。
- 修改主题：同时检查主窗口、抽屉、设置窗体和右键菜单的普通/悬停/按下/禁用状态，以及深色/浅色背景对比度。
- 修改窗口布局：验证 100%/150%/200% DPI、最小尺寸、工作区夹取、无标题栏缩放和退出时的资源释放。
- 修改动画：时长、偏移和起始透明度常量在 `AppConstants.Ui.Animation`，时钟与缓动在 `Infrastructure/WindowAnimation.cs`；新增过渡优先复用它，不要在窗体里各自实现 Timer 动画，也不要在帧回调里写日志。
- UI 控件：优先复用项目内 `DrawerListBox`、`ThemedScrollBar`、`ColorPreviewPanel` 和 `BehaviorCheckBoxGroup`；不要再引入 SunnyUI 或为单个小效果新增大型 UI 框架。按钮颜色统一走 `UiButtonTheming`；主窗口标题行的图标字形统一走 `IconGlyphs`，不要在窗体或设计器里硬编码私有区字符。
- 每次改动后执行完整 `dotnet build .\src\winDrawer.csproj`（Debug/Release），不要使用 `-t:Compile` 绕过资源生成；构建产物被占用时改用临时输出目录。

