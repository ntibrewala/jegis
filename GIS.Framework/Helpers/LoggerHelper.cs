using System;
using System.IO;

namespace GIS.Framework.Helpers
{
    /// <summary>
    /// Exclusively handles logging functionality across the framework.
    /// </summary>
    public static class LoggerHelper
    {
        // Target log directory specifically requested for this environment
        private const string LOG_DIRECTORY = @"D:\BHV\CLogs\GIS";

        /// <summary>
        /// Stores the last file I/O error so it can be reported via database when file logging fails.
        /// </summary>
        public static string LastError { get; private set; } = "";
        
        public static void Log(string message)
        {
            try
            {
                // Fallback for non-Windows local testing environments
                string finalPath = LOG_DIRECTORY;
                if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    finalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "BHV", "CLogs");
                }

                if (!Directory.Exists(finalPath))
                {
                    Directory.CreateDirectory(finalPath);
                }

                string logFilePath = Path.Combine(finalPath, $"GIS_Log_{DateTime.Now:yyyyMMdd}.txt");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";

                File.AppendAllText(logFilePath, logEntry);
            }
            catch (Exception ex)
            {
                LastError = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] LOG_DIRECTORY={LOG_DIRECTORY}, finalPath attempted, Error: {ex.ToString()}";
                // In production, failing to log should ideally not crash the main thread.
                try 
                { 
                    File.AppendAllText(@"C:\ProgramData\GIS_Log_Error.txt", LastError + Environment.NewLine); 
                } 
                catch { }
            }
        }

        public static void LogError(string context, Exception ex)
        {
            Log($"ERROR | {context} | Exception: {ex.Message} | StackTrace: {ex.StackTrace}");
        }
    }
}
