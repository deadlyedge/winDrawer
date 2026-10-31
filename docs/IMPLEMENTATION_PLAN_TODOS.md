# TODO 实施计划（2026-03）

本文件记录 `TODO.md` 中四项待办的实施范围、设计决定与验证方式。完成后转为历史记录。

## 1. 范围

| 项 | 目标 |
|---|---|
| A. 颜色对比辅助 | 提取统一的“浅底用深字、深底用浅字”逻辑，阈值集中在常量中；主窗口标题文字跟随该规则 |
| B. 首次运行示例 | 初次创建设置文件时，配置两个默认抽屉，并把内置演示目录写入用户配置目录 |
| C. 设置页链接 | 在设置窗口加入指向项目仓库的链接 |
| D. 锁定窗口 | 将 `Lock position` 改为 `Lock window`；锁定时隐藏主窗口的添加/设置按钮；标题栏右键可打开设置 |

不在本轮范围：`TODO.md` 的 thoughts 部分（`default_open_with`、设置按钮开关、主窗口文件拖放添加抽屉）。

## 2. 设计决定

- **对比度阈值**：只保留 `AppConstants.Theme.TextLuminanceThreshold`。调试时修改常量并重新构建，不增加环境变量或 TOML 字段。
- **演示内容打包**：使用 `EmbeddedResource`，逻辑名前缀 `winDrawer.Demo.`；运行时按资源名递归还原目录结构，避免单文件发布依赖外部 assets 目录。
- **示例目录不含 `drawer-settings.toml` 副本**：避免用户误认为它是活动配置，也避免与根模板出现两份需要同步的内容。
- **锁定后的设置入口**：托盘菜单或标题栏 logo 右键；因此锁定可无条件隐藏两个标题按钮，不需要托盘可用性回退。
- **`window_locked` 键名不变**，只调整语义与界面文案，避免无必要的配置迁移。

## 3. 实施步骤

### A. 文字对比度

1. `AppConstants.Theme` 增加 `TextLuminanceThreshold`、`LightTextColor`、`DarkTextColor`。
2. 新增 `src/Theming/TextContrast.cs`：`For(Color)` 与 `RelativeLuminance(Color)`。
3. `ThemePalette` 删除 `GetReadableTextColor`/`GetRelativeLuminance`，改用 `TextContrast`；新增 `ContentTextPrimary`（按内容区背景判断）。
4. 更新调用点：`MainWindow`、`DrawerWindow`、`DrawerSettingsDialog`、`SettingsForm`、`UiContextMenuTheming`。
5. 主窗口列表使用 `ContentTextPrimary`；标题 logo 使用 `TextContrast.For(theme.WindowBackground)`。

### B. 首次运行示例

1. 新增 `assets/demoDrawer/`：`可将文件拖入抽屉.txt`、`可预览图片（程序logo）.png`、`双击打开文件夹/说明.txt`。
2. `winDrawer.csproj` 增加带 `LogicalName` 前缀的 `EmbeddedResource` 项。
3. 新增 `src/Configuration/DemoDrawerInitializer.cs`：`FileMode.CreateNew` 只补缺失文件，逐项容错。
4. `SettingsRepository` 暴露 `WasInitializedFromDefaults`；`MainWindow` 启动时据此调用一次初始化器。
5. 根模板 `Drawer-settings.toml` 增加两条默认抽屉：配置目录与 `demoDrawer`。
6. 先验证嵌入资源名（构建后反射枚举），再接入初始化器。

### C. 设置页链接

1. `AppConstants.Branding.RepositoryUrl` 保存仓库地址。
2. `SettingsForm.Designer.cs` 在标题行右侧列加入 `LinkLabel`，固定亮蓝色。
3. 点击用 `ProcessStartInfo { UseShellExecute = true }` 交给 Shell；失败记录日志并提示。

### D. 锁定窗口

1. `BehaviorCheckBoxGroup` 文案改为 `Lock window`；设置页辅助描述同步。
2. `MainWindow` 新增 `ApplyWindowLocked(bool)`：隐藏按钮并让标题文字跨三列，避免缓存受 DPI 影响的列宽；恢复时取消跨列。
3. `HeaderDragBegin` 增加右键分支；`OpenSettingsPanel()` 作为按钮、托盘、右键的共用入口。
4. `headerToolTip` 随锁定状态更新标题提示，并补 logo 的可访问描述。

## 4. 文档更新

- `CONFIGURATION_FORMAT.md`：`window_locked` 新语义；模板新增默认抽屉；首次运行演示目录的创建规则。
- `WHITEPAPER.md`：运行模型中的首次初始化、标题栏右键、内容区文字色来源。
- `README.md`：锁定行为与设置入口。
- `AGENTS.md`：首次运行创建演示文件的受限例外；主窗口右键入口与共用方法。
- `docs/INDEX.md`：登记本文件。

## 5. 验证

1. `dotnet build .\src\winDrawer.csproj`（Debug/Release）。
2. 临时控制台程序加载 `winDrawer.dll`，打印含 `winDrawer.Demo.` 的资源名，确认中文与子目录分隔符。
3. `%TEMP%` 临时目录验证 `TextContrast` 阈值边界与 `DemoDrawerInitializer` 的首次创建、重复调用、只读文件降级。
4. 手工回归：深浅主题文字、锁定预览/取消/保存、标题栏右键、GitHub 链接、100%/150%/200% DPI。
5. 首次运行路径需要临时改名用户配置；执行前备份并在验证后恢复。

