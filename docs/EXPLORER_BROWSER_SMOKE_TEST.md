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

## 4. 仍需人工确认（自动化跑不到）

- [ ] `F2` 就地改名是否真的可用，提交后视图是否立即更新。
- [ ] 拖出 / 拖入是否都可用；同盘与异盘行为是否与资源管理器一致。
- [ ] 连续缩放、最大化、跨不同 DPI 显示器移动后布局是否正常。
- [ ] `Opacity` < 100% 时外壳视图是否仍然正确绘制（分层窗口），以及 `FWF_TRANSPARENT` 能否让视图背景用父窗口背景色。
- [ ] 字体、语言（若系统是中文，视图内是中文吗）、深浅色主题是否与 winDrawer 的配色预期冲突。

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
