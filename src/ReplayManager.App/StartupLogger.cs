using System;
using System.IO;

namespace ReplayManager.App
{
    /// <summary>
    /// Simple file logger for early startup diagnostics. Writes immediately to a file in the local AppData folder.
    /// </summary>
    public static class StartupLogger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReplayManager",
            "logs",
            "startup.log");

        static StartupLogger()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            }
            catch
            {
                // ignore directory creation errors
            }
        }

        public static void Log(string message)
        {
            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [INFO] {message}{Environment.NewLine}";
                File.AppendAllText(LogPath, line);
            }
            catch
            {
                // ignore logging errors
            }
        }
    }
}

