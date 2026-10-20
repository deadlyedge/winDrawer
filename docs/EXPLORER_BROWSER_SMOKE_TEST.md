# IExplorerBrowser 无框架文件夹视图烟测

对应 TODO：*设计一个烟测让我查看 IExplorerBrowser 的无框架文件夹视图效果，以评估迁移可能性。*

- 工具：`smoke/ExplorerBrowserProbe/`（独立工程，不加入 `src/winDrawer.slnx`，不影响应用构建）
- 运行方式、开关与窗口操作见 [`smoke/ExplorerBrowserProbe/README.md`](../smoke/ExplorerBrowserProbe/README.md)
- 目标：**先验证原生外壳视图能不能满足抽屉的最小需求，再决定要不要迁移**，而不是自己写一个文件管理器。

## 1. 最小验证范围

| # | 要验证的行为 | 期望观察到的结果 |
| --- | --- | --- |
| 1 | 指定目录 | 窗口直接显示该目录的**直接子项**（不递归、不带地址栏） |
| 2 | 隐藏外围界面 | 没有导航窗格、命令栏、详细信息窗格、状态栏；只有文件夹视图本身 |
| 3 | `F2` 改名 | 视图里选中条目按 `F2` 出现就地重命名编辑框并能提交 |
| 4 | 双向拖放 | 能从视图里把条目拖到资源管理器/桌面（拖出），也能把文件拖进视图（拖入），同盘移动/异盘拷贝交给系统 |
| 5 | 窗口尺寸变化 | 拖边框缩放时视图跟着重建布局，无闪烁、无残留、无错位（多 DPI 下同样成立） |

补充对照项：窗口不透明度 < 100%（`Opacity` 按钮）时外壳视图在半透明分层窗口里的表现；`--view=details` 与 `--hide-header` 下详细信息的列头是否可隐藏。

## 2. 操作步骤

```powershell
dotnet build smoke\ExplorerBrowserProbe\ExplorerBrowserProbe.csproj
dotnet run --project smoke\ExplorerBrowserProbe -- "<目录>" --dump
```

按顺序做这 5 件事，每一步都留意上面的期望结果：

1. 默认参数打开 → 检查第 1、2 项（`--dump` 会把窗口树写进 `probe.log`，可直接对照“有没有多余窗口”）。
2. 在视图里选中一个文件按 `F2` → 第 3 项。
3. 把视图里的条目拖到桌面/资源管理器；再从资源管理器拖文件回视图 → 第 4 项。
4. 拖动窗口边框改变大小，再最大化/还原 → 第 5 项。
5. 点 `Frames` 再点一次 → 直观看到“有无外围窗格”的差别；点 `Opacity` 看半透明。

## 3. 已在本机实测到的结论（Windows 11 / .NET 10 / SDK 10.0.26100）

以下结论来自本仓库 `probe.log` 的实际运行记录，不是推测：

- **无框架视图成立。** 不设置 `EBO_SHOWFRAMES` 时，窗口树只有：
  `ExplorerBrowserControl`（浏览器自己的包装窗口，带 `WS_BORDER`）→ `SHELLDLL_DefView` → `DirectUIHWND` + 两个 `ScrollBar`。
  没有 `ToolbarWindow32`／`msctls_statusbar32`／`NamespaceTreeControl` 之类外围界面，官方文档也说明此时“浏览器只用一个视图对象”。
