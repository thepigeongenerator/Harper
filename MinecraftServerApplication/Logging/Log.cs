using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerApplication.Logging;
internal static class Log
{
    public static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create((builder) => builder.AddConsole());
    private static Dictionary<string, ILogger> _loggers;

    static Log()
    {
        _loggers = new Dictionary<string, ILogger>();
    }

    private static void Write(Type type, LogLevel level, string msg, object?[] args)
    {
        string loggerName = type.Name;
        ILogger? logger = null;

        if (_loggers.ContainsKey(loggerName))
        {
            logger = _loggers[loggerName];
        }

        logger ??= CreateLogger(loggerName);

        logger.Log(level, msg, args);
    }

    public static ILogger CreateLogger(string name)
    {
        ILogger logger = _loggerFactory.CreateLogger(name);
        _loggers.Add(name, logger);
        return logger;
    }

    #region shorthands
    #region WriteInfo
    public static void WriteInfo<T>(string msg, params object?[] args) => Write(typeof(T), LogLevel.Information, msg, args);
    public static void WriteInfo(Type type, string msg, params object?[] args) => Write(type, LogLevel.Information, msg, args);
    #endregion //WriteInfo
    #region WriteWarning
    public static void WriteWarning<T>(string msg, params object?[] args) => Write(typeof(T), LogLevel.Warning, msg, args);
    public static void WriteWarning(Type type, string msg, params object?[] args) => Write(type, LogLevel.Warning, msg, args);
    #endregion //WriteWarning
    #region WriteError
    public static void WriteError<T>(string msg, params object?[] args) => Write(typeof(T), LogLevel.Error, msg, args);
    public static void WriteError(Type type, string msg, params object?[] args) => Write(type, LogLevel.Error, msg, args);
    #endregion //WriteError
    #region WriteCritical
    public static void WriteCritical<T>(string msg, params object?[] args) => Write(typeof(T), LogLevel.Critical, msg, args);
    public static void WriteCritical(Type type, string msg, params object?[] args) => Write(type, LogLevel.Critical, msg, args);
    #endregion //WriteCritical
    #region WriteDebug
    public static void WriteDebug<T>(string msg, params object?[] args) => Write(typeof(T), LogLevel.Debug, msg, args);
    public static void WriteDebug(Type type, string msg, params object?[] args) => Write(type, LogLevel.Debug, msg, args);
    #endregion //WriteDebug
    #endregion //shorthands
}
