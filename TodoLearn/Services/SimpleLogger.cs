using System;
using System.IO;

namespace TodoLearn.Services
{
    internal static class SimpleLogger
    {
        private static readonly string LogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "todolist.log");

        public static void Log(string message)
        {
            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
            catch
            {
                Exception ex = new Exception(message);
                string errorLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Failed to log message: {ex.Message}";
            }
        }
    }
}
