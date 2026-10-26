# 阶段 10 代码审查清单

> 目的：为 10.3 的逐项讨论提供依据。本文最初只记录审查结果；后续 SunnyUI 迁移已在独立测试分支完成并合并。
>
> 审查范围：`src/` 下全部手写 C#（不含 Designer 生成代码），以及 10.1/10.2 新增/修改的代码。工作树中的 `docs/TODOs.md` 为用户改动，本文未修改它。
>
> **后续状态（2026-10）：** SunnyUI 已完全移除，C1 已完成；按钮改为原生扁平 `Button`，设置页改为原生控件，滚动条改为裁剪原生条 + `ThemedScrollBar`。本文中关于 `UIButton`、`Radius`、SunnyUI 保留建议和旧行号的描述属于迁移前快照，当前实现见 [`WHITEPAPER.md`](WHITEPAPER.md) 与 [`SUNNYUI_REMOVAL_IMPACT.md`](SUNNYUI_REMOVAL_IMPACT.md)。A 组低风险清理与 B1/B2/B3/B5 已于 2026-10 完成，实施结果见第 2、3 节；B4/B6 与 C 组仍按原清单讨论，不代表已实施。

## 1. 当前规模

- 56 个 C# 文件，约 8,562 行；其中 Designer 约 770 行，手写代码约 7,792 行。
- 最大文件：
  - `MainWindow.cs` 430 行
  - `DrawerSettingsDialog.cs` 429 行
  - `SettingsForm.Designer.cs` 396 行
  - `DrawerWindow.FileList.cs` 394 行
  - `SettingsForm.cs` 355 行
  - `TrayController.cs` 320 行
  - `SettingsRepository.Persistence.cs` 304 行
  - `DrawerWindow.cs` 297 行
  - `SettingsRepository.Drawers.cs` 295 行
  - `ThemedScrollBar.cs` 295 行

需要先说明一个判断：这 8,500 行里真正“重复、可机械删除”的部分有限。大量代码来自 WinForms 布局、DPI、主题派生、延迟缩略图、拖放、配置原子写入和托盘生命周期。**仅靠重构很难把代码量减半；若目标是大幅缩减，更有效的是减少功能/配置项，而不是继续抽象。**

## 2. A 组：低风险（已实施）

> **实施状态（2026-10）：** A1–A9 已全部完成，Debug/Release 完整构建通过（0 警告 0 错误）。已有文件净减少 162 行；新增 `DialogFontSet`、`FormDragBehavior`、`DpiScale` 三个共享类共 131 行，整体净减少约 31 行。收益主要在统一规则和删除重复，而不是行数。下列“位置”仍是实施前的行号快照，实际落点见每条“实施结果”。

### A1 两个设置窗体的品牌字体生命周期重复

- 位置：
  - `SettingsForm.cs:15-19`（字段）、`34-40`（初始化）、`104-144`（创建/应用/释放）
  - `DrawerSettingsDialog.cs:13-16`、`52-55`、`95-113`、`115-130`
- 现象：两边都各自持有 `_ownedFonts`、创建 `BrandingFont.Create(10.5F/13F)`、遍历控件应用、在 `Dispose` 中释放。
- 建议：提取一个很小的 `DialogFontSet`（系统字体、按钮字体、标题字体 + `Dispose` + `ApplyTo(Control)`）。不引入基类，不改变控件布局。
- 预计减少：约 35–45 行；风险：低。
- 讨论点：是否接受新增一个约 60 行的小类，换取两个窗体各减少约 20 行，并统一释放顺序。
- 实施结果：已新增 `Controls/DialogFontSet.cs`（49 行）。两个窗体各持有一个 `_fonts`，统一提供常规/主按钮/紧凑按钮/标题字体；`Dispose` 由该对象统一释放，`SettingsForm.Designer.Dispose` 改为调用 `_fonts.Dispose()`。

### A2 按钮配色/圆角/光标重复

