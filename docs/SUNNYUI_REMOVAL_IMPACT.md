# 移除 SunnyUI 的影响评估

> 目的：评估从当前 winDrawer 中移除 SunnyUI（`SunnyUI` 3.9.8）的技术影响、工作量、风险和可选路线。
>
> 本文是评估文档，不代表已实施移除。当前工作树中的 `docs/TODOs.md` 与近期阶段文档保持不变。
>
> **后续更新**：已在 `test/sunnyui-simple-controls` 分支完成全量移除；第 11–14 节记录实际试装结果，第 1–10 节保留为移除前的评估依据。

## 1. 结论摘要

- **技术上可以移除**，但不是删除一个 NuGet 包那么简单。
- SunnyUI 在当前应用中不仅提供控件，还提供了一套统一的视觉与交互：圆角按钮及悬停/按下颜色、带主题色的 `UIListBox` 和滚动条、`UITrackBar`、`UICheckBoxGroup`、`UIGroupBox`、`UIPanel`、`UIForm` 窗口外观。
- 如果要求**视觉与行为基本不变**，需要自己实现或引入替代控件；**代码量很可能增加，而不是减少**。
- 如果接受**原生 WinForms 外观**，可以主要使用原生 `Button`、`ListBox`、`VScrollBar`、`TrackBar`、`GroupBox`、`Panel`、`Label`，工作量会下降，但界面会明显变化。
- 建议：
  - 如果目标只是“减少第三方依赖/包体积”，可以做**分阶段替换**，先替换低风险控件，最后再决定是否移除最后一层；
  - 如果目标是“减少代码量/提高可读性”，**不建议**为移除 SunnyUI 而重写 UI；
  - 如果目标是“现代 UI 架构”，应评估 WinUI 3/WPF/Avalonia 迁移，而不是在 WinForms 上手工复刻 SunnyUI。

粗略工作量（一名熟悉该代码的开发者）：

| 路线 | 估计 | 风险 |
|---|---:|---|
| 只替换简单控件（TableLayoutPanel、Label、Panel、GroupBox、Form） | 0.5–1 天 | 低 |
| 替换按钮/滚动条/列表/滑块/复选框组并保持接近现有视觉 | 2–4 天 | 中高 |
| 全量替换 + 100%/150%/200% DPI + 完整 UI 回归 | 1–2 天 | 中高 |
| 总计（保持接近现有体验） | **约 4–7 天** | 中高 |

## 2. 当前 SunnyUI 使用清单

统计范围：`src/` 下手写代码与 Designer；不含 `obj/bin`。

- `using Sunny.UI`：**7 个文件**
- SunnyUI 控件类型引用：**约 87 处**（包含 Designer 属性赋值与字段声明）

| SunnyUI 类型 | 约引用数 | 主要位置 | 替代方案 | 风险 |
|---|---:|---|---|---|
| `UIButton` | 30 | 主窗口、抽屉窗口、两个设置窗口、`UiButtonTheming` | 自定义 `ThemedButton` 或原生 `Button` | 中高：圆角、状态色、字体/提示 |
| `UITableLayoutPanel` | 18 | 三个窗体 Designer | 原生 `TableLayoutPanel` | 低：主要是类型替换和移除 `TagString` |
| `UILabel` | 14 | 设置窗口、抽屉设置、主窗口 | 原生 `Label` | 低：字体/颜色/对齐基本兼容 |
| `UIGroupBox` | 8 | 设置窗口、抽屉设置 | 原生 `GroupBox` 或自定义 `ThemedGroupBox` | 中：边框、圆角、背景色 |
| `UITrackBar` | 4 | 设置窗口（高度、透明度） | 原生 `TrackBar` 或自定义 `ThemedTrackBar` | 中：填充色、滑块外观、DPI |
| `UIPanel` | 3 | 设置窗口、抽屉设置的颜色预览 | 原生 `Panel` + Paint 或 `ColorPreviewPanel` | 低到中：边框/填充色 |
| `UICheckBoxGroup` | 2 | 设置窗口行为项 | 多个原生 `CheckBox` / `CheckedListBox` / 自定义组 | 中：`SelectedIndexes` 语义与布局 |
| `UIListBox` | 2 | 主窗口抽屉列表 | 原生 `ListBox` + owner-draw + hover | 高：hover、选择色、滚动条、拖放 |
| `UIScrollBar` | 2 | 抽屉内容窗口 | 原生 `VScrollBar` 或自定义滚动条 | 高：与 `ScrollableListView` 同步 |
| `UIStyle` | 3 | 主窗口 Designer | 删除，使用自定义颜色 | 低 |
| `UIForm` | 1 | `SettingsForm` 基类 | 原生 `Form` + 无边框/阴影 | 中：窗口外观与行为 |

