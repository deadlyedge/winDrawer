# 阶段 10 代码审查清单

> 目的：为 10.3 的逐项讨论提供依据。本文只记录审查结果，**不代表已修改代码**。
>
> 审查范围：`src/` 下全部手写 C#（不含 Designer 生成代码），以及 10.1/10.2 新增/修改的代码。工作树中的 `docs/TODOs.md` 为用户改动，本文未修改它。

## 1. 当前规模

- 46 个 C# 文件，约 8,958 行；其中 Designer 约 846 行，手写代码约 8,112 行。
- 最大文件：
  - `DrawerWindow.FileList.cs` 604 行
  - `SettingsForm.cs` 542 行
  - `DrawerSettingsDialog.cs` 525 行
  - `MainWindow.cs` 514 行
  - `SettingsForm.Designer.cs` 420 行
  - `DrawerWindow.cs` 379 行
  - `MainWindow.DrawerList.cs` 371 行
  - `SettingsRepository.Drawers.cs` 353 行
  - `SettingsRepository.Persistence.cs` 338 行

需要先说明一个判断：这 9,000 行里真正“重复、可机械删除”的部分有限。大量代码来自 WinForms 布局、DPI、主题派生、延迟缩略图、拖放、配置原子写入和托盘生命周期。**仅靠重构很难把代码量减半；若目标是大幅缩减，更有效的是减少功能/配置项，而不是继续抽象。**

## 2. A 组：低风险，建议优先讨论

### A1 两个设置窗体的品牌字体生命周期重复

- 位置：
  - `SettingsForm.cs:15-19`（字段）、`34-40`（初始化）、`104-144`（创建/应用/释放）
  - `DrawerSettingsDialog.cs:13-16`、`52-55`、`95-113`、`115-130`
- 现象：两边都各自持有 `_ownedFonts`、创建 `BrandingFont.Create(10.5F/13F)`、遍历控件应用、在 `Dispose` 中释放。
- 建议：提取一个很小的 `DialogFontSet`（系统字体、按钮字体、标题字体 + `Dispose` + `ApplyTo(Control)`）。不引入基类，不改变控件布局。
- 预计减少：约 35–45 行；风险：低。
- 讨论点：是否接受新增一个约 60 行的小类，换取两个窗体各减少约 20 行，并统一释放顺序。

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

### A3 颜色对话框配置与调用重复

- 位置：
  - `SettingsForm.cs:300-325`
  - `DrawerSettingsDialog.cs:476-525`
  - `UiDialogHelpers.cs` 已有自定义色和 Enter/Space 激活
- 现象：两边都设置 `AllowFullOpen/FullOpen/AnyColor/SolidColorOnly`，都调用 `BuildColorDialogCustomColors`；一边用局部 `ColorDialog`，一边长期持有。
- 建议：在 `UiDialogHelpers` 增加 `ShowColorDialog(owner, initial, secondary)`，返回 `Color?`；调用方只处理结果。保留 `HandleActivationKey`。
- 预计减少：约 25–35 行；风险：低。
- 讨论点：是否接受 helper 内部创建/释放 `ColorDialog`；这比当前 `DrawerSettingsDialog` 长期持有一个对话框更简单。

### A4 无边框设置窗体的拖动逻辑重复

- 位置：
  - `SettingsForm.cs:21-22`、`225-237`、`511-541`
  - `DrawerSettingsDialog.cs:32-33`、`411-440`
- 现象：两个窗体各自维护 `_isDragging/_dragOffset`，鼠标按下/移动/抬起逻辑几乎相同；`DrawerSettingsDialog` 额外使用 `Capture`。
- 建议：只对设置窗体提取 `FormDragBehavior`（绑定 header 控件）；主窗口拖动有锁定语义，不纳入。
- 预计减少：约 30–40 行；风险：低到中（鼠标捕获和 DPI 行为需手工验证）。
- 讨论点：是否值得为两个窗体引入一个行为类；如果只保留一处，优先保留 `SettingsForm` 的简单实现。

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

### A6 平凡包装/转发方法

- 位置：
  - `DrawerWindow.cs:143-146` `BlendColor` 仅转发 `ThemePalette.BlendOverlayColor`
  - `DrawerWindow.FileList.cs:50-53` `RefreshDrawerAsync` 仅转发 `PopulateFileListAsync`
  - `SettingsForm.cs:155-158` `ConfigureButton` 仅转发 `UiButtonTheming.ApplyColorPalette`