- 位置：
  - `SettingsForm.cs:146-158`
  - `DrawerSettingsDialog.cs:351-364`
  - `DrawerWindow.cs:289-312`
  - `UiButtonTheming.cs` 已有底层调色方法
- 现象：保存/取消/普通按钮的颜色组合重复；`DrawerSettingsDialog` 还额外设置 `Radius = 20`、`Cursor = Hand`。
- 建议：在 `UiButtonTheming` 增加按语义角色（Primary/Secondary/Danger）设置颜色的方法，或增加 `ApplyDialogButton`；`DrawerWindow` 的关闭/刷新按钮保留特殊处理。
- 预计减少：约 20–30 行；风险：低。
- 讨论点：语义色是否要成为唯一入口，还是只合并设置窗体的两处。
- 实施结果：`UiButtonTheming` 新增 `ApplyPrimaryPalette` / `ApplySecondaryPalette` / `ApplyDangerPalette`（可选 `handCursor`）。两个设置窗体删除各自的配色包装 `ConfigureButton` / `ConfigureButtonStyle`（`DrawerSettingsDialog` 中设置文字和尺寸的布局 `ConfigureButton` 保留）；`DrawerWindow` 的关闭按钮仍保留专用红色处理。

### A3 颜色对话框配置与调用重复

- 位置：
  - `SettingsForm.cs:300-325`
  - `DrawerSettingsDialog.cs:476-525`
  - `UiDialogHelpers.cs` 已有自定义色和 Enter/Space 激活
- 现象：两边都设置 `AllowFullOpen/FullOpen/AnyColor/SolidColorOnly`，都调用 `BuildColorDialogCustomColors`；一边用局部 `ColorDialog`，一边长期持有。
- 建议：在 `UiDialogHelpers` 增加 `ShowColorDialog(owner, initial, secondary)`，返回 `Color?`；调用方只处理结果。保留 `HandleActivationKey`。
- 预计减少：约 25–35 行；风险：低。
- 讨论点：是否接受 helper 内部创建/释放 `ColorDialog`；这比当前 `DrawerSettingsDialog` 长期持有一个对话框更简单。
- 实施结果：`UiDialogHelpers.ShowColorDialog(owner, initial, secondary)` 统一设置 `AllowFullOpen/FullOpen/AnyColor/SolidColorOnly` 与自定义色，并在每次调用内创建/释放对话框。`DrawerSettingsDialog` 的长期 `_colorDialog` 字段和两处配置方法已删除。

### A4 无边框设置窗体的拖动逻辑重复

- 位置：
  - `SettingsForm.cs:21-22`、`225-237`、`511-541`
  - `DrawerSettingsDialog.cs:32-33`、`411-440`
- 现象：两个窗体各自维护 `_isDragging/_dragOffset`，鼠标按下/移动/抬起逻辑几乎相同；`DrawerSettingsDialog` 额外使用 `Capture`。
- 建议：只对设置窗体提取 `FormDragBehavior`（绑定 header 控件）；主窗口拖动有锁定语义，不纳入。
- 预计减少：约 30–40 行；风险：低到中（鼠标捕获和 DPI 行为需手工验证）。
- 讨论点：是否值得为两个窗体引入一个行为类；如果只保留一处，优先保留 `SettingsForm` 的简单实现。
- 实施结果：已新增 `Controls/FormDragBehavior.cs`（61 行），两个设置窗体通过 `FormDragBehavior.Attach(this, header...)` 绑定。拖动偏移改用屏幕坐标，避免标题控件不在客户区原点时跳动；鼠标捕获在抬起时释放。主窗口因锁定状态和位置持久化语义仍保留自己的实现。

### A5 DPI 缩放散落

- 位置：
  - `MainWindow.cs:451`
  - `MainWindow.DrawerList.cs:263`
  - `DrawerWindow.cs:108`、`266`
  - `DrawerWindow.FileList.cs:77`、`140`