### 2.1 文件级影响

| 文件 | SunnyUI 使用 | 影响 |
|---|---|---|
| `src/Forms/SettingsForm.Designer.cs` | 44 处，几乎全部控件 | 需要重写/重新生成大部分 Designer 声明与属性 |
| `src/Forms/DrawerSettingsDialog.cs` | 19 处，代码构建 UI | 需要替换 GroupBox、Panel、Label、Button 及样式 |
| `src/Forms/MainWindow.Designer.cs` | 15 处，`UIListBox` 是核心 | 抽屉列表的绘制、hover、选择、滚动、拖放都要回归 |
| `src/Forms/DrawerWindow.Designer.cs` | 8 处，`UIScrollBar` | 滚动条位置/值/宽度/主题和视口同步需要重写 |
| `src/Forms/SettingsForm.cs` | `UIForm`、`UICheckBoxGroup`、`UITrackBar`、`UIPanel` | 行为映射与预览逻辑需要调整 |
| `src/Forms/DrawerWindow.cs` | `UIButton`、`UIScrollBar` | 主题配色、滚动条填充宽度/颜色 |
| `src/Controls/UiButtonTheming.cs` | 只依赖 `UIButton` 的三态/边框属性 | 需要改为 `ThemedButton` 或原生 `Button` 的映射层 |

### 2.2 包与构建

- `src/winDrawer.csproj` 当前引用：
  ```xml
  <PackageReference Include="SunnyUI" Version="3.9.8" />
  ```
- 完全移除后需要删除该引用、7 个 `using Sunny.UI`，以及所有 `Sunny.UI.*` 类型/Designer 字段。
- 发布体积会减少，但具体数值需要在干净输出目录中构建/发布后测量；本文不声称固定 MB 数。
- SunnyUI 的版本升级、主题行为和控件 bug 将不再由第三方维护，转移到本项目的自绘控件上。

## 3. 核心耦合点

### 3.1 `UIButton`：最大量的视觉耦合

- `UiButtonTheming` 直接设置 `FillColor`、`FillHoverColor`、`FillPressColor`、`FillSelectedColor`、`ForeColor`、`RectColor`、`RectHoverColor` 等属性。
- Designer 和 `DrawerSettingsDialog` 还使用 `Radius`（圆角）、`TipsFont`、`Style = UIStyle.Custom`、`RectSize`、`Cursor` 等。
- `SettingsForm.ApplyFontToControls` 通过 `control is UIButton` 决定按钮字体和 `TipsFont`。
- 原生 `Button` 没有这些状态色/圆角/提示字体属性。若保持现有视觉，需要实现一个 `ThemedButton : Button`（自绘背景、边框、圆角、hover/press/disabled 状态），大约 150–250 行；若接受原生外观，可直接替换，但按钮会从圆角彩色变为系统按钮。

### 3.2 `UIListBox`：交互最复杂

`MainWindow.DrawerList.cs` 依赖：

- `drawerListBox.ListBox.DrawMode = DrawMode.OwnerDrawFixed`（SunnyUI 内部原生 ListBox）
- `Items`、`SelectedIndex`、`IndexFromPoint`、`GetItemRectangle`
- `ItemHeight`、`FillColor`、`ForeColor`、`HoverColor`、`ItemSelectForeColor`、`ItemSelectBackColor`
- `ScrollBarBackColor`、`ScrollBarColor`、`ScrollBarHandleWidth`、`ScrollBarStyleInherited`
- `DoDragDrop`、`AllowDrop`、`MouseDown/MouseUp/MouseMove`

替换为原生 `ListBox` 可行，但需要：

