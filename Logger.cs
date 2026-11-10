using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace JumpRopeApp;

public static class Logger
{
    private static readonly string LogFileName = "app_diagnostics.log";

    public static string LogFilePath => Path.Combine(FileSystem.AppDataDirectory, LogFileName);

    public static void Log(string message, Exception? ex = null)
    {
        try
        {
            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logText = $"[{timeStamp}] {message}";

            if (ex != null)
            {
                logText += $"\nEXCEPTION: {ex.Message}\nSTACK TRACE:\n{ex.StackTrace}\n";
                if (ex.InnerException != null)
                {
                    logText += $"INNER EXCEPTION: {ex.InnerException.Message}\n";
                }
            }
            logText += new string('-', 50) + "\n";

            File.AppendAllText(LogFilePath, logText);
            System.Diagnostics.Debug.WriteLine(logText);
        }
        catch
        {
            // Fail quietly if file write fails
        }
    }

    public static string GetLogContent()
    {
        try
        {
            if (File.Exists(LogFilePath))
            {
                return File.ReadAllText(LogFilePath);
            }
        }
        catch (Exception ex)
        {
            return $"Error reading log file: {ex.Message}";
        }
        return "No diagnostic log entries recorded yet.";
    }

    public static void ClearLogs()
    {
        try
        {
            if (File.Exists(LogFilePath))
            {
                File.Delete(LogFilePath);
            }
        }
        catch { }
    }
}