using Microsoft.Extensions.Logging;
using MinecraftServerApplication.Discord;
using MinecraftServerApplication.Logging;
using MinecraftServerApplication.Minecraft;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;

namespace MinecraftServerApplication;

internal static class Program {
    public const string SETTINGS_PATH = "./settings";
    public const string DATA_PATH = "./data";
    public const string BACKUP_PATH = "./backups";
    public const string LOG_PATH = "./logs";
    private static readonly ILogger _log;
    private static readonly ManualResetEvent shutdownEvent = new(false);
    private static readonly List<IModule> _modules = [];

    static Program() {
        //get the executing assembly
        Assembly assembly = Assembly.GetExecutingAssembly();

        //init logger
        _log = Log.CreateLogger("System");

        //log the application version
        _log.LogInformation($"Running version: v{FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion}");

        //load modules
        foreach (Type type in assembly.GetTypes()) {
            if (!type.IsAbstract && type.IsClass && type.IsAssignableTo(typeof(IModule))) {
                try {
                    //init command
                    IModule module = Activator.CreateInstance(type) as IModule ?? throw new NullReferenceException("wasn't able to create an instance of the command");

                    //add the command
                    _modules.Add(module);
                }
                catch (Exception ex) {
                    _log.LogError($"Something went wrong when initializing type '{type.FullName}':\n{ex}");
                }
            }
        }
    }

    public static void Main() {
        MainAsync().Wait();
    }

    public static async void Shutdown() {
        shutdownEvent.Set();

        List<Task> shutdown = new();
        foreach (IModule module in _modules) {
            shutdown.Add(module.Shutdown());
        }

        await Task.WhenAll(shutdown);
    }

    public static Task WaitShutdownAsync() {
        return Task.Run(shutdownEvent.WaitOne);
    }

    public static T? GetModuleOfType<T>() where T : class, IModule {
        return (
            from module in _modules
            where module is T
            select module as T).FirstOrDefault();
    }

    private static async Task MainAsync() {
        List<Task> runModules = [];

        for (int i = 0; i < _modules.Count; i++) {
            _log.LogInformation($"{MathF.Round((float)i / _modules.Count * 100)}% running '{_modules[i].GetType().Name}'...");
            Task task = _modules[i].Run();
            runModules.Add(task);
        }

        _log.LogInformation("100% done!");

        await WaitShutdownAsync(); //await the shutdown event
        await Task.WhenAll(runModules); //await the modules from compleding
    }
}
