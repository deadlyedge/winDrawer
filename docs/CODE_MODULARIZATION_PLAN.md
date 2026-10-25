# winDrawer 代码拆分建议

> 目的：先把文件列表与抽屉窗口相关职责拆成易读、可分别验证的小模块，再开始实施 [`DP_STAGE_7.md`](DP_STAGE_7.md) 的阶段 7.1/7.2。本文是拆分方案，不表示代码已按此结构修改。
>
> 范围：优先处理 `DrawerWindow` 的文件列表管线。继续使用当前单个 WinForms 项目、BCL 和项目内轻量控件；SunnyUI 已在后续迁移中移除。不新增项目、不引入 MVVM、依赖注入框架或大型缓存层。`docs/TODOs.md` 和 `docs/DP_STAGE_7.md` 中已有的工作均应保留。
>
> **实施状态（本次已落地）：** 步骤 A–D 已按本方案完成——`DrawerWindow` 改为 partial 并新增 `DrawerWindow.FileList.cs`（列表编排、视口延迟加载、ImageList 所有权），业务逻辑拆到 `Drawers/DrawerItemInfo.cs`、`Drawers/DrawerItemEnumerator.cs`、`Drawers/DrawerItemImageLoader.cs`，`ScrollableListView` 增加 `TryGetVisibleItemRange`。排序采用已确认的“目录与文件全体混排、`LastWriteTimeUtc` 降序”契约，并已同步 [`DP_STAGE_7.md`](DP_STAGE_7.md)。时间排序、枚举降级、缩略图回退与源文件解锁、可见范围查询已用 `%TEMP%` 下的临时 smoke harness 验证（22 项通过）；大目录滚动、深浅主题、DPI 切换和双击打开等人工 UI 回归尚未执行。
>
> **附加拆分（同一约定，纯机械搬运）：** `MainWindow.cs` 由 1216 行拆为 `MainWindow.cs`（窗体生命周期、品牌资源、拖动/锁定、窗口边界、主题、设置快照应用，381 行）与 `MainWindow.DrawerList.cs`（抽屉列表绘制、选择切换、拖动重排与拖放提示）、`MainWindow.Drawers.cs`（打开/编辑/删除/添加抽屉）、`MainWindow.Settings.cs`（设置窗体协调、默认值重置与退出流程）、`MainWindow.Tray.cs`（托盘初始化与显示/隐藏）。成员签名集合与拆分前完全一致，未改动任何行为。
>
> **配置层拆分（同一约定）：** `SettingsRepository.cs` 由 1198 行拆为 `SettingsRepository.cs`（仓储状态、启动项读写、快照访问与通用 TOML/路径助手，225 行）、`SettingsRepository.Drawers.cs`（抽屉增删改、排序与抽屉表助手，312 行）、`SettingsRepository.Persistence.cs`（后台写盘循环、保存/刷新/释放与文件读写，368 行）、`SettingsRepository.Serialization.cs`（TOML 序列化与分区注释格式，129 行）、`SettingsSnapshot.cs`（设置快照模型及其 TOML 解析，185 行）。同为逐行搬运：成员签名集合与拆分前一致，除类声明加上 `partial` 外无代码行改动；临时 smoke harness 的配置读写检查（32 项）在拆分后全部通过。

## 1. 当前结构与主要问题

### 1.1 项目形态

- 应用只有一个 `net10.0-windows` WinForms 项目；SDK 默认包含项目目录中的 C# 文件，不需要为新增 `.cs` 文件手动登记项目文件。
- 目前已有 `Configuration`、`Theming` 子目录；主界面、抽屉窗体、托盘控制器和字体管理仍在单项目内。
- `ScrollableListView` 已是独立控件，负责 Win32 ListView 滚动范围、抑制横向滚动条、处理视口变化和 `ViewportChanged` 通知；当前实现保留原生垂直滚动条用于滚轮/滚动信息，但通过加宽 ListView 将其裁剪到宿主外侧；它不是目录业务模型。

### 1.2 `DrawerWindow` 的职责集中

`src/DrawerWindow.cs` 当前同时包含以下内容：

1. 窗体初始化、主题应用、DPI 和窗口尺寸保存。
2. 目录路径入口、刷新按钮、双击交给 Windows Shell 打开。
3. 直接子项枚举、名称排序、加载取消/generation 管理和错误提示。
4. ListView 项构造、状态行、ImageList 更新。
5. Shell 图标、图片缩略图生成及图像资源释放。
6. ListView 视口变化、独立滚动条同步。

