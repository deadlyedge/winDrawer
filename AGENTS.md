# winDrawer — AI 开发约定

## 项目目标

winDrawer 是一个 Windows 桌面小工具：维护一组常用文件夹，在程序内显示所选文件夹的直接内容；用户可预览常见图片类型，双击条目后将其交给 Windows Shell 按系统默认行为打开。首要目标是简单、可靠、容易继续维护，而不是复刻完整资源管理器。保留主题设置页面和预览，但简化控件实现。

当前实现与开发约束以 [`docs/WHITEPAPER.md`](docs/WHITEPAPER.md)、[`docs/CONFIGURATION_FORMAT.md`](docs/CONFIGURATION_FORMAT.md)、根 [`README.md`](README.md) 和本文件为准。阶段 0–10 的计划、审查与迁移记录归档在 [`docs/development-history/`](docs/development-history/README.md)，仅作历史参考；若历史文档中的旧要求（例如 HSLA 控件）与当前实现或用户最新明确要求冲突，以当前实现和用户最新要求为准，并先同步更新当前文档。

## 开始工作前：保护现有工作

- 首先检查 `git status --short`，并阅读打算修改文件的现有内容和 diff。
- 工作树中任何已有的源码、配置、解决方案和生成文件改动都必须视为用户工作。
- **不得**使用 `git reset`、`git checkout --`、`git clean`、批量还原或其他会丢弃现有改动的操作。
- 不要用整文件重写覆盖含有用户改动的文件，除非确有必要且已先保留其中有效内容。
- `.vs/` 中有已纳入 Git 的文件，必须保留其本地工作。根目录 `bin/`、`obj/` 是忽略的构建产物目录；仅在用户明确要求清理时审阅并清理，避免误删用户配置、日志或仍被运行进程使用的文件。
- 所有git命令不要分页显示以避免打断ai编码进程。

## 技术与架构约定

- 这是 Windows-only 的桌面项目；优先沿用单个 WinForms 应用项目和 BCL/WinForms 原生控件，不要仅为“现代架构”引入多项目分层、复杂 MVVM、依赖注入框架或微服务式结构。
- SunnyUI 已完全移除；不要再引入。当前 UI 使用原生控件和项目内轻量控件：`DrawerListBox`、`ThemedScrollBar`、`ScrollableListView`、`ColorPreviewPanel`、`BehaviorCheckBoxGroup`。按钮颜色统一走 `UiButtonTheming`；主操作使用 `ButtonFontSize`（10F），其他按钮使用 `CompactButtonFontSize`（9.5F）。主窗口标题行的“添加抽屉/设置”是不带文字的图标按钮，字形统一由 `IconGlyphs` 提供（Segoe MDL2 Assets，缺字体时退回 Segoe UI Symbol），不要在窗体或设计器里硬编码私有区字符；主窗口宽度是固定常量，不提供设置项；这两个按钮的 ToolTip 必须设置 `ShowAlways = true`（主窗口经常不是前台窗口，否则不会弹出）。设置面板统一通过 `MainWindow.OpenSettingsPanel()` 打开；标题文字右键也走该入口。`window_locked` 为真时隐藏这两个按钮并让标题文字跨三列，但设置入口（托盘菜单/标题右键）必须保持可用。
- 使用实施时 Microsoft 官方最新的 GA LTS .NET Windows 版本；当前计划目标是 `net10.0-windows`。实施前核实官方支持周期和 SDK，安装最新 servicing patch；不使用 preview 或 STS。
- 文件列表优先使用 WinForms `ListView` 的图标视图和 `ImageList`，让系统控件负责排列、选择和滚动。常见图片使用 Explorer 风格的图像缩略图作为列表图标；不另开图片预览窗格。主题设置页只保留颜色预览和独立的透明度滑块；点击颜色预览打开 Windows/WinForms 系统颜色控件（优先 `ColorDialog`）。不要保留 H/S/L 或 HSLA 调色控件，也不要重新实现每个文件一套自绘卡片布局。
- 抽屉内容区的原生 `ListView` 垂直滚动条用于滚轮/`GetScrollInfo`，但必须通过加宽 ListView 裁剪到宿主外侧，并由 `ThemedScrollBar` 覆盖可见区域；不要恢复可见的系统滚动条，也不要改回移除 `WS_VSCROLL` 后手动处理滚轮的旧方案。
- 文件和文件夹的 Shell 操作必须委托给 Windows：使用合适的 Shell 执行方式（例如 `ProcessStartInfo.UseShellExecute = true`），不要自行解析关联程序或拼接命令行。
- 当前代码已有 `Theming/ShellIcon.cs`、`Drawers/` 的直接子项枚举与排序、`Forms/DrawerWindow.cs` 与 `Forms/DrawerWindow.FileList.cs` 的列表/图像管线、`Forms/MainWindow.cs` 的抽屉列表与托盘协调，以及 `Configuration/SettingsRepository.*` 的 TOML 处理。优先精简、复用并添加测试；避免未经用户确认就换 UI 框架、配置格式或重写整个应用。
- 主题设置页仅显示当前颜色预览与独立透明度滑块（百分比 40–100，最小值 40 由 `AppConstants.Theme.OpacityMin` 决定）；点击颜色预览打开系统颜色对话框。颜色统一以 `#RRGGBB` 保存到 `startup.window_color`，透明度单独保存到 `startup.window_opacity`；不再持久化 HSLA。抽屉专属颜色沿用 `#RRGGBB`，蒙版强度仍固定为 70%。颜色选择、透明度和实时预览须遵循保存/取消语义。
- 程序尚未发布，不实现 `window_color_hsla` 运行时迁移。切换配置标准时，直接将开发者当前用户目录中的设置手动改为新字段，保留其颜色、透明度和其它设置；不得读取、转换或迁移旧 HSLA 字段。
- 保留常见图片的 Explorer 风格列表图标预览，至少支持 JPG/JPEG 和 PNG。图片缩略图显示在 `ListView` 图标视图中，不另开预览窗格；非图片项目使用 Windows Shell 图标。缩略图失败时回退为 Shell 图标，不得影响双击打开。
- 使用 Windows Shell 图标及合适的系统图标尺寸；需要处理 DPI，但不要把图标大小暴露为设置项。
- 保持改动直接、易读。类和方法只在职责确实独立时拆分；为小功能添加新抽象前，说明它解决的具体问题。

