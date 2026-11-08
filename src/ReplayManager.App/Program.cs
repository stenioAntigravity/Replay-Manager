using System;
using System.IO;

namespace ReplayManager.App
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Log entry for Program.Main start (simple file log, immediate write)
            try
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReplayManager", "logs", "startup.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [INFO] Program.Main entry. args: {string.Join(", ", args)}{Environment.NewLine}");
            }
            catch { /* ignore logging errors */ }

            // Delegar ao código gerado em App.g.cs que já carrega o XAML e executa OnStartup
            // Log after App.Main returns (app exited)
            StartupLogger.Log("Program.Main after App.Main returned");
            ReplayManager.App.App.Main();
        }
    }
}