- 现象：多处 `dpi / 96d`、`Math.Max(1, (int)Math.Round(...))` 重复。
- 建议：增加 `DpiScale.Pixels(logical, dpi)` / `DpiScale.Factor(dpi)` 两个小方法。
- 预计减少：约 10–15 行；风险：低。
- 讨论点：收益主要是“统一规则”，不是行数；是否纳入本轮。
- 实施结果：已新增 `Infrastructure/DpiScale.cs`（21 行），替换主窗口、抽屉窗口、滚动条和抽屉设置中的 DPI 取整。剩余 `Math.Round` 只用于比例（滚动条/图像缩放）和颜色计算，不再承担 DPI 换算。

### A6 平凡包装/转发方法

- 位置：
  - `DrawerWindow.cs:143-146` `BlendColor` 仅转发 `ThemePalette.BlendOverlayColor`
  - `DrawerWindow.FileList.cs:50-53` `RefreshDrawerAsync` 仅转发 `PopulateFileListAsync`
  - `SettingsForm.cs:155-158` `ConfigureButton` 仅转发 `UiButtonTheming.ApplyColorPalette`
- 建议：删除包装，调用点直接用目标方法；`RefreshDrawerAsync` 统一命名为 `PopulateFileListAsync` 或反向重命名。
- 预计减少：约 10–15 行；风险：低。
- 讨论点：是否保留 `RefreshDrawerAsync` 作为语义名称（“用户刷新”）而把 `PopulateFileListAsync` 改名，避免两个名字。
- 实施结果：已删除 `DrawerWindow.BlendColor`；`RefreshDrawerAsync` 的所有调用点（F5、右键刷新、排序、拖放后刷新）统一为 `PopulateFileListAsync`；`SettingsForm.ConfigureButton` 已随 A2 删除。

### A7 主窗口清空选择状态的重复分支

- 位置：`MainWindow.Drawers.cs:133-151`
- 现象：`InvokeRequired` 的两个分支写了同一段清空选中逻辑。
- 建议：统一为一个本地方法，必要时 `BeginInvoke` 一次；或直接使用 `BeginInvoke`（窗体句柄已存在）。
- 预计减少：约 10–15 行；风险：低。
- 讨论点：是否要保留同步路径以避免关闭时的竞态；需要确认事件触发上下文。
- 实施结果：已提取 `ClearDrawerListSelection()`，保留 `InvokeRequired` 的 `BeginInvoke` 分支和同步分支，并用 `try/finally` 复位 `_suppressSelectionChange`。

### A8 透明度范围硬编码

- 位置：`SettingsSnapshot.cs:25` 使用 `Math.Clamp(windowOpacity, 0, 100)`，而 `AppConstants.Theme` 已有 `OpacityMin/OpacityMax`。
- 建议：改用常量。
- 预计减少：0 行；风险：无；属于一致性修正。
- 讨论点：是否纳入“顺手清理”。
- 实施结果：`SettingsSnapshot.WindowOpacity` 已改用 `AppConstants.Theme.OpacityMin` / `OpacityMax`。

### A9 命名与注释一致性

- 位置/现象：
  - `MainWindow.cs:419-447` 的 `ApplySnapshot` 只是赋值后调用 `ApplySettings`，两个名字语义接近；
  - `MainWindow.cs:439`、`MainWindow.Drawers.cs:136` 有中文注释，其它代码注释为英文；
  - 两个类都有 `ConfigureButton`，语义不同。
- 建议：只重命名确实容易混淆的成员；不批量翻译注释。
- 预计减少：0 行；风险：低。
- 讨论点：是否把 `ApplySettings` 改为 `ApplySettingsSnapshot`，把 `ApplySnapshot` 合并进去；这会触及主窗口多处调用，需单独确认。
- 实施结果：`ApplySnapshot` 已合并进 `ApplySettingsSnapshot`（先保存快照，再应用 UI），主窗口与设置面板的 6 处调用已更新；`MainWindow.cs` 中被点名的中文注释已改为英文；`SettingsForm.ApplySnapshotToUi` 保留，因为它是窗体内部的 UI 同步，不属于同一职责。其余中文注释未批量翻译。