## 产品行为约定

- 抽屉显示所选目录的**直接子项**；不在抽屉内递归扫描目录。
- 抽屉在 `ListView` 图标视图中显示直接子项；JPG/JPEG/PNG 等常见图片显示缩略图，其他文件使用 Windows Shell 图标。单击只选择条目；双击将其交给 Windows Shell 执行默认 `open` 行为。目录通常在资源管理器中打开；文件由 Windows 默认关联程序打开，和在资源管理器中双击的行为一致。
- 不在抽屉内实现子目录导航或任意文件类型预览。若需求变更为“在资源管理器中选中某文件”而不是打开它，先向用户确认，再实现相应 Shell 行为。
- 应用自身不主动删除、移动、重命名或修改用户文件；唯一例外是用户在抽屉列表上**显式拖放**、且完全由 Windows Shell 执行的移动/拷贝（拖出由资源管理器执行，拖入经 `IFileOperation` 执行）。另一项受限例外是首次启动：活动设置文件不存在时，在 `%USERPROFILE%\.config\winDrawer\demoDrawer` 下创建内嵌示例文件，只补缺失文件、绝不覆盖，失败只记录日志。不实现删除、重命名、新建文件夹、复制/剪切/粘贴，也不自建文件传输、进度或撤销 UI。
- 对不存在的目录、拒绝访问和 Shell 调用失败给出可理解的提示；错误不能导致整个程序退出。

## 代码与验证