- 设置 `DrawMode = OwnerDrawFixed`、`ItemHeight`、`BorderStyle.None`；
- 自己维护 hover 项（原生 ListBox 没有 hover 状态）；
- 在 `DrawItem` 中绘制背景、选择色、hover 色、文字和焦点矩形；
- 决定使用原生滚动条还是保留自定义滚动条；
- 回归拖动重排、右键设置、点击切换、长列表滚动。

这部分的视觉/行为风险最高，不能只做类型替换。

### 3.3 `UIScrollBar`：与视口管线绑定

`DrawerWindow.FileList.cs` 与 `ScrollableListView` 协作：

- `drawerContentsScrollBar.Maximum`、`Value`、`Visible`；
- `ValueChanged` → `ScrollToVerticalPosition`；
- `FillWidth`、`FillColor`、`ForeColor`、`HoverColor`、`PressColor`；
- `PositionDrawerScrollBar()` 负责宽度、位置、DPI 缩放和无障碍名称。

原生 `VScrollBar` 可以承载值/范围，但没有 `FillWidth` 和自定义三态颜色；若要接近现有视觉，需要自绘滚动条。另一个方向是改用 `ListView` 自带滚动条并删除独立滚动条，这会改变布局/宽度计算和主题外观，回归面也不小。

### 3.4 `UITrackBar` 与 `UICheckBoxGroup`

- `SettingsForm` 使用 `UITrackBar.ValueChanged`、`FillColor`、`Minimum/Maximum/Value`。
- `UICheckBoxGroup` 使用 `Items`、`SelectedIndexes`、`ValueChanged`、`CheckBoxSize`、`ItemSize`、`ColumnCount`、`ColumnInterval`、`HoverColor`，并在 `FitBehaviorItemSize` 中依赖 SunnyUI 的文本绘制偏移。
- 替换为原生控件需要重写“选择索引 ↔ 配置布尔值”的映射、布局和预览事件；原生 `TrackBar` 外观与现有蓝色填充滑块不同。

### 3.5 `UIForm`、`UIGroupBox`、`UIPanel`、`UILabel`、`UITableLayoutPanel`

- `SettingsForm : UIForm`，Designer 使用 `ShowTitle = false`、`RectColor = Color.Transparent`、`ZoomScaleRect`；`UIForm` 提供无标题栏窗口外观/阴影/边框行为。换成原生 `Form` 需要显式设置无边框、背景、拖动区域和外阴影（如需要）。
- `UIGroupBox` 使用 `FillColor`/`RectColor`，原生 `GroupBox` 只有标题和边框；要保持现有深色圆角分组，需要自定义绘制。
- `UIPanel` 用于颜色预览，`FillColor`/`RectColor` 可改为 `Panel` + `Paint` 或一个小型 `ColorPreviewPanel`。
- `UILabel` 基本可由原生 `Label` 替代；`UITableLayoutPanel` 基本可由原生 `TableLayoutPanel` 替代，只需移除 `TagString` 等 SunnyUI 专有属性。
- 这些简单控件的替换工作量低，但会改变细节视觉（边框、圆角、背景、字体缩放）。

## 4. 非视觉影响

### 4.1 DPI 与布局

- 当前 `SettingsForm` 使用 `AutoScaleMode.Dpi`，`MainWindow` 使用 `AutoScaleMode.Font`，`DrawerWindow` 使用 `AutoScaleMode.None` 并手工缩放。
- 替换控件后，原生控件的默认尺寸、内边距、最小尺寸和字体度量会变化，必须重新验证 100%/150%/200% DPI。
- 自定义 `ThemedButton`/滚动条/列表必须自己处理 DPI，否则会把当前已经解决的 DPI 问题重新引入。
- `SunnyUI` 控件对 DPI 的处理并不完全等同于原生控件；不能假设“换成原生就更自动”。

### 4.2 可访问性

- 原生 `Button`/`ListBox`/`TrackBar` 通常有更好的默认可访问性。
- 自绘控件（`ThemedButton`、`ThemedListBox`、自定义滚动条）必须显式设置 `AccessibleName`、`AccessibleDescription`、`AccessibleRole`，否则会退化。
- 当前抽屉路径、关闭/刷新按钮、滚动条已有部分无障碍设置，替换时必须保留。

