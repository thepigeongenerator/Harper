using log4net;
using log4net.Config;
using MinecraftServerApplication.Discord;
using MinecraftServerApplication.Minecraft;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;

namespace MinecraftServerApplication;

internal static class Program {
    public const string SETTINGS_PATH = "./settings";
    public const string DATA_PATH = "./data";
    public const string BACKUP_PATH = "./backups";
    public static readonly Version _appVersion = new(5, 9);
    private static readonly ILog _log = LogManager.GetLogger("System");
    private static readonly ManualResetEvent shutdownEvent = new(false);
    private static readonly List<IModule> _modules = [];

    //programs entry point
    public static void Main() {
        Init();
        RunAsync().Wait();
    }

    //manages when the program is shut down
    public static async void Shutdown() {
        shutdownEvent.Set();

        List<Task> shutdown = new();
        foreach (IModule module in _modules) {
            shutdown.Add(module.Shutdown());
        }

        await Task.WhenAll(shutdown);
    }

    //awaits until the application is shut down
    public static Task WaitShutdownAsync() {
        return Task.Run(shutdownEvent.WaitOne);
    }

    //gets a loaded module
    public static T? GetModuleOfType<T>() where T : class, IModule {
        return (
            from module in _modules
            where module is T
            select module as T).FirstOrDefault();
    }

    //in charge for initializing the application
    private static void Init() {
        //init the log4net logger using the configuration
        XmlConfigurator.Configure(new FileInfo(Path.Combine(SETTINGS_PATH, "log4.config")));

        //init unhandled exception logging
        AppDomain.CurrentDomain.UnhandledException += (sender, exception) => _log.Fatal(exception.ToString());

#if DEBUG
        _log.Info($"Running application version: v{_appVersion} (DEBUG)");
#else
        _log.Info($"Running application version: v{_appVersion}");
#endif

        //load modules
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()) {
            if (!type.IsAbstract && type.IsClass && type.IsAssignableTo(typeof(IModule))) {
                try {
                    //init module
                    IModule module = Activator.CreateInstance(type) as IModule ?? throw new NullReferenceException("wasn't able to create an instance of the command");

                    //add the module
                    _modules.Add(module);
                    _log.Info($" |- loaded module: {type}");
                }
                catch (Exception ex) {
                    _log.Error($" |- something went wrong when initializing type '{type.FullName}':\n{ex}");
                }
            }
        }
    }

    //runs the application
    private static async Task RunAsync() {
        List<Task> runModules = [];

        for (int i = 0; i < _modules.Count; i++) {
            _log.Info($"starting applications: {MathF.Round((float)i / _modules.Count * 100)}% running '{_modules[i].GetType().Name}'...");
            Task task = _modules[i].Run();
            runModules.Add(task);
        }

        _log.Info("starting applications: 100% done!");

        await WaitShutdownAsync(); //await the shutdown event
        await Task.WhenAll(runModules); //await the modules from compleding
        _log.Info("the application quit safely!");
    }
}