- 建议：删除包装，调用点直接用目标方法；`RefreshDrawerAsync` 统一命名为 `PopulateFileListAsync` 或反向重命名。
- 预计减少：约 10–15 行；风险：低。
- 讨论点：是否保留 `RefreshDrawerAsync` 作为语义名称（“用户刷新”）而把 `PopulateFileListAsync` 改名，避免两个名字。

### A7 主窗口清空选择状态的重复分支

- 位置：`MainWindow.Drawers.cs:133-151`
- 现象：`InvokeRequired` 的两个分支写了同一段清空选中逻辑。
- 建议：统一为一个本地方法，必要时 `BeginInvoke` 一次；或直接使用 `BeginInvoke`（窗体句柄已存在）。
- 预计减少：约 10–15 行；风险：低。
- 讨论点：是否要保留同步路径以避免关闭时的竞态；需要确认事件触发上下文。

### A8 透明度范围硬编码

- 位置：`SettingsSnapshot.cs:25` 使用 `Math.Clamp(windowOpacity, 0, 100)`，而 `AppConstants.Theme` 已有 `OpacityMin/OpacityMax`。
- 建议：改用常量。
- 预计减少：0 行；风险：无；属于一致性修正。
- 讨论点：是否纳入“顺手清理”。

### A9 命名与注释一致性

- 位置/现象：
  - `MainWindow.cs:419-447` 的 `ApplySnapshot` 只是赋值后调用 `ApplySettings`，两个名字语义接近；
  - `MainWindow.cs:439`、`MainWindow.Drawers.cs:136` 有中文注释，其它代码注释为英文；
  - 两个类都有 `ConfigureButton`，语义不同。
- 建议：只重命名确实容易混淆的成员；不批量翻译注释。
- 预计减少：0 行；风险：低。
- 讨论点：是否把 `ApplySettings` 改为 `ApplySettingsSnapshot`，把 `ApplySnapshot` 合并进去；这会触及主窗口多处调用，需单独确认。

## 3. B 组：中等风险，先讨论再决定

### B1 `DrawerSettingsDialog` 的代码布局过长

- 位置：`DrawerSettingsDialog.cs:132-234`（`ConfigureLayout`，约 103 行）、`254-311`（`ConfigureOverlayGroup`，约 58 行）、`312-357`（`ApplyStyle`，约 46 行）。
- 现象：一个方法里同时创建 header、字段、路径、颜色区、按钮区和行高；阅读时需要来回跳。
- 建议：按区域拆成 `BuildHeader/BuildDetails/BuildOverlay/BuildFooter`，把 `CreateLabel/FindLabels` 等通用控件工厂移到小的 UI helper。**不改成 Designer**，避免生成代码增加。
- 预计减少：约 30–50 行；风险：中（布局/DPI 需手工验证）。
- 讨论点：目标是“可读”还是“行数”；拆方法可能不减少总行数，只降低单方法长度。

### B2 `DrawerWindow.FileList` 的图像缓存/视口加载

- 位置：`DrawerWindow.FileList.cs:15-30`（字段）、`369-604`（视口请求、图像加载、释放、代际判断）。
- 现象：列表编排与图像所有权耦合在一个 604 行 partial 中；取消/代际/释放语义是当前最复杂的部分。
- 建议：提取 `DrawerItemImageCache`，拥有 `_loadedImagesByIndex`、占位图、`SemaphoreSlim`、图像代际和释放逻辑；窗口只负责“请求哪些索引”和把图像写入 `ImageList`。
- 预计减少：文件行数约 180–220 行；**总代码量可能持平或略增**（新类有字段/方法边界）。收益是可读性和可测试性。
- 风险：中到高（必须保持取消、过期请求、ImageList 绑定顺序和 Dispose 顺序）。
- 讨论点：如果目标是显著减少总行数，这一项不应优先；如果目标是让文件列表可维护，值得做。

### B3 `MainWindow.DrawerList` 的绘制与拖动重排

- 位置：`MainWindow.DrawerList.cs` 全文件 371 行；绘制项、选择、拖动重排、插入标记混在一起。
- 建议：拆成 `MainWindow.DrawerList.Paint.cs` 与 `MainWindow.DrawerReorder.cs`；或把重排索引计算提取为纯逻辑 helper，便于测试。
- 预计减少：0 行（文件拆分）；风险：中（拖动/绘制行为需回归）。
- 讨论点：当前是否真的难以维护；如果只是文件长，拆分可能只是把复杂度搬走。