### 4.3 行为/焦点

- `UIListBox` 的 hover/选择/滚动/拖放语义与原生 `ListBox` 不完全一致；需要手工补齐。
- `UITrackBar` 的键盘步进、方向键、PageUp/PageDown 行为与原生 `TrackBar` 可能不同。
- `UICheckBoxGroup` 的 `SelectedIndexes` 是列表语义，原生 `CheckBox` 是独立布尔值；需要保证保存/取消/Default 的行为不变。
- `UIForm` 的关闭、拖动、阴影、焦点行为需要重新确认。

### 4.4 代码量与依赖

- 移除 SunnyUI 会删除一个较大的第三方依赖和它的版本升级负担。
- 但为了保持现有视觉，需要新增 `ThemedButton`、`ThemedListBox`、`ThemedScrollBar` 等自绘控件；**总代码量很可能增加 200–500 行**，而不是减少。
- Designer 文件不会因为去掉 SunnyUI 自动变小：属性赋值仍要保留，只是类型/属性名变化。
- 结论：**移除 SunnyUI 是“依赖减少”，不是“代码精简”**。

## 5. 可选路线

### 路线 A：保留 SunnyUI（当前推荐）

- 适合：希望继续小步优化代码、减少回归风险、保持现有视觉。
- 代价：保留第三方依赖和升级负担。
- 与 10.3 的安全清理（去重/命名/DPI helper）不冲突。

### 路线 B：分阶段替换，先简单后复杂

1. 替换 `UITableLayoutPanel` → `TableLayoutPanel`，`UILabel` → `Label`，`UIPanel` → `Panel`/自定义预览面板，`UIGroupBox` → `GroupBox`/自定义分组。
2. 替换 `UIForm` → 原生 `Form` + 无边框外观。
3. 替换 `UITrackBar`、`UICheckBoxGroup`。
4. 最后替换 `UIButton`、`UIListBox`、`UIScrollBar`（视觉/行为风险最高）。
5. 全部替换后再删除 NuGet 引用。

优点：每一步可构建/回归，风险分散。  
缺点：中间状态同时存在 SunnyUI 和原生控件，主题规则可能短期分叉。

### 路线 C：全量替换并自绘以保持视觉

- 适合：明确要移除依赖，且接受为视觉一致性投入维护成本。
- 需要新增自绘控件和统一主题映射；工作量最大，长期维护成本也转移到本项目。
- 预计总代码量增加，不建议以“减少代码”为目标选择。

### 路线 D：迁移到 WinUI 3 / WPF / Avalonia

- 能同时摆脱 SunnyUI 和大部分 WinForms 自绘/DPI 代码。
- 但这是完整 UI 重写，涉及窗口、列表、拖放、Shell 图标、主题、托盘等，远超“移除 SunnyUI”的范围。
- 适合未来产品级重写，不适合当前小工具的一次依赖清理。

## 6. 如果决定移除，建议的实施顺序

1. **建立视觉基线**：记录主窗口、抽屉、设置页在 100%/150%/200% DPI 下的截图；记录按钮/列表/滚动条/滑块状态。
2. **先替换无行为耦合的控件**：`UITableLayoutPanel`、`UILabel`、`UIPanel`、`UIGroupBox`、`UIForm`。
3. **替换 `UITrackBar`、`UICheckBoxGroup`**，重点回归设置保存/取消/Default。
4. **替换 `UIButton`**：优先实现 `ThemedButton`，保持 `UiButtonTheming` 的调用接口，减少调用点改动。
5. **替换 `UIListBox`**：实现 hover/选择/滚动/拖放，回归抽屉打开/切换/重排/右键。
6. **替换 `UIScrollBar`**：确认 `ScrollableListView` 的滚动范围、视口、图像延迟加载和 DPI 缩放。
7. **删除 `SunnyUI` PackageReference 和所有 `using Sunny.UI`**，清理 Designer 字段。
8. **完整回归**：构建、单文件发布、DPI、深浅主题、抽屉容量上限、托盘/热键、设置保存/取消、拖放。


## 7. 回归清单

### 7.1 主窗口