## 3. B 组：中等风险，先讨论再决定

> **实施状态（2026-10）：** B1/B2/B3/B5 已完成；B4/B6 本轮不做。下列“位置”是实施前快照，实际结果见各条“实施结果”。

### B1 `DrawerSettingsDialog` 的代码布局过长

- 位置：`DrawerSettingsDialog.cs:132-234`（`ConfigureLayout`，约 103 行）、`254-311`（`ConfigureOverlayGroup`，约 58 行）、`312-357`（`ApplyStyle`，约 46 行）。
- 现象：一个方法里同时创建 header、字段、路径、颜色区、按钮区和行高；阅读时需要来回跳。
- 建议：按区域拆成 `BuildHeader/BuildDetails/BuildOverlay/BuildFooter`，把 `CreateLabel/FindLabels` 等通用控件工厂移到小的 UI helper。**不改成 Designer**，避免生成代码增加。
- 预计减少：约 30–50 行；风险：中（布局/DPI 需手工验证）。
- 讨论点：目标是“可读”还是“行数”；拆方法可能不减少总行数，只降低单方法长度。
- 实施结果：`ConfigureLayout` 已拆为 `BuildMainLayout` / `BuildHeader` / `BuildDetails` / `BuildOverlay` / `BuildOverlayColorSelector` / `BuildFooter`；主文件 429 行，diff 净增 58 行，`ConfigureLayout` 约 15 行。颜色区修复了真实溢出：旧 overlay 内容区实际约 95px，却固定了 88+26=114px，并且 FlowLayoutPanel 默认 Margin 再裁掉预览约 2px；新布局用 Percent 行 + TableLayoutPanel，预览填满、Remove overlay 按钮固定 40px 垂直居中。`No overlay` 改为 `Remove overlay`，无颜色时禁用，并补充无障碍文本。另补 `AutoScaleDimensions` + `OnLoad` 按 `DeviceDpi / 96` 缩放，修复 200% DPI 下提示行和整窗裁切。192 DPI 离屏探针确认预览、按钮、提示文字均在父容器内。

### B2 `DrawerWindow.FileList` 的图像缓存/视口加载

- 位置：`DrawerWindow.FileList.cs:15-30`（字段）、`369-604`（视口请求、图像加载、释放、代际判断）。
- 现象：列表编排与图像所有权耦合在一个 604 行 partial 中；取消/代际/释放语义是当前最复杂的部分。
- 建议：提取 `DrawerItemImageCache`，拥有 `_loadedImagesByIndex`、占位图、`SemaphoreSlim`、图像代际和释放逻辑；窗口只负责“请求哪些索引”和把图像写入 `ImageList`。
- 预计减少：文件行数约 180–220 行；**总代码量可能持平或略增**（新类有字段/方法边界）。收益是可读性和可测试性。
- 风险：中到高（必须保持取消、过期请求、ImageList 绑定顺序和 Dispose 顺序）。
- 讨论点：如果目标是显著减少总行数，这一项不应优先；如果目标是让文件列表可维护，值得做。
- 实施结果：新增 `Drawers/DrawerItemImageCache.cs`（216 行）；`DrawerWindow.FileList.cs` 394 行，diff 净减少 160 行，B2 净增约 56 行。缓存拥有占位图、已加载图像、请求索引、信号量、取消源和图像代际；窗口只负责可见范围、ImageList 绑定和占位图回退。`DrawerWindow.Designer.Dispose` 改为 `_imageCache.Dispose()`；增加“已在缓存中的索引不再重复加载”和 ImageList 添加失败时回滚缓存的保护。缩略图、滚动、取消和窗口关闭释放仍需手工回归。

### B3 `MainWindow.DrawerList` 的绘制与拖动重排