这些工作流都合理，但集中在同一个窗体类后，单独调整排序或图标加载时很容易连带碰到 UI 线程、刷新取消、DPI 和图像所有权。阶段 7.1/7.2 正好会改动其中最容易互相影响的“枚举/排序”和“图标/缩略图”部分，因此宜先明确拆分边界。

### 1.3 现有难点应保持可见

- 当前顺序是目录先按名称排序、再显示文件（文件也按名称排序）。阶段七文档中出现了“全体按时间混排”和“目录/文件分别排序”的不同描述；实施前必须统一为一种明确契约。
- 当前刷新流程已有 `CancellationTokenSource` 和 generation 防止过期目录枚举结果写回；拆分不得绕开或复制出第二套过期判断。
- 当前图片/图标对象需要由 ImageList 与窗体共同协调释放；不能将仍由控件使用的 `Image` 提前 Dispose，也不能让取消的后台任务把图像对象留在内存中。
- `ScrollableListView` 的滚动与视口通知依赖原生消息和图标视图布局，不应与目录枚举、文件日期排序合并成一个“通用列表服务”。

## 2. 推荐的目标结构

先保持一个应用项目，按职责新增文件即可：

```text
src/
  Configuration/
    ...
  Drawers/
    DrawerItemInfo.cs              # 直接子项的不可变元数据
    DrawerItemEnumerator.cs        # 直接子项枚举、元数据读取、稳定排序
    DrawerItemImageLoader.cs       # 单条目 Shell 图标/图片缩略图生成
    LoadedDrawerItemImage.cs       # 可选：索引与 Image 的加载结果
  Theming/
    ShellIcon.cs
    ThemePalette.cs
  DrawerWindow.cs                  # 窗体生命周期、主题、窗口行为
  DrawerWindow.FileList.cs         # 文件列表 UI 编排、取消、视口加载、ImageList 所有权
  ScrollableListView.cs            # 原生列表滚动和可视区查询
```

如果 `Drawers` 文件夹中的类型沿用现有 `winDrawer` 命名空间也可以；若采用 `winDrawer.Drawers`，只在业务模型/服务间使用该命名空间，避免为一个小功能重排整个项目的命名空间。

## 3. 模块职责建议

### 3.1 `DrawerWindow.cs`：窗体与交互入口

保留：

- Designer 初始化及窗体生命周期。
- DPI、窗口边框/尺寸、主题应用。
- 路径栏打开文件夹、刷新按钮事件、列表项双击打开。
- 对目录加载错误显示窗体级提示。

移出：条目排序算法、完整目录枚举实现、缩略图像素处理和逐项图像资源管线。

`DrawerWindow` 仍可作为文件列表 UI 的拥有者和协调者；拆分目标不是让窗体“零逻辑”，而是让它不再同时实现文件系统、图像生成和列表消息细节。

### 3.2 `DrawerItemInfo.cs`：纯元数据

建议使用内部不可变 record，字段至少为：

- `Path`
- `Name`
- `IsDirectory`
- `LastWriteTimeUtc`（可空；读取失败时为空）

该模型不包含 `Image`、`ListViewItem`、`ImageList` 或窗体引用。`Tag` 可保存此记录，双击时仍由 `DrawerWindow` 负责通过 Shell 打开路径。

### 3.3 `DrawerItemEnumerator.cs`：目录枚举和确定性排序

职责：

- 只枚举所选目录的**直接子项**，继续使用 `Directory.EnumerateFileSystemEntries`，不递归。
- 对每个条目读取属性、名称及 `LastWriteTimeUtc`；一个文件消失、拒绝访问或时间戳读取失败时隔离该项或标记缺失时间并记录诊断，不中断其他条目。
- 在同一地方实现排序，并返回已排序的 `List<DrawerItemInfo>`。
- 令牌在目录迭代和排序前/后检查；比较器内不要抛取消异常，以免造成排序实现处于不确定状态。

建议把比较器做成 `internal static` 的纯逻辑方法，方便临时 smoke harness 或后续小型测试验证，而不需要启动 WinForms 窗口。

**排序契约需先确定：** [`DP_STAGE_7.md`](DP_STAGE_7.md) 的说明包含“目录/文件混排”和“目录与文件分组”的两种表述。建议以最新明确需求为准。如果选“全体混排”，按 `LastWriteTimeUtc` 降序；缺失时间排在有时间的条目之后；日期相同时按名称 `OrdinalIgnoreCase` 升序，再按完整路径 `Ordinal` 升序。如果选“目录在前”，则先比较目录/文件组，再应用相同的日期和稳定名称顺序。无论选哪一种，代码、测试和计划验收必须采用同一契约。