- 三个以上抽屉的显示、选择、打开/关闭切换；
- 抽屉列表 owner-draw 的普通/hover/选中/带专属颜色状态；
- 拖动重排、插入标记、右键打开抽屉设置；
- 设置按钮、添加抽屉按钮的普通/hover/按下/禁用状态；
- 托盘显示/隐藏、Win+Shift+D、退出；
- 无任务栏按钮、置顶、窗口锁定。

### 7.2 抽屉窗口

- 打开、关闭、再次打开；
- 直接子项、排序（newest/oldest/name）、F5/右键 Refresh；
- 缩略图/Shell 图标、滚动、视口图像延迟加载；
- 超过 `max_drawer_items` 的状态；
- 底栏路径入口、打开资源管理器；
- 拖入/拖出、Ctrl/Shift 语义、重名冲突；
- 窗口缩放、最小尺寸、尺寸持久化；
- 无标题栏、顶部缩放手柄、关闭/刷新按钮主题。

### 7.3 设置页

- 颜色预览点击、系统颜色对话框、透明度滑块、窗口高度滑块；
- 行为复选框四项、保存/取消/Default；
- 登录启动、置顶、窗口锁定、热键开关；
- Open settings TOML、Close app；
- 设置预览的实时应用与取消恢复；
- 字体、按钮配色、分组边框、DPI。

### 7.4 抽屉设置对话框

- 名称、路径 Browse、颜色选择/清除、Delete、Save/Cancel；
- 路径只读、Enter/Space 激活；
- 无边框拖动、Escape/Alt+F4 取消；
- 主题分组/按钮外观。

### 7.5 工程与打包

- Debug/Release 完整构建 0 警告 0 错误；
- 单文件发布成功，资源（模板/字体/图标）仍在；
- 无 `SunnyUI` 残留 `using`/类型/包引用；
- 程序集/发布体积变化记录；
- 100%/150%/200% DPI、副屏/跨屏。

## 8. 风险矩阵

| 风险 | 概率 | 影响 | 缓解 |
|---|---|---|---|
| 按钮/列表/滚动条视觉明显变化 | 高 | 中 | 先做视觉基线；接受原生外观或实现自绘控件 |
| `UIListBox` hover/选择/拖放行为退化 | 中高 | 高 | 单独实现 `ThemedListBox`，逐项回归 |
| `UIScrollBar` 与 `ScrollableListView` 不同步 | 中高 | 高 | 保留现有滚动同步协议，先替换外观再换值模型 |
| `UICheckBoxGroup` 保存/取消映射错误 | 中 | 中 | 用四项布尔值显式映射，覆盖 Default/Cancel |
| DPI 回归 | 中 | 中 | 每个控件替换后测试 100/150/200% |
| 代码量增加、维护成本转移 | 高 | 中 | 明确目标是“去依赖”而非“减代码” |
| Designer 大改导致布局漂移 | 中 | 中 | 一次只替换一类控件，保留原始尺寸/锚点 |
| 无障碍退化 | 中 | 中 | 自绘控件补 `AccessibleName/Role` |
| 包移除后遗漏资源/主题 | 低 | 中 | 全仓搜索 `Sunny.UI`/`UI*`，完整发布验证 |

## 9. 建议

**当前不建议仅为“减少代码量”移除 SunnyUI。**  
当前 87 处引用中，大部分是 Designer 属性；移除后为了保持视觉，仍需要自绘控件和同等数量的主题属性，代码量可能增加。

建议按目标选择：

1. **目标：稳定、少回归、继续小步优化**  
   → 保留 SunnyUI；继续 10.3 的安全清理（去重、helper、命名）。

2. **目标：减少第三方依赖/包体积**  
   → 采用路线 B：先替换简单控件，最后处理按钮/列表/滚动条；分阶段提交，不一次性删除包。

3. **目标：现代 UI/长期维护**  
   → 评估 WinUI 3/WPF/Avalonia 迁移；这是独立的大项目，不应和当前 WinForms 小工具重构混在一起。

4. **目标：完全移除且保持现有视觉**  
   → 接受新增自绘控件与约 4–7 天工作量；先做视觉基线，再按第 6 节顺序实施。

### 决策建议

如果以下任一条件成立，才建议启动移除：

- 组织明确要求减少第三方 UI 依赖；
- SunnyUI 出现无法接受的维护/许可/安全问题；
- 计划同时重做设置页/抽屉列表的视觉与交互。

