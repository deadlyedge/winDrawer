# ExplorerBrowserProbe — IExplorerBrowser 无框架文件夹视图烟测

一次性评估工具，用来判断 winDrawer 的抽屉内容是否值得从 `ListView` 迁移到外壳文件夹视图。
**它不参与应用构建**：不在 `src/winDrawer.slnx` 里，也不引用主程序，只会在 `smoke/ExplorerBrowserProbe/bin` 下产出。

完整评估口径、验收清单和已记录的实测结论见 [`../../docs/EXPLORER_BROWSER_SMOKE_TEST.md`](../../docs/EXPLORER_BROWSER_SMOKE_TEST.md)。

## 运行

```powershell
# 默认：当前用户的桌面，无框架视图，图标模式
dotnet run --project smoke\ExplorerBrowserProbe

# 指定目录 + 输出窗口结构诊断
dotnet run --project smoke\ExplorerBrowserProbe -- "D:\some\folder" --dump

# 对比打开外围窗格（命令栏/导航窗格/详细信息窗格）
dotnet run --project smoke\ExplorerBrowserProbe -- --frames

# 全部开关
dotnet run --project smoke\ExplorerBrowserProbe -- --help

# 自动关闭，用于验证销毁路径（应打印 Unadvise/Destroy 成功并以退出码 0 结束）
dotnet run --project smoke\ExplorerBrowserProbe -- --dump --auto-close=8
```

日志写在可执行文件旁的 `probe.log`（窗口标题里也会显示路径）。

## 窗口操作

- 无标题栏：拖动状态栏文字可以移动窗口，拖动四周窄边可以缩放，`Close` 按钮 / `Alt+F4` / `Esc` 关闭。
- 工具条按钮：`Frames`（切换 `EBO_SHOWFRAMES`）、`View mode`（图标→小图标→列表→详细→图块）、`Reload`、`Opacity`（切换窗口不透明度）、`Diagnostics`（把窗口结构写进日志）。
- 键盘 `Esc` / `F5` / `F7` / `F8` / `F9` 仅在工具条有焦点时生效；鼠标在视图里时按键交给外壳（这样才测得到 `F2` 改名）。

## 关键实现点

- 宿主是一个 WinForms 子控件，`IExplorerBrowser::Initialize` 直接挂在它的 HWND 上；视图是宿主的子窗口，因此宿主必须带 `WS_CLIPCHILDREN`，否则它的背景绘制会盖住外壳视图。
- `IExplorerBrowser::SetOptions` 只对后续导航生效，所以切换 `EBO_*` 走“销毁 + 重建”，顺带验证生命周期。
- 默认 `EBO_NOPERSISTVIEWSTATE`，烟测不会把视图状态写回用户的资源管理器设置。
- 缩放调用 `SetRect`；导航完成后用 `GetCurrentView(IID_IShellView)` 拿视图，`UIActivate(SVUIA_ACTIVATE_FOCUS)` 让 `F2` 改名有 UI 激活状态。
- 窗口不透明度 < 100% 时整个窗口变成分层窗口，外壳视图也在其中——这是评估 winDrawer 半透明抽屉的关键对照。