- **打开 `EBO_SHOWFRAMES` 后会一次性带回全套外围界面**：`DUIViewWndClassName`、`NamespaceTreeControl`+`SysTreeView32`（导航窗格）、`Shell Preview Extension Host`（预览窗格）、命令模块按钮等 —— 所以“隐藏外围界面”应该依靠**不设置** `EBO_SHOWFRAMES`，而不是事后去关窗格。
- **给宿主实现 `IExplorerPaneVisibility` 那条路走不通（重要负面结论）。** 官方要求宿主响应 `QueryService(SID_ExplorerPaneVisibility)` 且“必须在站点链里”，但 `IExplorerBrowser` 没有 `SetSite`。烟测用 `IObjectWithSite::SetSite` 把提供者装到视图站点上：`SetSite` 返回 `S_OK`，之后能看到外壳持续来查询其它服务并被正确转发，**但外壳从未查询过 `SID_ExplorerPaneVisibility`**，`GetPaneState` 一次都没被调用，导航窗格照旧显示。直接用视图站点查询该服务返回 `E_NOTIMPL`。
- **`FOLDERSETTINGS` 有效**：`ViewMode`（`FVM_ICON`/`FVM_DETAILS`…）和 `FWF_TRANSPARENT`、`FWF_NOCOLUMNHEADER`、`FWF_NOHEADERINALLVIEWS` 都能被接受，`IShellView::GetCurrentInfo` 会回显外壳实际生效的值（外壳自己会追加 `NoClientEdge|FullRowSelect`）。注意 `FVM_SMALLICON` 在外壳里会折叠成“`FVM_ICON` + 小图标尺寸”，`GetCurrentInfo` 仍报 `FVM_ICON`。
- **销毁路径干净**：关闭窗口时 `Unadvise`、`Destroy` 均返回 `S_OK`，进程以退出码 0 结束（用 `--auto-close=<秒>` 可自动验证）。
- **导航与激活链路正常**：`Initialize`/`SetOptions`/`Advise`/`BrowseToObject` 全部返回 `S_OK`，`OnNavigationPending → OnViewCreated → OnNavigationComplete` 按序回调，`GetCurrentView(IID_IShellView)` 与 `UIActivate(SVUIA_ACTIVATE_FOCUS)` 成功。目录不存在时 `SHCreateItemFromParsingName` 返回 `0x80070002`，视图留空，进程不崩。
- 包装窗口 `ExplorerBrowserControl` 默认带 `WS_BORDER`；`EBO_NOBORDER` / `EBO_NOWRAPPERWINDOW` 是否真的消除这条边框，需要肉眼确认。
- **`IPersistFolder2` 拿不到当前目录**：对 `IShellView` 查询 `IPersistFolder2` 失败，宿主无法让视图自报“现在在哪个目录”；判断当前位置只能依靠 `OnNavigationPending`/`OnNavigationComplete` 回调里的 pidl（用 `SHGetNameFromIDList` 解析）。

## 3.1 第二轮追加实验：双击文件夹、排序菜单、彩色背景

用 `--self-test`（脚本化自检，日志见 `probe.log` 的“自检开始/结束”块）实测，结论如下。

### 问题 1：双击文件夹能否改成“另开一个资源管理器窗口”

**可以，而且不需要放弃原生行为。** 做法是宿主实现 `IExplorerBrowserEvents::OnNavigationPending`：文档规定“返回任何失败码都会取消导航”，因此在回调里判断目标目录是否就是抽屉锚定目录——不是就返回 `E_ABORT` 取消，并按需用 `ProcessStartInfo(path) { UseShellExecute = true }` 让 Shell 打开一个新窗口。

实测（`--folder-open=block` / `--folder-open=explorer`，目录 `src`）：

- 第一次导航到锚定目录时回调打印 `放行（正是锚定目录）`；
- 触发进入 `src\bin` 时打印 `已取消导航`，视图**仍然留在 `src`**（等待 2 秒后事件计数 `OnNavigationPending=2`、`OnNavigationComplete=1`，说明第二次导航确实没有完成）；
- `explorer` 模式下先打印 `Shell 打开 '<子目录>' -> 已请求`，再取消导航；
- 取消后外壳会紧接着回调 `OnNavigationFailed`，这与文档“取消导航”的语义一致，不是错误。

两个必须注意的实现细节：

1. `OnNavigationPending` 是**异步**送达的：宿主动调用 `BrowseToObject` 后立即返回，回调要等消息循环下一轮才到（实测约 50 ms 后），所以不能在调用点同步假设“已经拦下了”。
2. 三种策略的取舍：`inplace` 保留外壳原生导航（在小窗口里容易迷路）；`explorer` = 拦下并另开资源管理器窗口（推荐）；`block` = 只拦下（适合固定单层内容）。另外 `EBO_NAVIGATEONCE` 只能做到“不再继续导航”，不会替你打开新窗口。

### 问题 2：排序/分组选项能否通过选项隐藏

**没有公开的 FOLDERFLAGS 能去掉它们；只能由宿主接管右键菜单。**

客观证据来自菜单转储（`IShellView::GetItemObject(SVGIO_BACKGROUND, IID_IContextMenu)` + `IContextMenu::QueryContextMenu`，把真实菜单枚举出来），在同时加了 `FWF_NOGROUPING`、`FWF_NOFILTERS`、`FWF_NOCOLUMNHEADER`、`FWF_NOHEADERINALLVIEWS`（`GetCurrentInfo` 确认全部生效）的情况下，背景菜单依旧是：

```
[0] 查看(&V)      → 超大图标/大图标/中等图标/小图标/列表/详细信息/平铺/内容
[1] 排序方式(&O)  → 名称/修改日期/类型/大小/递增/递减
[2] 分组依据(&P)  → none/(无)/递增/递减/更多…
[3] 刷新(&E)      ……以及粘贴、第三方菜单项（Git、VS Code、WizTree、PowerRename 等）、新建、属性
```

即：这些标志影响的是**视图状态**（分组、筛选器、列头），不会移除菜单项。列头可以用 `FWF_NOCOLUMNHEADER` 隐藏，但“排序方式/分组依据”菜单项不行。

