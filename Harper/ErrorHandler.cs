using System;
using System.Runtime.InteropServices;
using log4net;

namespace Harper;

public class ErrorHandler
{
    private readonly Core core;
    private readonly ILog log;

    public ErrorHandler(Core core, ILog log)
    {
        this.core = core;
        this.log = log;

        // subscribe to exit events
        PosixSignalRegistration.Create(PosixSignal.SIGINT, c => PosixSignalHandler(c, ExitGracefully));
        PosixSignalRegistration.Create(PosixSignal.SIGQUIT, c => PosixSignalHandler(c, ExitGracefully));
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, c => PosixSignalHandler(c, ExitImmediately));
        AppDomain.CurrentDomain.ProcessExit += (s, a) => ExitGracefully();
        AppDomain.CurrentDomain.UnhandledException += (s, a) => ExitImmediately();
    }


    // allows the program's execution to run it's corse, then let execution terminate
    private void ExitGracefully()
    {
        core.Quit().Wait();
    }

    // call dispose to dispose of everything as fast as possible
    private void ExitImmediately()
    {
        log.Error("exiting immediately! this might cause data loss.");

        core.Dispose();
    }

    private void PosixSignalHandler(PosixSignalContext context, Action exec)
    {
        log.Info($"processing posix signal: {context.Signal}");
        context.Cancel = true;
        exec.Invoke();
    }
}
