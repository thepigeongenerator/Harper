using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Harper.Discord;
using Harper.Logging;
using Harper.Minecraft;
using Harper.Minecraft.Data;
using log4net;

namespace Harper;

public class Core : IDisposable
{
    private readonly ILog log = null;
    private readonly IModule[] modules;                 // contains the modules of this application
    private readonly ManualResetEvent exited = null;    // signals when the application has quit
    private readonly ErrorHandler errorHandler = null;
    private int8 exitCode = 1;                          // assume an exit code of 1; failure
    private bool running = false;
    private bool disposed = false;

    // properties
    public int8 ExitCode => exitCode;

    // constructor
    public Core()
    {
        Log.Initialize();
        log = this.GetLogger();
        exited = new ManualResetEvent(false);
        errorHandler = new ErrorHandler(this, log);

        modules = [
            new DiscordBot(),
            new MCServerManager(),
        ];
    }

    // executes something for each module
    private Task ForEachModule(Func<IModule, Task> exec)
    {
        List<Task> tasks = new(modules.Length);

        foreach (IModule mod in modules)
            tasks.Add(exec.Invoke(mod));

        return Task.WhenAny(tasks);
    }

    // called when the program is executed, keeps the thread until it's finished executing
    public void Run()
    {
        running = true;
        ForEachModule(m => m.Start()).Wait();

        // wait for the application to exit
        exited.WaitOne();
        running = false;
    }

    // called when the program needs to stop
    public async Task Quit(uint8 exitCode = 0)
    {
        if (running == false)
            return;

        await ForEachModule(m => m.Stop());

        // lastly; dispose of everything
        Dispose();
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
}
