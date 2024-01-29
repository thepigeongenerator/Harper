namespace MinecraftServerApplication.Logging;
internal static class LogExtentions {
    public static void LogInfo(this IModule module, string msg, params object?[] args) => Log.WriteInfo(module.GetType(), msg, args);
    public static void LogWarning(this IModule module, string msg, params object?[] args) => Log.WriteWarning(module.GetType(), msg, args);
    public static void LogError(this IModule module, string msg, params object?[] args) => Log.WriteError(module.GetType(), msg, args);
    public static void LogCritical(this IModule module, string msg, params object?[] args) => Log.WriteCritical(module.GetType(), msg, args);
    public static void LogDebug(this IModule module, string msg, params object?[] args) => Log.WriteDebug(module.GetType(), msg, args);
}
