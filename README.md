# winDrawer

winDrawer is a small Windows Forms utility for keeping frequently used folders close at hand. Select a configured folder to open its drawer, which lists that folder's direct child files and directories. Double-clicking an item delegates the default open action to Windows Shell. Items can also be dragged out to Explorer, and files or folders can be dragged into an open drawer, with Windows performing the transfer. Common image files can be previewed as list thumbnails; theme and window settings are stored as TOML.

## Highlights

- **Folder shortcuts** – Open configured folders and browse direct child items in a Windows `ListView`.
- **Image thumbnails** – JPG/JPEG and PNG thumbnails appear as list icons; other items use Windows Shell icons.
- **Drag files in and out** – Drag selected drawer entries to Explorer, the desktop or another folder, or drop files/folders onto an open drawer. Windows performs the transfer: entries on the drawer folder's own volume are moved, entries from another volume are copied, Ctrl forces a copy and Shift forces a move. Progress, name conflicts and undo information come from the Shell; winDrawer never moves or copies files itself and offers no delete, rename, new-folder or paste commands.
- **Clickable folder link** – The folder icon and path in the drawer footer form one click target that opens the folder in File Explorer. The path stays on a single line and is truncated with an ellipsis when the drawer is narrow; hovering it shows the full path.
- **Selectable list order with lazy icons** – Directories and files stay mixed; items default to newest first (`LastWriteTimeUtc`, descending; ties by name then full path, unreadable timestamps last) and can be switched to oldest first or name order from the drawer's right-click menu, which also offers **Refresh** (F5). The choice is stored per drawer as `drawers[].sort_mode`. Icons and thumbnails load only for the visible area plus a one-screen margin, so large folders appear immediately.
- **Always on top** – Optionally keep the main window and the current drawer window above other windows; the option never applies to the settings panels.
- **Global show/hide shortcut** – Press `Win+Shift+D` from any application to hide or show the main window together with the drawer window that is open, exactly like the tray menu. The shortcut is registered per main-window handle and can be switched off with the **Enable Win+Shift+D** option in Settings; if another program already owns the combination, winDrawer keeps the option enabled, logs a warning and retries on the next start.
- **Live theming** – Choose the theme color through the Windows color dialog, adjust window opacity separately, and preview changes before saving.
- **Adjustable main window** – Set the main window height; the drawer list uses the remaining area and scrolls naturally.
- **Persistent settings** – Settings are stored in `%USERPROFILE%\.config\winDrawer\Drawer-settings.toml`.
- **Windows login startup** – Optionally register the app to start for the current user.
- **System tray control** – The main window and auxiliary windows do not create taskbar buttons during normal operation. Use the tray icon (or `Win+Shift+D`) to hide or show the main window together with the drawer window that is open, to open settings, or to exit; if tray initialization fails, the main window remains available from the taskbar. Exiting (from the tray menu or the settings panel) flushes pending settings and closes the windows gracefully instead of killing the process.
- **Structured diagnostics** – Execution flow and errors are written to `%USERPROFILE%\.config\winDrawer\Drawer.log` for easier troubleshooting. Debug-level entries are off by default (enable them with the `WINDRAWER_LOG_LEVEL` environment variable) and the log rotates to `Drawer.log.1` once it reaches 1 MB; unexpected errors are also reported once in a message box that points at this file.

## Getting Started

### Prerequisites

