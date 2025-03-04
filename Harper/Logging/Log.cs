using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Filter;
using log4net.Repository.Hierarchy;

namespace Harper.Logging;

public static class Log
{
    public static bool LogDebug()
    {
#if DEBUG
        return true;
#else
        string env = Environment.GetEnvironmentVariable(ENV_HARPER_DEBUG);
        if (int32.TryParse(env, out int32 i))
            return i != 0;
        return false;
#endif
    }

    public static void Initialize()
    {
        // create the pattern layout
        PatternLayout pattern = new("[%date{HH:mm:ss}] %-8level %-32.32logger{1} - %message%newline");
        pattern.ActivateOptions();

        // create the console appender printing to stdout
        AnsiColorTerminalAppender stdout = new()
        {
            Layout = pattern,
            Target = "Console.Out",
        };
        stdout.AddMapping(new() { Level = Level.Debug, ForeColor = AnsiColorTerminalAppender.AnsiColor.Magenta });
        stdout.AddMapping(new() { Level = Level.Info, ForeColor = AnsiColorTerminalAppender.AnsiColor.White });
        stdout.AddMapping(new() { Level = Level.Warn, ForeColor = AnsiColorTerminalAppender.AnsiColor.Yellow });
        stdout.AddFilter(new LevelRangeFilter() { LevelMin = Level.Debug, LevelMax = Level.Warn });
        stdout.ActivateOptions();

        // create a console appender printing to stderr
        AnsiColorTerminalAppender stderr = new()
        {
            Layout = pattern,
            Target = "Console.Error",
        };
        stderr.AddMapping(new() { Level = Level.Error, ForeColor = AnsiColorTerminalAppender.AnsiColor.Red });
        stderr.AddMapping(new() { Level = Level.Fatal, ForeColor = AnsiColorTerminalAppender.AnsiColor.White, BackColor = AnsiColorTerminalAppender.AnsiColor.Red });
        stderr.AddFilter(new LevelRangeFilter() { LevelMin = Level.Error });
        stderr.ActivateOptions();

        // configure the root logger with a threshold
        var hierarchy = (Hierarchy)LogManager.GetRepository();
        hierarchy.Root.AddAppender(stdout);
        hierarchy.Root.AddAppender(stderr);
        hierarchy.Configured = true;

        if (LogDebug())
            hierarchy.Root.Level = log4net.Core.Level.Debug;
        else
            hierarchy.Root.Level = log4net.Core.Level.Info;

        // configure the repository with the appenders
        BasicConfigurator.Configure(stdout, stderr);
    }

    public static ILog GetLogger(this object obj) => LogManager.GetLogger(obj.GetType());
}
