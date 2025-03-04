using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Harper;

// handles the errors that may occur in the program, allows for catching errors if they're deeply nested in multi-threaded mind-fuckery
public class ErrorHandler
{
    private static ErrorHandler instance = null;                        // will contain the latest instance for ease of access
    private readonly Core core = null;                                  // a reference to the core, so we can send the appropriate signals to the core (quit / dispose)
    private readonly ILog log = null;                                   // reference to the core's logger for logging
    private readonly PosixSignalRegistration[] registrations = null;    // stores the posix signal registrations so we can unsubscribe them later

    // anonymous functions to allow for later unsubscribing
    private Action<PosixSignalContext> PosixStopHandle => c => PosixSignalHandler(c, ExitGracefully);
    private Action<PosixSignalContext> PosixTermHandle => c => PosixSignalHandler(c, ExitImmediately);
    private EventHandler EventStopHandle => (s, a) => ExitGracefully();
    private UnhandledExceptionEventHandler UnhandledExceptionHandle => (s, a) => ExitImmediately((Exception)a.ExceptionObject);
    private EventHandler<UnobservedTaskExceptionEventArgs> TaskExceptionHandle => (s, a) => ExitImmediately((Exception)a.Exception);

    // constructor
    public ErrorHandler(Core core, ILog log)
    {
        instance = this;

        this.core = core;
        this.log = log;

        // subscribe to exit events
        registrations = [
            PosixSignalRegistration.Create(PosixSignal.SIGINT, PosixStopHandle),
            PosixSignalRegistration.Create(PosixSignal.SIGQUIT, PosixStopHandle),
            PosixSignalRegistration.Create(PosixSignal.SIGTERM, PosixTermHandle),
        ];
        AppDomain.CurrentDomain.ProcessExit += EventStopHandle;
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandle;
        TaskScheduler.UnobservedTaskException += TaskExceptionHandle;
    }

    // destructor
    ~ErrorHandler()
    {
        // unsubscribe from exit events
        foreach (var reg in registrations)
            reg.Dispose();

        AppDomain.CurrentDomain.ProcessExit -= EventStopHandle;
        AppDomain.CurrentDomain.UnhandledException -= UnhandledExceptionHandle;
        TaskScheduler.UnobservedTaskException -= TaskExceptionHandle;
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
            log.Fatal(e);
        }

        core.Dispose();
    }

    private void PosixSignalHandler(PosixSignalContext context, Action exec)
    {
        log.Info($"processing posix signal: {context.Signal}");
        context.Cancel = true;
        exec.Invoke();
    }

    // catches upon errors, returns "true" if one occurred
    public static async Task<bool> CatchError(Func<Task> act)
    {
        try
        {
            await act.Invoke();
        }
        catch (Exception e)
        {
#if DEBUG
            instance.ExitImmediately(e);
#else
            instance.log.Error($"an exception occurred but was caught! {e.GetType().Name}: {e.Message}");
            instance.log.Error(e);
#endif
            return true;
        }

        return false;
    }
}
