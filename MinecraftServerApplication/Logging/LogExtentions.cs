using log4net;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MinecraftServerApplication.Logging;
internal static class LogExtentions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void LogInfo(this IModule module, string msg, params object[] args) => LogManager.GetLogger(module.GetType().Name).Info(string.Format(msg, args));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void LogWarn(this IModule module, string msg, params object[] args) => LogManager.GetLogger(module.GetType().Name).Warn(string.Format(msg, args));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void LogError(this IModule module, string msg, params object[] args) => LogManager.GetLogger(module.GetType().Name).Error(string.Format(msg, args));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void LogFatal(this IModule module, string msg, params object[] args) => LogManager.GetLogger(module.GetType().Name).Fatal(string.Format(msg, args));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void LogDebug(this IModule module, string msg, params object[] args) => LogManager.GetLogger(module.GetType().Name).Debug(string.Format(msg, args));
}
