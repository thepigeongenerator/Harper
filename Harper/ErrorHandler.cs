using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Harper;

public class ErrorHandler
{
    private static ErrorHandler instance = null;
    private readonly Core core = null;
    private readonly ILog log = null;

    public ErrorHandler(Core core, ILog log)
    {
        instance = this;

        this.core = core;
        this.log = log;

        // subscribe to exit events
        PosixSignalRegistration.Create(PosixSignal.SIGINT, c => PosixSignalHandler(c, ExitGracefully));
        PosixSignalRegistration.Create(PosixSignal.SIGQUIT, c => PosixSignalHandler(c, ExitGracefully));
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, c => PosixSignalHandler(c, ExitImmediately));
        AppDomain.CurrentDomain.ProcessExit += (s, a) => ExitGracefully();
        AppDomain.CurrentDomain.UnhandledException += (s, a) => ExitImmediately((Exception)a.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (s, a) => ExitImmediately((Exception)a.Exception);
    }


    // allows the program's execution to run it's corse, then let execution terminate
    private void ExitGracefully()
    {
        core.Quit().Wait();
    }

    // call dispose to dispose of everything as fast as possible
    private void ExitImmediately() => ExitImmediately(null);
    private void ExitImmediately(Exception e)
    {
        if (e == null)
            log.Warn("exiting immediately! this might cause data loss.");
        else
        {
            log.Fatal($"exiting due to an exception! this might cause data loss. {e.GetType().Name}: {e.Message}");
            log.Debug(e);
        }

        core.Dispose();
    }

    private void PosixSignalHandler(PosixSignalContext context, Action exec)
    {
        log.Info($"processing posix signal: {context.Signal}");
        context.Cancel = true;
        exec.Invoke();
    }

    public static Task CatchError(Func<Task> act)
    {
        try
        {
            act.Invoke().Wait();
        }
        catch (Exception e)
        {
#if DEBUG
            instance.ExitImmediately(e);
#else
            instance.log.Error($"an exception occurred but was caught! {e.GetType().Name}: {e.Message}");
            instance.log.Debug(e);
#endif
        }

        return Task.CompletedTask;
    }
}