- 位置：`MainWindow.DrawerList.cs` 全文件 371 行；绘制项、选择、拖动重排、插入标记混在一起。
- 建议：拆成 `MainWindow.DrawerList.Paint.cs` 与 `MainWindow.DrawerReorder.cs`；或把重排索引计算提取为纯逻辑 helper，便于测试。
- 预计减少：0 行（文件拆分）；风险：中（拖动/绘制行为需回归）。
- 讨论点：当前是否真的难以维护；如果只是文件长，拆分可能只是把复杂度搬走。
- 实施结果：已拆为 `MainWindow.DrawerList.cs`（113 行）、`MainWindow.DrawerList.Paint.cs`（54 行）、`MainWindow.DrawerReorder.cs`（199 行）；拖动状态字段从 `MainWindow.cs` 移入重排文件（−13 行）。diff 净减少 8 行，行为不变。拖动重排、插入标记和右键菜单仍需手工回归。

### B4 `SettingsRepository` 后台写盘与状态机

- 位置：`SettingsRepository.Persistence.cs:12-49`（FlushAsync）、`51-93`（SavePendingChanges）、`95-230`（BackgroundWriterLoop）。
- 现象：`_dirty/_writeInProgress/_flushTcs` 状态机约 220 行；对一个小工具来说偏重，但承担了防抖、退出 flush、失败保留和原子写入。
- 建议：**暂不重构**。若一定要简化，可评估单一 `SaveAsync` + 防抖 + 退出时同步 flush，但必须保留失败语义；预计减少 80–120 行，风险高。
- 讨论点：你更看重“代码少”还是“配置写入可靠”；我不建议为了行数牺牲后者。
- 本轮结论：未改动；保留现有后台写入、防抖、退出 flush 和失败语义。

### B5 `ThemePalette` 的 HSL 往返与派生色

- 位置：`ThemePalette.cs:48-133` 及 HSL 辅助方法。
- 现象：从 `#RRGGBB` 转 HSL，再按亮度偏移生成背景/卡片/强调色；派生规则较多。
- 建议：若要减少代码，可改为直接 RGB 混合/固定偏移，减少 HSL 转换；但会改变实际配色。
- 预计减少：约 50–80 行；风险：高（视觉回归、深浅色对比度）。
- 讨论点：是否接受视觉变化；如果只想统一规则，当前实现可以保留。
- 实施结果：已删除 `ToHsl` / `FromHslInternal` / `HueToRgb` / `AdjustLightness`，改为直接 RGB 混色，并按感知亮度灰去饱和；`ThemePalette.cs` 229 → 158 行，diff 净减少 51 行。默认 `#000533` 的窗口背景会比原 HSL 派生更中性、少一点蓝色；这是已接受的视觉变化，需在 UI 中确认深浅色对比度。

### B6 `SettingsForm.Designer` / `DrawerWindow.Designer`

- 位置：`SettingsForm.Designer.cs` 420 行、`DrawerWindow.Designer.cs` 234 行。
- 建议：保留生成代码，不手改以“减少行数”；Designer 行数不代表维护复杂度。
- 讨论点：无，除非你希望彻底重做设置页布局。
- 本轮结论：未改动；保留 Designer 生成代码。

## 4. C 组：高风险或收益不明确，不建议默认实施

### C1 移除 SunnyUI 依赖（已完成）

- 迁移前现状：约 88 处引用，集中在 `SettingsForm.Designer.cs`、`DrawerSettingsDialog.cs`、`MainWindow.Designer.cs`、`DrawerWindow.Designer.cs`。
- 原判断：移除依赖 ≠ 减少代码；原生控件需要自己实现主题、滚动条和布局，且会改变视觉。该判断仍适用于评估“是否值得为减行数重写 UI”。
- 实施结果：按独立测试分支完成全量移除；当前 SunnyUI 源码/包引用为 0，新增 `DrawerListBox`、`ThemedScrollBar`、`ColorPreviewPanel`、`BehaviorCheckBoxGroup` 等项目内控件。
- 详细过程、视觉变化和回归项见 [`SUNNYUI_REMOVAL_IMPACT.md`](SUNNYUI_REMOVAL_IMPACT.md)。
- 风险：高；视觉/DPI/滚动行为仍需持续人工回归。

