using System;
using System.IO;
using System.Windows.Forms;

namespace CornDownloader
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Without these, an exception thrown anywhere in an event handler — including
            // the async void handlers used throughout MainForm (OnInstallClicked etc.) —
            // takes the whole app down with Windows' generic crash dialog and leaves no
            // trail of what happened. Catch it, log it, let the person keep working.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => HandleUnhandled(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => HandleUnhandled(e.ExceptionObject as Exception);

            Application.Run(new MainForm());
        }

        private static void HandleUnhandled(Exception ex)
        {
            if (ex == null) return;

            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CornStudios", "CornDownloader");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
            }
            catch { /* best-effort logging — don't let the handler itself throw */ }

            try
            {
                MessageBox.Show(
                    $"CornDownloader hit an unexpected error:\n\n{ex.Message}\n\n" +
                    "Details were saved to crash.log in the app's data folder. " +
                    "You can usually keep working — if something looks wrong, restart the app.",
                    "Unexpected error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch { }
        }
    }
}