using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Harper.Discord;
using Harper.Logging;
using Harper.Minecraft;
using Harper.Minecraft.Data;
using Harper.Util;
using log4net;

namespace Harper;

public class Core : IDisposable
{
    private static Core instance;
    public static Core Instance => instance;

    private readonly ILog log = null;
    private readonly IModule[] modules;                 // contains the modules of this application
    private readonly ManualResetEvent exited = null;    // signals when the application has quit
    private readonly ErrorHandler errorHandler = null;  // for handling errors
    private int8 exitCode = 1;                          // assume an exit code of 1; failure
    private bool running = false;
    private bool disposed = false;

    // properties
    public int8 ExitCode => exitCode;

    // constructor
    public Core()
    {
        if (instance != null)
            throw new InvalidOperationException("only one instance of Core may exist");
        instance = this;

        Log.Initialize();
        log = this.GetLogger();
        exited = new ManualResetEvent(false);
        errorHandler = new ErrorHandler(this, log);


        {
            FileVersionInfo ver = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        #if DEBUG
        log.Info($"Running application version: v{ver.FileVersion} (DEBUG)");
#else
        log.Info($"Running application version: v{ver.FileVersion}");
#endif
        }
        log.Debug($"cwd: {Directory.GetCurrentDirectory()}");

        modules = [
            new DiscordBot(),
            new MCServerManager(),
        ];
    }

    // executes something for each module
    private Task ForEachModule(Func<IModule, Task> exec)
    {
        List<Task> tasks = new(modules.Length);
        return Task.WhenAll(TaskUtil.ForEachTask<IModule>(mod => exec.Invoke(mod), modules));
    }

    // called when the program is executed, keeps the thread until it's finished executing
    public void Run()
    {
        running = true;
        ForEachModule(m =>
        {
            log.Info($"starting {m.GetType().Name}");
            return m.Start();
        }).Wait();

        // wait for the application to exit
        exited.WaitOne();
        running = false;
    }

    // called when the program needs to stop
    public async Task Quit(int8 exitCode = 0)
    {
        if (running == false)
            return;

        await ForEachModule(m => m.Stop());

        this.exitCode = exitCode;
        exited.Set();
    }

    public async Task Restart()
    {
        await Quit(2); // return an exit code of 2, signifying that the application should restart instead
    }

    // called when the program needs to stop immediately, cleans up all resources as fast as possible
    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        GC.SuppressFinalize(this);

        // dispose all modules, and remove their references to this program to make them viable for garbage collection
        for (uint32 i = 0; i < modules.Length; i++)
        {
            modules[i].Dispose();
            modules[i] = null;
        }

        exitCode = 0;
        exited.Set();
    }

    public static T GetModuleOfType<T>() where T : IModule, new()
    {
        return (
            from mod in Instance.modules.AsEnumerable()
            where mod is T
            select (T)mod)
            .FirstOrDefault();
    }
}