- Windows 10/11
- [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

> The project targets `net10.0-windows` and uses Windows Forms.

### Build

```powershell
dotnet restore .\src\winDrawer.csproj
dotnet build .\src\winDrawer.csproj
```

The default build output is under `src\bin\Debug\net10.0-windows\win-x64\`. `bin/` and `obj/` contain generated build files and are ignored by Git.

### Publish

```powershell
dotnet publish .\src\winDrawer.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:IncludeAllContentForSelfExtract=true `
  -o .\publish\win-x64
```

This framework-dependent single-file build requires the .NET 10 Windows Desktop runtime on the target machine. To include the runtime, use a self-contained publish instead:

```powershell
dotnet publish .\src\winDrawer.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:IncludeAllContentForSelfExtract=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o .\publish\win-x64

```

### Run

```powershell
dotnet run --project .\src\winDrawer.csproj
```

The main window launches without a border. When its position is unlocked, drag the branded header (shown with a move cursor) to reposition it; locking the position disables dragging and removes that cursor hint. Select a configured folder to open its drawer. Drawers show direct child files and directories in a native `ListView`; single-click selects an item and double-click delegates its default open action to Windows Shell. JPG/JPEG/PNG thumbnails are shown as list icons where available. Items are ordered newest first by last write time (directories and files mixed), and list icons for the visible area are generated as you scroll. The drawer hides its title bar but keeps a resizable frame: the caption-less top band is reclaimed as client area, and an invisible strip at the top of the content keeps the top edge (and the top corners) resizable like the other three sides. Right-clicking the list opens the display menu with **Refresh** (also `F5`), **Sort by name**, **Newest first** and **Oldest first**; the active order is check-marked and saved per drawer. The drawer no longer shows a refresh button in its bottom-right corner.

Drag any entry, or a multi-selection, out of the drawer to Explorer, the desktop or another folder; Windows performs the move or copy with its own progress and conflict dialogs, and the drawer refreshes itself after a move. Drag files or folders from Explorer onto the drawer contents to bring them into the folder: same-volume sources are moved, other volumes are copied, Ctrl forces a copy and Shift forces a move. Dropping a folder into itself or one of its subfolders is refused, and only real file-system entries are accepted (archive contents and mail attachments are ignored). The footer's folder icon and path are one click target that opens the folder in File Explorer; in a narrow drawer the path stays on one line with a trailing ellipsis, and the tooltip keeps showing the full path.

## Configuration

- **Settings file** – `%USERPROFILE%\.config\winDrawer\Drawer-settings.toml` stores startup behaviour (`start_with_windows`, `window_locked`, `always_on_top`, `toggle_hotkey_enabled`), window height and position, the `#RRGGBB` theme color, separate window opacity, and drawer data. A missing user settings file is initialized from the embedded root `Drawer-settings.toml` template; an existing file is never replaced by the template. If settings cannot be read or initialized, the app reports an error and leaves the existing file untouched. The settings panel's **Open Settings TOML** button opens the active file in Windows Notepad.
- **Current TOML format** – Theme color is `startup.window_color = "#RRGGBB"`; `startup.window_opacity` is a separate integer percentage (0–100). Drawer overlay colors use `drawers[].overlay_color = "#RRGGBB"` and are blended at a fixed 70%. Drawer sizes use `size = [width, height]` and the display order uses `sort_mode = "newest" | "oldest" | "name"` (missing or unknown means `"newest"`) inside each `[[drawers]]` table. See [`docs/CONFIGURATION_FORMAT.md`](docs/CONFIGURATION_FORMAT.md) for validation rules and unsupported legacy forms.
- **Existing legacy settings** – Back up an existing TOML file before manually converting it to the current format. The application does not migrate `window_color_hsla`, old `[drawers.size]` subtables, or other legacy representations; it does not search other directories for settings. The historical file under `reference/` is not a supported import template.
- **Assets** – The window/tray icon and Lobster branding font are embedded in the application assembly from the `assets/` folder at build time:
  - `assets\drawer.icon.4.ico` for the window and tray icon.
  - `assets/fonts/Lobster-Regular.ttf` for all branded headings and buttons. The application loads it from the embedded resource; it does not depend on the font being installed in Windows or on a loose font file beside the executable.
- **Logging** – New diagnostic output is appended to `%USERPROFILE%\.config\winDrawer\Drawer.log`. Existing logs beside an older executable are not migrated or removed. Logging failures do not prevent the app from running. The minimum logged level is `Information` (`Debug` while a debugger is attached) and can be overridden per run with `WINDRAWER_LOG_LEVEL=Debug|Information|Warning|Error|Off`; an invalid value falls back to the default. When the active file reaches 1 MB it is rotated to `Drawer.log.1` (replacing the previous rotated file), so a tray session running for weeks cannot grow the log without bound. An unexpected UI-thread error is logged every time and additionally reported once in a message box that names the log file; a failed settings save cancels the close instead of discarding changes.

### Managing folders and settings

1. Use **Add Drawer** to add a frequently used folder; use the existing remove action to remove a shortcut.
2. Click the theme-color preview to open the Windows color dialog. Adjust opacity independently; these visual changes preview live and can be saved or cancelled.
3. Toggle **Lock position** to immediately enable/disable main-window dragging and its move-cursor hint. Cancel restores the setting that was active when Settings opened; Save persists the new state.
4. Adjust **window height** to resize the main window. The drawer list scrolls to keep every configured folder available.
5. Toggle **Always on top** to keep the main window and the open drawer window above other windows; the preview applies immediately, Cancel restores the previous state, Save persists it, and Default restores the template value (`false`).
6. Toggle **Enable Win+Shift+D** to register or release the global show/hide shortcut immediately; Cancel restores the previous state, Save persists it, and Default restores the template value (`true`). If another program already owns the combination, the option stays enabled, the failure is reported once and the registration is retried on the next start.
7. Enable **Start with windows** to register startup for the current Windows user.
8. Use **Open Settings TOML** to inspect or edit the active configuration in Notepad.

The main window and auxiliary windows normally do not appear as taskbar buttons; use the system-tray menu (or `Win+Shift+D`) to hide or show the main window together with the open drawer window, open Settings, or exit. If tray initialization fails, the main window remains available from the taskbar.

Drawer settings (right-click a drawer) edit the display name and the bound folder. The folder path is display-only: click it, or focus it and press Enter/Space, to open the same folder picker as **Browse**…, which sits on its own row below the path. **Save**, **Cancel** and **Delete** keep their previous behaviour; there is no separate close button — Escape or Alt+F4 cancels without submitting the draft. Both settings windows open at the main window position offset by (100, 100) and are clamped into the working area of the screen the main window is on; their own position is never saved.

- `Program.cs` – Application entry point and global exception hooks.
- `Forms/` – All windows and dialogs with their designer files: `MainWindow.*` (drawer list, tray integration via `TrayController`, settings preview), `DrawerWindow.*` (drawer contents, lazy thumbnails, theme, footer folder link), `DrawerWindow.DragDrop.cs` (drag-out with `CF_HDROP`, drop acceptance and Shell hand-off), `SettingsForm.*` (modeless settings panel) and `DrawerSettingsDialog.cs`.
- `Controls/` – Reusable UI helpers: `ScrollableListView.cs` (native `ListView` scrolling, vertical scroll bar synchronization and viewport notifications), `EllipsisLabel.cs` (guaranteed single-line, ellipsis-truncated path label), `UiButtonTheming.cs` (shared accent/palette button colours), `UiContextMenuTheming.cs` (palette colours and readable text for context menus), `UiDialogHelpers.cs` (colour-dialog preset palette and Enter/Space activation), `UiMessages.cs` (single funnel for message boxes).
- `Drawers/` – Drawer content model and services: `DrawerItemInfo` (item metadata), `DrawerItemEnumerator` (direct-child enumeration and sorting), `DrawerSortMode` (display orders and their TOML values), `DrawerItemImageLoader` (Shell icon / thumbnail per item), `DrawerDropPlanner.cs` (same-volume move / cross-volume copy planning without file-system access).
- `Configuration/` – Settings data and persistence: `SettingsRepository.*` (in-memory TOML model, drawer operations, background writer, serialization), `SettingsSnapshot.cs` (settings snapshot and TOML parsing), `SettingsKeys.cs` (TOML key names), `DrawerDefinition.cs`, `AppConstants.cs`.
- `Theming/` – `ThemePalette.cs` (RGB hex parsing and derived palette), `BrandingFont.cs` (embedded Lobster font), `ShellIcon.cs` (Shell icon interop), `ThemePreviewEventArgs.cs`.
- `Infrastructure/` – Cross-cutting support: `DrawerLogger.cs` (file logger), `TrayController.cs` (NotifyIcon lifecycle), `StartupRegistration.cs` (per-user login startup), `PathComparison.cs` (single path-equality rule), `GlobalHotKey.cs` (Win32 `RegisterHotKey`/`UnregisterHotKey` wrapper for the Win+Shift+D shortcut), `ShellFileOperation.cs` (`IFileOperation` interop so drops are executed by the Windows Shell).

## Dependencies

- [SunnyUI 3.9.8](https://github.com/lcxfs1991/SunnyUI) – Modern WinForms controls.
- [Tomlyn 2.10.1](https://github.com/xoofx/Tomlyn) – TOML parsing and serialization.

Both packages are restored automatically via NuGet during build.

SunnyUI remains in use by WinForms forms and Designer controls; Tomlyn remains in use for settings parsing and serialization.

## Contributing

1. Fork and clone the repository.
2. Create a feature branch from `main`.
3. Run `dotnet build` and test your changes.
4. Submit a pull request with a clear description.

## License

This project is distributed under the MIT License. Update `LICENSE.txt` with your name and year before shipping.