如果只是为了“代码看起来少一点”，优先做 10.3 的安全清理，不要动 SunnyUI。

## 10. 假设与非目标

- 本评估基于当前 `SunnyUI 3.9.8`、`net10.0-windows`、单个 WinForms 项目。
- 不评估具体第三方替代控件库；若引入新的 UI 库，需要重新评估依赖/许可/体积。
- 不包含 WinUI 3/WPF/Avalonia 迁移的详细设计。
- 不修改任何源码、`SunnyUI` 包引用或 `docs/TODOs.md`。
- 工作量是粗略估计，实际取决于是否要求像素级视觉一致、是否保留自绘控件、以及 DPI/主题回归范围。

## 11. 第一批试装记录：布局与文本控件

分支：`test/sunnyui-simple-controls`

本批次只替换低风险的布局/文本控件，SunnyUI 包仍保留：

| 项目 | 结果 |
|---|---|
| `UITableLayoutPanel` → `TableLayoutPanel` | 9 个实例，含 3 个窗体 Designer |
| `UILabel` → `Label` | 8 个实例，含 `DrawerSettingsDialog` |
| `TagString` 属性 | 已删除 |
| `logoLabel.Style = UIStyle.Custom` | 已删除（`UIStyle` 仍被按钮使用） |
| 原生 Label 布局 | 显式 `AutoSize = false`，`BackColor = Transparent` |
| 替换后的 TableLayoutPanel 背景 | 显式 `#404040`，避免深色界面出现系统灰块 |
| SunnyUI 类型引用 | 约 87 → 54 |
| 仍保留的 SunnyUI 类型 | `UIButton`、`UIGroupBox`、`UITrackBar`、`UIPanel`、`UICheckBoxGroup`、`UIListBox`、`UIScrollBar`、`UIStyle`、`UIForm` |
| Debug/Release 构建 | 均成功，无构建错误 |

未完成：尚未进行实际窗口视觉/DPI 手工验收。建议重点检查设置页标题与滑块标签、主窗口标题、抽屉底栏在 100%/150%/200% DPI 下是否仍保持原有深色背景和布局。


## 12. 第二批试装记录：按钮

分支：`test/sunnyui-simple-controls`

| 项目 | 结果 |
|---|---|
| `UIButton` → 原生 `Button` | 约 30 处引用全部替换 |
| 按钮主题 | `UiButtonTheming` 改用 `FlatStyle.Flat` + `BackColor` + `FlatAppearance` |
| 已删除的 SunnyUI 属性 | `Radius`、`TipsFont`、`Fill*`、`Rect*`、`ForeHover/Press/Selected/Disable`、`Style` |
| `DrawerWindow` 的 `using Sunny.UI` | 已删除 |
| SunnyUI 类型引用 | 约 54 → 22 |
| 仍保留的 SunnyUI 类型 | `UIGroupBox`、`UITrackBar`、`UIPanel`、`UICheckBoxGroup`、`UIListBox`、`UIScrollBar`、`UIForm` |
| Debug/Release 构建 | 均成功；因 `bin` 被运行中的实例锁定，使用 `-o Z:\Temp\...` 验证 |

接受的外观变化：

- 按钮从圆角变为直角；
- 不再有 SunnyUI 的 `TipsFont` 和独立禁用/悬停文字颜色；
- 禁用状态使用 WinForms 原生绘制；
- 主窗口的 `settings`/`add drawer` 按钮会显示主题边框。

功能行为保持不变：点击、主题色、hover/press 背景色、字体、无障碍名称和对话框默认按钮均沿用原有逻辑。


## 13. 第三批试装记录：窗体、分组、滑块与复选框

分支：`test/sunnyui-simple-controls`