- C# 类型、方法和变量沿用 .NET 命名约定；新增说明文档使用中文。代码注释只解释必要的非显然行为。
- 每次改动后尽可能在 Windows/最新 LTS .NET SDK 环境构建项目；有配置解析、路径比较、颜色十六进制解析/格式化、主题颜色迁移或混色等纯逻辑时，优先增加小而有针对性的自动化测试，不引入 UI 自动化测试框架作为前置条件。
- 检查 Nullable 警告、资源释放（特别是 `Icon`、图片缩略图 `Image`、`ImageList` 和窗体事件）及高 DPI 行为。缩略图生成不得锁住源文件，图片和图标资源在列表刷新或窗体关闭时应正确释放。
- 完成后汇报：改了哪些文件、采用了哪些行为、执行了哪些构建/测试、尚未验证什么。不得声称运行过未实际执行的测试。
- 构建验证必须走完整构建流程；**不要**用 `dotnet build -t:Compile` 之类只跑编译器的目标来绕过被占用的输出目录：它跳过资源生成，并把缺少 `.resx`/嵌入资源的 DLL 写入 `obj`，随后增量构建会把它拷到 `bin`（表现：设置窗体抛 `MissingManifestResourceException`、嵌入式字体回退为 Segoe UI）。若 `bin\<配置>` 被运行中的实例锁定，先请用户退出应用，或改用 `dotnet build -c <配置> -o <临时目录>` 验证（该方式会正常生成资源，且不触碰被锁的输出）。
- 临时/外部探针不要跨进程发送带指针的 `LVM_*` 消息（例如 `LVM_GETITEMRECT`、`LVM_GETITEMPOSITION`）：`lParam` 是调用方进程的地址，目标进程写入时会破坏自身内存并直接崩溃（表现为 `coreclr.dll` 的 `CallWindowProc` 访问冲突）。需要条目几何时改用截图/像素采样，或在同一进程内验证。
- 退出与关闭必须走单一路径：所有退出请求（托盘菜单、设置面板）都调用 `MainWindow.ShutdownApplication(reason)`；不要在窗体或分部文件里直接调用 `Application.Exit()`、`Environment.Exit()` 或 `Process.Kill()`（仅 `ForceProcessExit()` 作为最后兜底允许）。`SettingsRepository.Dispose()` 必须在 `MainWindow.OnFormClosed` 中调用（顺序：字体 → 托盘 → 仓储）。注意 `Application.Exit()` 会以 `CloseReason.ApplicationExitCall` 触发 `FormClosing/FormClosed`，`e.Cancel` 被尊重；拥有窗体在关闭时会二次触发 `FormClosed`，且**不得**在拥有窗体的 `FormClosed` 处理器里调用属主窗口的 `Close()`（会递归/栈溢出）。
- 日志统一用 `DrawerLogger`：默认最低级别 `Information`（挂调试器时为 `Debug`），可用 `WINDRAWER_LOG_LEVEL` 覆盖；活动日志超过 `AppConstants.Logging.MaxLogFileBytes`（1 MB）轮转为 `Drawer.log.1`。不要在逐项/逐帧的热路径里写 `Debug` 日志，也不要在日志内部再调用日志方法（用 `Debug.WriteLine`）。
- 共用工具只保留一份，新增同类逻辑前先复用：用户提示走 `UiMessages`（不要直接 `MessageBox.Show`）、按钮配色走 `UiButtonTheming`、颜色对话框预设与 Enter/Space 激活走 `UiDialogHelpers`、路径相等判断走 `PathComparison`、TOML 键名走 `SettingsKeys`（不要在序列化/解析里写字符串字面量）、深浅文字判断走 `Theming/TextContrast.cs`（阈值是 `AppConstants.Theme.TextLuminanceThreshold`，只通过改常量并重新构建来调试）。
- 过渡动画统一走 `Infrastructure/WindowAnimation.cs`（`AnimationClock`、`WindowFade`、`WindowSlide`），时长/偏移/起始透明度常量放在 `AppConstants.Ui.Animation`，帧间隔来自 `startup.animation_frame_ms`（0 关闭全部过渡）。不要在各窗体重复实现 Timer 动画，也不要在逐帧回调里写日志。动画结束时必须把 `Opacity` 还原为配置的透明度，并且系统关闭"动画效果"（`SPI_GETCLIENTAREAANIMATION`）时直接落到终态。
- 抽屉内容列表的悬停高亮必须走原生条目绘制：在 `ScrollableListView` 里拦截反射的 `NM_CUSTOMDRAW` 并设置 `clrTextBk`。不要改回 owner-draw（`DrawDefault` 会擦掉自绘背景、完全自绘又要复刻图标/标签布局）或 `WM_PAINT` 之后叠加绘制（会闪烁并残留旧高亮）。收到 `WM_MOUSELEAVE` 时不要直接清高亮：条目 tooltip 会盖住指针并触发该消息，必须用 `Cursor.Position` 判断指针是否真的离开客户区。
