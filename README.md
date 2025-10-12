# winDrawer

winDrawer is a Windows desktop companion that keeps frequently used folders only a swing away. Drag the main window to park it along a screen edge and open “drawers” that list the contents of your favourite directories with themed, high-DPI friendly UI elements powered by **SunnyUI**. Settings (window position, theme, drawers and more) are persisted in a TOML file so you can tailor the experience across sessions.

## Highlights

- **One-click drawers** – Browse directories with visual tiles sized from your chosen icon size.
- **Live theming** – Adjust hue, saturation, lightness and opacity and preview changes before saving.
- **Persistent layout** – Window and drawer sizes as well as drawer metadata are saved to `Drawer-settings.toml`.
- **System tray control** – Toggle visibility, jump into settings, or exit the app from the tray icon handled by `TrayController`.
- **Structured diagnostics** – Execution flow and errors are written to `Drawer.log` for easier troubleshooting.

## Getting Started

### Prerequisites

- Windows 10/11
- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

> The project targets `net9.0-windows` and uses Windows Forms.

### Build

```powershell
pwsh -NoLogo -Command "dotnet build"
```

The binaries are placed under `bin\Debug\net9.0-windows\win-x64\`.

### Run

```powershell
pwsh -NoLogo -Command "dotnet run"
```

The main window launches without a border and can be dragged by its header logo. Each configured drawer opens as an adjacent floating window.

## Configuration

- **Settings file** – `Drawer-settings.toml` (copied to the output directory) stores startup behaviour, theme values, window position, icon size, and the list of drawers.
- **Assets** – Icons and fonts live under `assets\`. The application expects:
  - `assets\drawer.icon.4.ico` for the window and tray icon.
  - `assets\fonts\Lobster-Regular.ttf` for the logo.
- **Logging** – Diagnostic output is appended to `Drawer.log` (same directory as the executable). Global exception handlers and contextual logging throughout `MainWindow`, `DrawerWindow`, `SettingsRepository`, and `SettingsForm` surface runtime issues.

### Customising drawers

1. Launch the app and press **Add Drawer** to select a folder.
2. Use the settings panel to adjust icon size, startup behaviour, and theme.
3. Drawer window sizes are remembered per path when you resize them.

## Project Layout

- `Program.cs` – Application entry point and global exception hooks.
- `MainWindow.cs` – Primary UI, drawer list, tray integration via `TrayController`.
- `DrawerWindow.cs` – Presentation of drawer contents, thumbnail generation, and theme application.
- `SettingsForm.cs` – Modeless settings panel with live theme preview.
- `Configuration/SettingsRepository.cs` – Handles reading/writing `Drawer-settings.toml`.
- `Theming/ThemePalette.cs` – Calculates derived colours from HSLA input.
- `AppConstants.cs` – Central store for shared constants (file names, UI dimensions, defaults).
- `DrawerLogger.cs` – File-backed logger with debugger mirroring.
- `TrayController.cs` – Encapsulates NotifyIcon setup and lifecycle.

## Dependencies

- [SunnyUI 3.8.8](https://github.com/lcxfs1991/SunnyUI) – Modern WinForms controls.
- [Tomlyn 0.11.0](https://github.com/xoofx/Tomlyn) – TOML parsing and serialization.

Both packages are restored automatically via NuGet during build.

## Contributing

1. Fork and clone the repository.
2. Create a feature branch from `main`.
3. Run `dotnet build` and test your changes.
4. Submit a pull request with a clear description.

## TODO

- add new tab in settings to edit drawers (add, edit name, sort and remove)

- click fullPath to open windows explorer

- add drawercontent preloading and cache at ".cachefiles" folder.

## License

This project is distributed under the MIT License. Update `LICENSE.txt` with your name and year before shipping.

