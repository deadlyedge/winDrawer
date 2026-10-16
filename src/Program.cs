namespace winDrawer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.ThreadException += (_, args) =>
                DrawerLogger.LogError("Unhandled UI thread exception", args.Exception);

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var exception = args.ExceptionObject as Exception
                                ?? new Exception($"Non-exception thrown: {args.ExceptionObject}");
                DrawerLogger.LogError("Unhandled domain exception", exception);
            };

            DrawerLogger.LogInformation("Application bootstrap starting.");

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            try
            {
                MainWindow mainWindow;
                try
                {
                    mainWindow = new MainWindow();
                }
                catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or System.Security.SecurityException or Tomlyn.TomlException)
                {
                    DrawerLogger.LogError("Unable to load or initialize the settings file. The existing settings file was not replaced.", ex);
                    MessageBox.Show(
                        $"winDrawer could not load or initialize its settings file. Check the TOML syntax and current configuration format, verify that the configuration directory is writable, and ensure the application settings template is available. Existing settings were not replaced.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                        "Settings Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                Application.Run(mainWindow);
                DrawerLogger.LogInformation("Application shutdown completed.");
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Fatal error during application run loop.", ex);
                throw;
            }
        }
    }
}