同时这次转储也证明**接管菜单在技术上可行**：`GetItemObject(SVGIO_BACKGROUND, IID_IContextMenu)` 能拿到可用的 `IContextMenu`，`QueryContextMenu` 能构建出完整菜单（含第三方项）。因此若要隐藏排序/分组，路径是宿主自己处理视图的右键消息、把这份菜单删掉「排序方式」「分组依据」（及可选「查看」）后再显示，并自己转发 `InvokeCommand`。这项工作需要在 DefView 窗口上做子类化，属于真正的功能开发，不在烟测范围内。

### 问题 3：能否给内容区应用彩色/透明背景

**不能。视图自身背景由外壳绘制，公开选项里没有可用的着色手段。**

- `FWF_TRANSPARENT` 的官方说明是“Draw transparently. This is used only for the desktop.”——它本来就是给桌面用的。实测：宿主背景设为 `#203040`、加 `--transparent`（同时去掉 `WS_CLIPCHILDREN`，让宿主有机会先画背景），再叠加 `--no-wrapper`（让 DefView 直接挂在宿主上），外壳视图 5×5 采样的像素**全部仍是 `#FFFFFF`**，从未出现宿主背景色。
- 深色实验（`--dark`，调用 uxtheme 未公开导出 `SetPreferredAppMode(ForceDark)`，实测返回“之前的模式=0”即调用成功）也不会让视图变深色：背景采样仍是 `#FFFFFF`。深色渲染依赖系统/应用的深色模式支持，不能用这个开关当作可靠手段。
- 因此可用的“颜色/透明”手段只剩窗口整体 `Opacity`（WinForms 让整个窗口变成分层窗口，外壳视图一起被降低不透明度）。winDrawer 现在的“专属颜色 + 70% 蒙版”效果无法用外壳视图复现；要么接受浅色内容区，要么继续自己画列表。

> 采样可信度：`--sample-pixels` 在窗口不可见时会打印“结果不可信”。上面的结论里，第二、三次采样没有出现该提示（窗口可见），且与官方文档一致；仍建议在真实桌面上再跑一次 `--sample-pixels` 复核。


## 4. 仍需人工确认（自动化跑不到）

- [ ] `F2` 就地改名是否真的可用，提交后视图是否立即更新。
- [ ] 拖出 / 拖入是否都可用；同盘与异盘行为是否与资源管理器一致。
- [ ] 连续缩放、最大化、跨不同 DPI 显示器移动后布局是否正常。
- [ ] 双击文件夹：把 `Folders` 切到 explorer 模式后，双击是否只新开资源管理器窗口、本窗口内容保持不变（需要在真实桌面上确认，自检里的自动触发无法验证“新窗口真的出现了”）。
- [ ] 右键菜单：肉眼复核一次确实存在「排序方式」「分组依据」（问题 2 的结论基于程序化转储）。
- [ ] `Opacity` < 100% 时外壳视图在分层窗口里的实际观感（文字是否发虚、能否接受）。
- [ ] 真实桌面上再跑一次 `--sample-pixels`，复核问题 3 的“背景不透明”结论。
- [ ] 字体、语言与深浅色：系统处于深色模式时视图内的文字/背景是否可读，是否与 winDrawer 的配色预期冲突。

## 5. 迁移判断要点（给结论用）

- 会**免费**得到：完整的外壳项目管理（`F2` 改名、右键菜单、拖放、排序、筛选、分组、搜索框、虚拟文件夹、缩略图与图标全部跟随系统设置、DPI 与无障碍）——这些是当前 `ListView` 版本要自己实现或直接没有的。
- 会**失去**或需要重新评估：
  - 主题一致性：外壳视图是系统绘制的，与 winDrawer 的半透明抽屉配色不同源；`Opacity` 依赖分层窗口，外壳视图未必配合。
  - `AGENTS.md`/`DEVELOPMENT_PLAN.md` 目前明确写了“不嵌入 `IExplorerBrowser`/完整资源管理器 Shell 视图”，迁移必须先改这两份文档并确认范围。
  - 版本差异（不同 Windows 版本呈现的窗格/按钮集合不同）、无法隐藏包装窗口边框（待确认）等不可控面。
  - 只显示**直接子项**这一产品约束：外壳视图天然允许双击进入子目录，若坚持不导航，需要 `EBO_NAVIGATEONCE` 约束并验证交互感受。
- 建议结论形式：勾选 5 项最小验证的实测结果 + 上面两类得失的取舍，再决定“迁移 / 只在抽屉里混用（外壳视图 + 主题蒙版）/ 保持 `ListView`”。

## 6. 记录模板

```
日期／环境：
结论：迁移 / 部分采用 / 不迁移
1 指定目录：
2 隐藏外围界面：
3 F2 改名：
4 双向拖放：
5 尺寸变化：
其他（主题、DPI、透明度）：
遗留问题：
```