## 6. 实施结果（2026-03）

已完成：

- `Theming/TextContrast.cs` 与 `ThemePalette.ContentTextPrimary`；所有调用点已切换。阈值固定为 `AppConstants.Theme.TextLuminanceThreshold = 0.6`，只能改常量后重新构建。
- 内嵌 `assets/demoDrawer`（3 个文件）与 `DemoDrawerInitializer`；模板新增两个默认抽屉；`WasInitializedFromDefaults` 控制一次性创建。
- 设置页标题行加入 `Project on GitHub` 链接（`UseShellExecute = true`）。
- `Lock window` 文案、`ApplyWindowLocked`（隐藏按钮 + 标题跨三列）、标题右键统一走 `OpenSettingsPanel`。

已验证：

- `dotnet build` Debug/Release 均成功（.NET SDK 10.0.403）。
- 临时探针：嵌入资源名（中文与反斜杠正确）、`TextContrast` 阈值结果、模板解析出的两个抽屉路径、临时仓储首次初始化为 2 个抽屉、`DemoDrawerInitializer` 首次写入 3 个文件且二次调用不覆盖用户修改。

尚未验证（需要手工 UI 回归）：

- 深浅主题下主窗口/抽屉/右键菜单/设置预览的实际颜色。
- 锁定预览、取消、保存后的按钮与标题布局；标题栏右键；托盘设置入口。
- GitHub 链接点击（依赖默认浏览器）。
- 100%/150%/200% DPI 下的布局。
- 真实首次启动路径（需备份并临时改名用户配置后才能触发）。

## 7. 追加：抽屉背景色决定前景色（2026-03）

- `DrawerWindow.ApplyTheme` 不再使用主题的 `ContentTextPrimary`/`TextSecondary`，而是对叠加 `overlay_color` 后的 `contentBackground`/`windowBackground` 调用 `TextContrast.For`；底栏次级文字由同一结果再向窗口背景混合 35%。
- `MainWindow.DrawerList.Paint` 对带专属色的条目按 `BlendOverlayColor` 后的行背景调用 `TextContrast.For`；选中/悬停仍按“先强调色或悬停底色、再叠加 overlay”的顺序合成，激活行继续显示专属色；文字色从非悬停背景推导，避免悬停渐变时跳色。选中且无 overlay 时仍使用 `ItemSelectForeColor`（强调色文字）。
- `AppConstants.Theme.TextLuminanceThreshold` 默认值从 0.6 调整为 0.229：这是 `#303030` 与 `#F2F2F2` 对比度相等的交叉点，可避免白色/浅色 overlay 被误判为深色背景。仍可通过修改该常量并重建来调整倾向。
## 8. 性能优化：抽屉列表颜色缓存与刷新（2026-03）

- `DrawerListItem` 改为携带预计算颜色的可变对象：普通背景、前景、选中背景/前景、悬停目标色在列表填充或主题变化时计算；绘制时直接使用缓存值，悬停只在进入/离开行时各失效一次，不再逐帧执行 `Math.Pow`、overlay 混色或 `Blend`。
- 主题变化时原地更新条目颜色并只调用一次 `Invalidate()`；不替换 ListBox 条目，避免选中状态丢失或逐行重绘。
- `PopulateDrawerList` 使用 `BeginUpdate/EndUpdate`，避免清空/重建过程中的中间帧。
- `ApplySettingsSnapshot` 增加展示比较：只有名称、路径、专属色、顺序或目录存在状态变化时才重建列表；仅尺寸或排序变化时保留行与选中状态，只同步 `_currentDrawers` 定义（抽屉关闭会持久化尺寸，这是切换抽屉时闪动的主要来源）。
- 抽屉关闭事件先清理激活状态再重载设置，避免短暂重新选中已关闭的抽屉。
- 打开/切换抽屉期间使用 `_switchingDrawer` 抑制 `ClearDrawerListSelection`/`UpdateSelectedDrawer`，不再在打开前清空选中：点击行从鼠标按下的选中态一直保持到新抽屉显示完成，避免“选中背景 → 普通背景 → 选中背景”的闪动。`OpenDrawer` 现在返回是否成功，失败时清除选中，避免留下没有窗口的激活行。
- 去掉主抽屉列表的 160ms 悬停渐变：原生 `ListBox` 关闭了 `UserPaint`，`DoubleBuffered` 对原生绘制无效；逐帧 `InvalidateItem` 会让整个列表反复重绘并闪动。现在悬停直接使用预计算的 `HoverBackground`，只在进入/离开行时各失效一次。其它动画（抽屉滑入/淡入、滚动条加宽）不受影响。
- 新增 `Controls/CompositedPanel.cs` 作为抽屉列表的宿主（`WS_EX_COMPOSITED`）：原生 ListBox 即使只失效一行，也可能擦除并重绘整个客户区；把列表放进启用子窗口合成的面板后，原生绘制先落在离屏缓冲区，再整体呈现，避免单次失效造成的整表闪动。