### C2 改用虚拟列表/重写缩略图管线

- 判断：`ListView` 当前非 VirtualMode，条目最多 200（10.2 后），视口图像已延迟加载；改虚拟模式会引入新状态和回归面。
- 建议：不做。

### C3 改为同步保存/移除后台 writer

- 判断：会简化配置层，但 UI 可能卡顿，退出/失败语义要重写；与“可靠的小工具”目标冲突。
- 建议：不做，除非你明确接受简化后的保存语义。

### C4 合并 partial 文件以减少文件数

- 判断：文件数不是主要问题；合并会让单文件更长，阅读更差。
- 建议：不做；只在不增加单文件复杂度时拆/并。

## 5. 预估收益汇总

| 组 | 内容 | 实际/预计行数变化 | 风险 | 建议 |
|---|---|---:|---|---|
| A1–A9 | 字体/按钮/颜色/拖动/DPI/包装/重复分支/一致性 | 已有文件净减少 162 行；新增共享类 131 行，整体约减少 31 行 | 低 | 已完成 |
| B1 | 拆分抽屉设置布局 | diff 净 +58；含颜色区/DPI 修复 | 中 | 已完成，需 UI 回归 |
| B2 | 提取图像缓存 | FileList −160；新增缓存 216；净 +56 | 中高 | 已完成，需 UI 回归 |
| B3 | 拆分抽屉列表绘制/重排 | diff 净 −8（含字段移动） | 中 | 已完成，需拖动回归 |
| B4 | 简化配置 writer | — | 高 | 本轮不做 |
| B5 | 移除 HSL 派生 | diff 净 −51 | 高（视觉变化） | 已完成，需视觉确认 |
| C1 | 移除 SunnyUI | 已实施；代码量可能增加 | 高 | 已完成，见迁移记录 |

结论：A 组整体约减少 31 行；B1/B2/B3/B5 合计约增加 55 行（git diff：B1 +58、B2 +56、B3 −8、B5 −51）。B 组收益是职责分离和可读性，而不是缩小代码库。如果目标是显著缩小代码库，应单独讨论“删功能/降配置复杂度”，例如是否保留拖放、主题派生、抽屉专属颜色、全局热键、托盘常驻等。

## 6. 建议的逐项讨论顺序

1. ~~A1–A9~~（已完成，见第 2 节）
2. ~~B1/B2/B3/B5~~（已完成，见第 3 节；需手工 UI 回归）
3. B4 配置 writer（本轮不做）
4. B6 Designer（保留，不做）
5. 其余 C 组项逐项确认“做/不做”

## 7. 状态与待确认问题

1. A1–A9 已全部实施；A9 采用合并 `ApplySettingsSnapshot` 的低风险方案，未做批量注释翻译。A4 的鼠标捕获/DPI 行为仍需人工回归。
2. B1/B2/B3/B5 已实施；B1 的 100%/150%/200% UI、B2 的缩略图/滚动/取消、B3 的拖动重排、B5 的配色对比度仍需手工回归；B1 已在 192 DPI 离屏探针验证颜色区边界。
3. B4/B6 本轮明确不做；是否仍要讨论“删功能”以显著减少代码量？如果愿意，哪些功能可以放弃（拖放、主题派生、抽屉颜色、热键、托盘常驻等）？
4. C1 已完成；C2/C3/C4 维持不做。

验证：Debug 与 Release 完整构建（使用 `-o %TEMP%`，因运行中的 winDrawer 锁定 `bin\Debug`）均为 0 警告 0 错误；B1 使用 192 DPI 离屏探针确认颜色预览、Remove overlay、提示文字均在父容器内；未执行 UI 自动化测试，B2/B3/B5 与 B1 其他 DPI 的人工回归尚未完成。

