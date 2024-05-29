using NLog;

namespace YuLauncher.Core.lib;

public static class LoggerController
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public static void LogInfo(string message)
    {
        Logger.Info(message);
    }
    
    public static void LogError(string message)
    {
        Logger.Error(message);
    }
    
    public static void LogWarn(string message)
    {
        Logger.Warn(message);
    }
    
    public static void LogDebug(string message)
    {
        Logger.Debug(message);
    }
    
    public static void LogFatal(string message)
    {
        Logger.Fatal(message);
    }
    
    public static void LogTrace(string message)
    {
        Logger.Trace(message);
    }
}