### B4 `SettingsRepository` 后台写盘与状态机

- 位置：`SettingsRepository.Persistence.cs:12-49`（FlushAsync）、`51-93`（SavePendingChanges）、`95-230`（BackgroundWriterLoop）。
- 现象：`_dirty/_writeInProgress/_flushTcs` 状态机约 220 行；对一个小工具来说偏重，但承担了防抖、退出 flush、失败保留和原子写入。
- 建议：**暂不重构**。若一定要简化，可评估单一 `SaveAsync` + 防抖 + 退出时同步 flush，但必须保留失败语义；预计减少 80–120 行，风险高。
- 讨论点：你更看重“代码少”还是“配置写入可靠”；我不建议为了行数牺牲后者。

### B5 `ThemePalette` 的 HSL 往返与派生色

- 位置：`ThemePalette.cs:48-133` 及 HSL 辅助方法。
- 现象：从 `#RRGGBB` 转 HSL，再按亮度偏移生成背景/卡片/强调色；派生规则较多。
- 建议：若要减少代码，可改为直接 RGB 混合/固定偏移，减少 HSL 转换；但会改变实际配色。
- 预计减少：约 50–80 行；风险：高（视觉回归、深浅色对比度）。
- 讨论点：是否接受视觉变化；如果只想统一规则，当前实现可以保留。

### B6 `SettingsForm.Designer` / `DrawerWindow.Designer`

- 位置：`SettingsForm.Designer.cs` 420 行、`DrawerWindow.Designer.cs` 234 行。
- 建议：保留生成代码，不手改以“减少行数”；Designer 行数不代表维护复杂度。
- 讨论点：无，除非你希望彻底重做设置页布局。

## 4. C 组：高风险或收益不明确，不建议默认实施

### C1 移除 SunnyUI 依赖

- 现状：约 88 处引用，集中在 `SettingsForm.Designer.cs`（40 处）、`DrawerSettingsDialog.cs`（13 处）、`MainWindow.Designer.cs`（12 处）、`DrawerWindow.Designer.cs`（9 处）。
- 判断：移除依赖 ≠ 减少代码。原生控件需要自己实现主题、圆角、滚动条和布局，代码量可能增加；SunnyUI 也是当前视觉的一部分。
- 建议：不作为 10.3 的默认项；除非目标是“减少第三方依赖”而不是“减少行数”。
- 风险：高。

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

| 组 | 内容 | 预计行数变化 | 风险 | 建议 |
|---|---|---:|---|---|
| A1–A9 | 字体/按钮/颜色/拖动/DPI/包装/重复分支/一致性 | 约减少 120–180 行 | 低 | 优先讨论 |
| B1 | 拆分抽屉设置布局 | 约减少 30–50 行 | 中 | 可做，需 UI 回归 |
| B2 | 提取图像缓存 | 文件减少约 200 行，总量持平 | 中高 | 按“可维护性”决定 |
| B3 | 拆分抽屉列表绘制/重排 | 0 行 | 中 | 仅当当前难维护 |
| B4 | 简化配置 writer | 减少 80–120 行 | 高 | 不建议 |
| B5 | 简化 ThemePalette | 减少 50–80 行 | 高 | 需接受视觉变化 |
| C1 | 移除 SunnyUI | 可能增加 | 高 | 不建议默认做 |

结论：**安全清理的现实收益约 120–180 行（约 1.5–2%）**，不是“减半”。如果目标是显著缩小代码库，应单独讨论“删功能/降配置复杂度”，例如是否保留拖放、主题派生、抽屉专属颜色、全局热键、托盘常驻等。

## 6. 建议的逐项讨论顺序

1. A1 字体、A2 按钮、A3 颜色对话框（三处最明确的重复）
2. A4 拖动、A5 DPI、A6 包装、A7 重复分支
3. A8/A9 一致性
4. B1 布局拆分
5. B2 图像缓存（只在你认为文件列表难维护时）
6. 其余 B/C 项逐项确认“做/不做”

## 7. 待你确认的问题

1. A1–A9 是否全部纳入？还是只选 A1/A2/A3？
2. 是否接受“总代码量不降、但可读性提升”的 B1/B2？
3. 是否愿意讨论“删功能”以显著减少代码量？如果愿意，哪些功能可以放弃（拖放、主题派生、抽屉颜色、热键、托盘常驻等）？
4. B4/B5/C1 是否明确暂缓？

