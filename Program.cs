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
                Application.Run(new MainWindow());
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