### 3.4 `DrawerItemImageLoader.cs`：单条目图像生成

职责：

- 输入 `DrawerItemInfo`、目标 DPI 图像尺寸和 `CancellationToken`，返回一张**由调用方拥有**的 `Image`。
- 目录、非图片文件使用现有 `ShellIcon`；支持的图片先尝试缩略图，失败时回退 Shell 图标。
- 保留当前缩略图用 `FileStream` 读取并立即绘制到独立 Bitmap 的做法；不得让返回图像继续引用输入文件流。
- 单个条目在图像读取期间被删除或变成不可访问时回退到通用图标/占位，不把一个坏条目升级成整个目录刷新失败。
- 加载过程中以及返回图像后，所有权必须清楚：成功返回的 `Image` 由 UI 协调层 Dispose；失败/取消且尚未返回的 Bitmap 在 loader 内 Dispose。

不建议此类维护无上限的静态缓存。阶段 7.2 的第一版用一个受限的“当前视口 + 预取区”缓存即可；离开预取区就从 ImageList 移除并释放。

### 3.5 `DrawerWindow.FileList.cs`：ListView 编排与异步生命周期

推荐采用 `public partial class DrawerWindow` 放在新文件中，先让相关私有 UI 控件继续由窗体拥有，避免为了解耦而暴露一堆控件属性。

负责：

- 刷新：取消旧工作、增加 generation、调用枚举器、在 UI 线程显示状态和完整项目列表。
- 建立所有 `ListViewItem` 和共享占位图；首次列项时不为每个条目生成缩略图或 Shell 图标。
- 使用 `ScrollableListView.ViewportChanged` 进行短暂防抖，计算可视范围并附带少量预取项。
- 在后台线程串行或小批量调用 `DrawerItemImageLoader`；UI 线程再次校验 generation、取消状态和目标索引仍在当前预取集合，再更新 ImageList 和 ListViewItem。
- 管理 ImageList 中图像的所有权，移出预取集合、刷新、DPI 变化、关闭和 Dispose 时按序解绑、移除并释放。
- 单独维持图像加载请求 token/generation；不得把刷新元数据的 token 和滚动图像加载 token 混成一个状态，否则快速滚动会错误地取消目录列表本身。

强烈建议使用单线程或有限批次加载，不要对几千条图片同时启动 `Task.Run`。可用 4–8 项的小批次；滚动变化时取消当前批次，返回 UI 后丢弃旧代结果。

### 3.6 `ScrollableListView.cs`：原生视口与滚动

继续只负责 ListView 的 Win32 滚动、横向滚动条抑制、视口通知和（当前实现下的）原生垂直条裁剪配合。若阶段 7.2 需要 `TryGetVisibleItemRange`，该方法可加在此类中，但要保证：

- 仅在 ListView 已创建句柄、未 Dispose、尺寸非零时查询。
- 图标视图应处理项目之间空白、图标排布方向和 DPI；不应假设连续索引区间在所有 ListView 布局模式下都等价于几何可视区。
- 不在视口查询中逐项创建图标或访问文件系统。
- 如果原生命中测试无法可靠给出精确集合，可取“可视行/列映射出的保守索引区间”，允许少量额外预取但不能漏掉可见条目。

## 4. 建议实施步骤与每步边界

### 步骤 A：机械拆文件，不改行为

1. 将 `DrawerWindow` 声明改成 `partial`。
2. 新建 `DrawerWindow.FileList.cs`，移动列表枚举/应用/清理/状态行/取消生命周期与滚动条同步方法，以及专属字段/私有 record。
3. `DrawerWindow.cs` 只保留窗口初始化、主题、DPI/尺寸、路径入口、双击打开和按钮事件转发。
4. 这一步只搬代码，不顺便改排序、异步模式或资源所有权；先构建、核对 git diff，再继续。

**验收：** Debug 构建通过；列表刷新、空目录、缺失目录、双击打开、滚动条和 Dispose 行为与拆分前一致。

### 步骤 B：抽出条目模型、枚举与排序

1. 新建 `DrawerItemInfo.cs` 和 `DrawerItemEnumerator.cs`。
2. 将元数据收集和排序从窗体 partial 文件移出；此步骤执行阶段 7.1 的全体混排或分组策略（实施前统一语义）。
3. 用临时目录设置文件和目录的 UTC 修改时间；验证降序、同一时间名称/路径 tie-break、无有效日期的降级顺序以及刷新稳定性。

