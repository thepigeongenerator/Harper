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
        PosixSignalRegistration.Create(PosixSignal.SIGINT, PosixSignalReceived);
        PosixSignalRegistration.Create(PosixSignal.SIGQUIT, PosixSignalReceived);
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, PosixSignalReceived);
        AppDomain.CurrentDomain.ProcessExit += ExitGracefully;
        AppDomain.CurrentDomain.UnhandledException += ExitImmediately;
    }

    // called when the program has started
    private async Task Start() { }

    // called when the program needs to stop
    public async Task Stop()
    {
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

    private void ExitGracefully(object sender = null, EventArgs args = null)
    {
        // call stop to allow the program's execution to run it's corse if it's still running
        Stop().Wait();
    }

    private void ExitImmediately(object sender = null, EventArgs args = null)
    {
        log.Error("exiting immediately! this might cause data loss.");

        // call dispose to dispose of everything as fast as possible
        Dispose();
    }

    // handles posix signals
    private void PosixSignalReceived(PosixSignalContext context)
    {
        log.Info($"received the '{context.Signal}' signal!");

        Action action = context.Signal switch
        {
            PosixSignal.SIGINT => () => ExitGracefully(),
            PosixSignal.SIGQUIT => () => ExitGracefully(),
            PosixSignal.SIGTERM => () => ExitImmediately(),

            _ => () => Throw(log, new InvalidOperationException($"signal {context.Signal} is unknown!")),
        };

        // cancel the default response, and handle it using the action
        context.Cancel = true;
        action.Invoke();
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
