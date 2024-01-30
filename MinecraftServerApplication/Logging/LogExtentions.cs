using log4net;
using System.Diagnostics;

namespace MinecraftServerApplication.Logging;
internal static class LogExtentions {
    public static void LogInfo(this IModule module, string msg, params object[] args) => LogManager.GetLogger(module.GetType().Name).Info(string.Format(msg, args));
    public static void LogWarn(this IModule module, string msg, params object[] args) => LogManager.GetLogger(module.GetType().Name).Warn(string.Format(msg, args));
    public static void LogError(this IModule module, string msg, params object[] args) => LogManager.GetLogger(module.GetType().Name).Error(string.Format(msg, args));
    public static void LogFatal(this IModule module, string msg, params object[] args) => LogManager.GetLogger(module.GetType().Name).Fatal(string.Format(msg, args));
    public static void LogDebug(this IModule module, string msg, params object[] args) => LogManager.GetLogger(module.GetType().Name).Debug(string.Format(msg, args));
}