| 项目 | 结果 |
|---|---|
| 按钮字体 | `10.5F` → `10F`，减少花体字基线在按钮底部被裁切的程度 |
| `UIForm` → `Form` | `SettingsForm` 使用无边框原生 Form，保留自定义拖动/保存/取消逻辑 |
| `UIGroupBox` → `GroupBox` | 设置页与抽屉设置对话框改为原生分组框 |
| `UIPanel` → `ColorPreviewPanel` | 新增轻量自绘面板，保留 `FillColor`/`RectColor`/文字预览 API |
| `UITrackBar` → `TrackBar` | 使用原生滑块，设置 `TickStyle=None` 和深色背景 |
| `UICheckBoxGroup` → `BehaviorCheckBoxGroup` | 新增原生 CheckBox 两列分组，保留 `SelectedIndexes`/`ValueChanged` API |
| SunnyUI 类型引用 | 约 22 → 4 |
| 仍保留的 SunnyUI 类型 | 仅 `UIListBox`、`UIScrollBar` |
| `using Sunny.UI` | 仅剩 `MainWindow.Designer.cs`、`DrawerWindow.Designer.cs` |
| Debug/Release 构建 | 均成功，使用临时输出目录避开被运行实例锁定的 `bin` |

接受的外观变化：

- 设置页分组框由圆角/自绘边框变为原生 GroupBox 边框；
- 滑块由 SunnyUI 自绘样式变为原生 TrackBar；
- 行为复选框由 SunnyUI 绘制变为原生 CheckBox，保留两列布局；
- 设置窗口不再有 SunnyUI 的窗口阴影/圆角外观。

功能行为保持不变：设置保存/取消/Default、颜色预览、透明度/高度即时预览、四个行为项、无障碍名称和窗体拖动逻辑均沿用原有事件路径。

尚未完成：`UIListBox` 与 `UIScrollBar` 仍是下一批高风险项；本批次也尚未做实际窗口视觉/DPI 手工验收。


## 14. 第四批试装记录：列表与滚动条（SunnyUI 已完全移除）

分支：`test/sunnyui-simple-controls`

| 项目 | 结果 |
|---|---|
| `UIListBox` → `DrawerListBox` | 原生 owner-draw ListBox，保留 hover、选中色、拖放和抽屉插入标记 |
| `UIScrollBar` → `ThemedScrollBar` | 项目内细自绘垂直滚动条，重新计算 `LargeChange`/`Maximum`/`Value` |
| SunnyUI 类型引用 | **0** |
| `using Sunny.UI` | **0** |
| `winDrawer.csproj` | 已删除 `SunnyUI 3.9.8` 包引用 |
| 构建产物 | Debug/Release 输出中不再包含 `SunnyUI.dll` |
| Debug/Release 构建 | 均成功 |
| 设置页显示区域 | 适当减小原生 GroupBox 的上下 Padding，给内容多留高度 |

接受的外观变化：

- 抽屉列表滚动条由 SunnyUI 自绘样式变为项目内细自绘滚动条（不再显示原生系统滚动条）；
- 列表边框和 hover 绘制细节以原生 ListBox 为准；
- 设置页 GroupBox 的标题/边框改为原生外观。

仍需人工验证：

- 长列表滚动、拖放重排和插入标记；
- 抽屉内容窗口的滚动范围、滚轮、缩略图加载和 DPI；
- 设置页各分组在 100%/150%/200% DPI 下是否仍有高度裁切；
- 主窗口/抽屉列表 hover 与选中颜色。

### 迁移结论

SunnyUI 已从源码和包引用中完全移除。当前项目只依赖 WinForms 原生控件和项目内轻量控件：

- `DrawerListBox`
- `ColorPreviewPanel`
- `BehaviorCheckBoxGroup`

后续维护重点是这些自绘/自绘容器控件的行为回归，而不是第三方 UI 库升级。

## 15. 后续调整：按钮字体与自绘滚动条

- 按钮字体分两级：
  - `Cancel`/`Save` 保持 `ButtonFontSize = 10F`；
  - 其他按钮使用 `CompactButtonFontSize = 9.5F`，减少花体字底部裁切。
- `UIScrollBar` 先替换为原生 `VScrollBar` 后，因系统样式不符合深色主题，又替换为项目内 `ThemedScrollBar`：
  - 细轨道 + 圆角滑块；
  - 支持 hover/pressed 颜色、拖动、Track 点击、滚轮和键盘；
  - 通过 `Maximum`（最大滚动位置）和 `LargeChange`（视口高度）计算滑块比例。
- 设置页继续压缩原生 GroupBox 的顶部 Padding，为内容留出更多高度。

