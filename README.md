# winDrawer

winDrawer is a small Windows Forms utility for keeping frequently used folders close at hand. Select a configured folder to open its drawer, which lists that folder's direct child files and directories. Double-clicking an item delegates the default open action to Windows Shell. Common image files can be previewed as list thumbnails; theme and window settings are stored as TOML.

## Highlights

- **Folder shortcuts** – Open configured folders and browse direct child items in a Windows `ListView`.
- **Image thumbnails** – JPG/JPEG and PNG thumbnails appear as list icons; other items use Windows Shell icons.
- **Live theming** – Choose the theme color through the Windows color dialog, adjust window opacity separately, and preview changes before saving.
- **Adjustable main window** – Set the main window height; the drawer list uses the remaining area and scrolls naturally.
- **Persistent settings** – Settings are stored in `%USERPROFILE%\.config\winDrawer\Drawer-settings.toml`.
- **Windows login startup** – Optionally register the app to start for the current user.
- **System tray control** – The main window and auxiliary windows do not create taskbar buttons during normal operation. Use the tray icon to show/hide the main window, open settings, or exit; if tray initialization fails, the main window remains available from the taskbar.
- **Structured diagnostics** – Execution flow and errors are written to `%USERPROFILE%\.config\winDrawer\Drawer.log` for easier troubleshooting.

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

The main window launches without a border. When its position is unlocked, drag the branded header (shown with a move cursor) to reposition it; locking the position disables dragging and removes that cursor hint. Select a configured folder to open its drawer. Drawers show direct child files and directories in a native `ListView`; single-click selects an item and double-click delegates its default open action to Windows Shell. JPG/JPEG/PNG thumbnails are shown as list icons where available.

## Configuration

- **Settings file** – `%USERPROFILE%\.config\winDrawer\Drawer-settings.toml` stores startup behaviour, window height and position, the `#RRGGBB` theme color, separate window opacity, and drawer data. A missing user settings file is initialized from the embedded root `Drawer-settings.toml` template; an existing file is never replaced by the template. If settings cannot be read or initialized, the app reports an error and leaves the existing file untouched. The settings panel's **Open Settings TOML** button opens the active file in Windows Notepad.
- **Current TOML format** – Theme color is `startup.window_color = "#RRGGBB"`; `startup.window_opacity` is a separate integer percentage (0–100). Drawer overlay colors use `drawers[].overlay_color = "#RRGGBB"` and are blended at a fixed 70%. Drawer sizes use `size = [width, height]` inside each `[[drawers]]` table. See [`docs/CONFIGURATION_FORMAT.md`](docs/CONFIGURATION_FORMAT.md) for validation rules and unsupported legacy forms.
- **Existing legacy settings** – Back up an existing TOML file before manually converting it to the current format. The application does not migrate `window_color_hsla`, old `[drawers.size]` subtables, or other legacy representations; it does not search other directories for settings. The historical file under `reference/` is not a supported import template.
- **Assets** – The window/tray icon and Lobster branding font are embedded in the application assembly from the `assets/` folder at build time:
  - `assets\drawer.icon.4.ico` for the window and tray icon.
  - `assets/fonts/Lobster-Regular.ttf` for all branded headings and buttons. The application loads it from the embedded resource; it does not depend on the font being installed in Windows or on a loose font file beside the executable.
- **Logging** – New diagnostic output is appended to `%USERPROFILE%\.config\winDrawer\Drawer.log`. Existing logs beside an older executable are not migrated or removed. Logging failures do not prevent the app from running.

### Managing folders and settings

1. Use **Add Drawer** to add a frequently used folder; use the existing remove action to remove a shortcut.
2. Click the theme-color preview to open the Windows color dialog. Adjust opacity independently; these visual changes preview live and can be saved or cancelled.
3. Toggle **Lock position** to immediately enable/disable main-window dragging and its move-cursor hint. Cancel restores the setting that was active when Settings opened; Save persists the new state.
4. Adjust **window height** to resize the main window. The drawer list scrolls to keep every configured folder available.
5. Enable **Start with windows** to register startup for the current Windows user.
6. Use **Open Settings TOML** to inspect or edit the active configuration in Notepad.

The main window and auxiliary windows normally do not appear as taskbar buttons; use the system-tray menu to show/hide the main window, open Settings, or exit. If tray initialization fails, the main window remains available from the taskbar.

## Project Layout

- `Program.cs` – Application entry point and global exception hooks.
- `MainWindow.cs` – Primary UI, drawer list, tray integration via `TrayController`.
- `DrawerWindow.cs` – Presentation of drawer contents, thumbnail generation, and theme application.
- `SettingsForm.cs` – Modeless settings panel with live theme preview.
- `Configuration/SettingsRepository.cs` – Handles reading/writing `%USERPROFILE%\.config\winDrawer\Drawer-settings.toml`.
- `Theming/ThemePalette.cs` – Parses RGB hex colors and calculates the derived theme palette.
- `AppConstants.cs` – Central store for shared constants (file names, UI dimensions, defaults).
- `DrawerLogger.cs` – File-backed logger with debugger mirroring.
- `TrayController.cs` – Encapsulates NotifyIcon setup and lifecycle.

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

