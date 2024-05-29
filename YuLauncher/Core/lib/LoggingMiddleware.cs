using System.Collections.Generic;
using System.IO;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace YuLauncher.Core.lib;
using NLog;

public class LoggingMiddleware
{
    private static Dictionary<string,string> _loggerNameMap = new();
    
    private static NLog.Config.LoggingConfiguration _config = new();
    
    private static readonly string FileFormat = "${basedir}/logs/${shortdate}.log";
    
    public static readonly string LongFileFormat = "${longdate} ${level:uppercase=true:padding=-5} [${threadid}] ${logger} - ${message} ${exception:format=tostring}";
    
    public static bool GetInstance(string LoggerName, ref NLog.Logger logger)
    {
        if (_loggerNameMap.ContainsKey(LoggerName))
        {
            logger = LogManager.GetLogger(_loggerNameMap[LoggerName]);
            return true;
        }
        else
        {
            return false;
        }
    }

    public static void LogAsync(string loggerName, string fileName, LogLevel levMin, LogLevel levMax,
        string layout =
            "${longdate} ${level:uppercase=true:padding=-5} [${threadid}] ${logger} - ${message} ${exception:format=tostring}")
    {
        if (_loggerNameMap.ContainsKey(loggerName))
        {
            return;
        }

        var logFile = new FileTarget(loggerName);
        
        logFile.FileName = fileName;
        logFile.ArchiveAboveSize = 1000000;
        logFile.MaxArchiveFiles = 10;
        logFile.ArchiveNumbering = ArchiveNumberingMode.Sequence;
        logFile.Layout = layout;

        AsyncTargetWrapper wrapper = new AsyncTargetWrapper();
        
        wrapper.WrappedTarget = logFile;
        wrapper.QueueLimit = 5000;
        wrapper.OverflowAction = AsyncTargetWrapperOverflowAction.Discard;
        
        _config.AddRule(levMin, levMax, wrapper, loggerName + "_logger");
        
        LogManager.Configuration = _config;
        
        _loggerNameMap.Add(loggerName, loggerName + "_logger");
    }

    public static void CloseLogger()
    {
        LogManager.Shutdown();
        
        _loggerNameMap.Clear();
    }
}