using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Harper.Discord;
using Harper.Logging;
using Harper.Minecraft;
using log4net;

namespace Harper;

public class Core : IDisposable
{
    private readonly ILog log = null;
    private readonly IModule[] modules;                 // contains the modules of this application
    private readonly ManualResetEvent exited = null;    // signals when the application has quit
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

        // subscribe to exit events
        PosixSignalRegistration.Create(PosixSignal.SIGINT, c => PosixSignalHandler(c, ExitGracefully));
        PosixSignalRegistration.Create(PosixSignal.SIGQUIT, c => PosixSignalHandler(c, ExitGracefully));
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, c => PosixSignalHandler(c, ExitImmediately));
        AppDomain.CurrentDomain.ProcessExit += (s, a) => ExitGracefully();
        AppDomain.CurrentDomain.UnhandledException += (s, a) => ExitImmediately();
    }

    // called when the program has started
    private async Task Start() { }

    // called when the program needs to stop
    public async Task Stop()
    {
        if (running == false)
            return;

        // lastly; dispose of everything
        Dispose();
    }

    // called when the program is executed, keeps the thread until it's finished executing
    public void Run()
    {
        running = true;
        Start().Wait();

        // wait for the application to exit
        exited.WaitOne();
        running = false;
    }

    // allows the program's execution to run it's corse, then let execution terminate
    private void ExitGracefully()
    {
        Stop().Wait();
    }

    // call dispose to dispose of everything as fast as possible
    private void ExitImmediately()
    {
        log.Error("exiting immediately! this might cause data loss.");

        Dispose();
    }

    private void PosixSignalHandler(PosixSignalContext context, Action exec)
    {
        log.Info($"processing posix signal: {context.Signal}");
        context.Cancel = true;
        exec.Invoke();
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