**验收：** 读取不再因日期读取失败丢弃整批条目；没有递归；排序可独立验证。

### 步骤 C：抽出单条目图像生成

1. 新建 `DrawerItemImageLoader.cs`，逐步搬入支持格式判断、Shell 图标/缩略图回退、尺寸辅助方法。
2. 保持同一 DPI 目标图像尺寸；Shell icon、输入流及 Bitmap 所有权有单一、明确的释放方。
3. 用不存在/不支持/损坏/锁定但可共享的临时图片分别验证回退和源文件可重新打开。

**验收：** 缩略图成功时为独立 Bitmap；失败时返回可显示的 Shell/占位图；没有未释放的 Icon、Stream 或 Image。

### 步骤 D：接入视口延迟加载

1. 先显示全量排序条目和共享占位图；将 `ScrollableListView.ViewportChanged` 防抖接到文件列表 partial。
2. 只请求可视区及一屏预取范围的图片，每批限量处理；完成后回 UI 线程更新 ImageList。
3. 滚出预取区即把条目 `ImageKey` 改回占位键，再移除对应图像、最后 Dispose；确保控件不再引用待释放图像。
4. 刷新、DPI 改变和窗体真正关闭时：先取消后台图像工作并提高 generation，随后清除条目引用、ImageList 和自有图像；若 FormClosing 被取消，不应永久取消仍在显示的窗口请求。
5. worker 只接收已捕获的条目元数据、图像尺寸和令牌；不要从 worker 线程读写 WinForms 控件、ImageList 或可变窗体字段。

**验收：** 初次加载只枚举/排序元数据并显示完整占位列表；滚动到新区域才生成图标；快速滚动/刷新/DPI/关闭均不产生旧结果回写；缓存大小受预取集合限制。

### 步骤 E：阶段验收与回归

- 单独运行时间排序 smoke checks 和图像回退/释放 smoke checks。
- 执行 Debug、Release 构建；发布验证输出到新临时目录，不覆盖现有 `bin/`、`obj/` 或发布目录。
- 人工检查 Windows 大目录、深浅主题、DPI 切换、滚动方向、双击打开、刷新时删除条目、快速关闭/重开。
- 阶段 7.1/7.2 验收完成后，再进入 7.3–7.6；不要在模块化和 7.1/7.2 尚未稳定时提前删改 `docs/TODOs.md` 中其它待办。

## 5. 不建议的拆法

- 不为每个 helper 新建一个项目或引入 DI 容器。
- 不把 `ListViewItem`、`ImageList`、窗体和主题塞进 `DrawerItemInfo`。
- 不让枚举器控制 WinForms 控件，也不让图片 loader 决定 ListView 排序。
- 不把文件系统 I/O 放进排序 comparer。
- 不用 `VirtualMode` 作为第一步；当前为 `View.LargeIcon`，系统 `ListView` 已负责布局/滚动，先实现“全量轻量元数据 + 视口图片延迟加载”。
- 不同时重构 `MainWindow`、`SettingsRepository`、设置窗体或 Shell 启动逻辑；它们与 7.1/7.2 的直接收益无关。

## 6. 需要保持的当前工作

- 本次制定方案期间 `docs/TODOs.md` 已有用户未提交改动；代码拆分期间不得覆盖或格式化该文件。
- `docs/DP_STAGE_7.md` 是阶段七计划来源；实施时应同步其中的排序契约，并消除“混排”和“目录分组”两种表述冲突。
- 只在对应阶段验收完成后更新阶段状态；不要把文档计划或临时 harness 描述成已完成的 UI 手工测试。

> **当前目录结构（阶段 7.6 整理后）：** `src/Program.cs`；`src/Forms/`（`MainWindow.*`、`DrawerWindow.*`、`SettingsForm.*`、`DrawerSettingsDialog.cs`）；`src/Controls/ScrollableListView.cs`；`src/Drawers/`（条目模型、枚举排序、图像加载）；`src/Configuration/`（设置仓储分部文件、`SettingsSnapshot`、`DrawerDefinition`、`AppConstants`）；`src/Theming/`（主题、品牌字体、Shell 图标）；`src/Infrastructure/`（`DrawerLogger`、`TrayController`、`StartupRegistration`）。文件移动不改文件名与命名空间，WinForms 窗体的 `.cs`/`.Designer.cs`/`.resx` 保持同名同目录，因此设计器资源名不变（构建后仍为 6 个嵌入资源）。
