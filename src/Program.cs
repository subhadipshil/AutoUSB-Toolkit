using System;
using System.Runtime.InteropServices;
using System.Windows;
using AutoUSBToolkit.Core;
using AutoUSBToolkit.UI;

namespace AutoUSBToolkit
{
    public class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // Enable Per-Monitor High-DPI scaling on modern Windows
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    SetProcessDPIAware();
                }
            }
            catch { }

            var app = new Application();
            app.DispatcherUnhandledException += (s, e) =>
            {
                Logger.Error("Unhandled UI Exception: " + e.Exception.Message);
                MessageBox.Show(
                    "An unexpected error occurred:\n\n" + e.Exception.Message + "\n\nCheck the log console for technical details.",
                    "AutoUSB Toolkit Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                e.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                if (ex != null)
                {
                    Logger.Error("Fatal AppDomain Exception: " + ex.Message);
                }
            };

            try
            {
                var mainWindow = new MainWindow();
                app.Run(mainWindow);
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal Startup Exception: " + ex.ToString());
                MessageBox.Show(
                    "AutoUSB Toolkit encountered a startup error:\n\n" + ex.Message + "\n\n" + ex.StackTrace,
                    "AutoUSB Toolkit Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}

