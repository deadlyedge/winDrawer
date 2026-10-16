namespace winDrawer
{
    internal static class Program
    {
        private static int _unexpectedErrorDialogShown;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Handle UI-thread exceptions here instead of letting the default WinForms dialog appear,
            // so the user gets one actionable message (including the log path) while every occurrence
            // still lands in the log.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;

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
                    UiMessages.ShowError(
                        null,
                        "Settings Error",
                        $"winDrawer could not load or initialize its settings file. Check the TOML syntax and current configuration format, verify that the configuration directory is writable, and ensure the application settings template is available. Existing settings were not replaced.{Environment.NewLine}{Environment.NewLine}{ex.Message}");
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

        /// <summary>
        /// Logs every UI-thread exception and surfaces the first one to the user: reporting the fault
        /// without a visible message made earlier failures look like "nothing happened".
        /// </summary>
        private static void OnThreadException(object sender, ThreadExceptionEventArgs args)
        {
            DrawerLogger.LogError("Unhandled UI thread exception", args.Exception);

            if (Interlocked.Exchange(ref _unexpectedErrorDialogShown, 1) != 0)
            {
                return;
            }

            try
            {
                UiMessages.ShowError(
                    null,
                    $"{AppConstants.Branding.ApplicationDisplayName} - Unexpected Error",
                    BuildUnexpectedErrorDialogText(args.Exception));
            }
            catch (Exception dialogFailure)
            {
                DrawerLogger.LogError("Failed to show the unexpected error dialog.", dialogFailure);
            }
        }

        /// <summary>
        /// Builds the unexpected-error message. Kept separate so the wording can be verified without
        /// displaying a modal dialog.
        /// </summary>
        internal static string BuildUnexpectedErrorDialogText(Exception exception)
        {
            var logHint = string.IsNullOrWhiteSpace(DrawerLogger.LogFilePath)
                ? "Diagnostic details were written to the application log."
                : $"Diagnostic details were written to:{Environment.NewLine}{DrawerLogger.LogFilePath}";

            return $"{AppConstants.Branding.ApplicationDisplayName} hit an unexpected error and the last action may not have completed. "
                + "You can keep working; if the problem repeats, close and restart the application."
                + $"{Environment.NewLine}{Environment.NewLine}{logHint}"
                + $"{Environment.NewLine}{Environment.NewLine}{exception.Message}";
        }
    }
}
