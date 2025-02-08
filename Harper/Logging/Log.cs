using System;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.Repository;
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

        // create the console appender with the pattern layout
        AnsiColorTerminalAppender console = new()
        {
            Layout = pattern
        };

        AnsiColorTerminalAppender.LevelColors[] colours = [
            new() { Level = Level.Debug, ForeColor = AnsiColorTerminalAppender.AnsiColor.Magenta },
            new() { Level = Level.Info, ForeColor = AnsiColorTerminalAppender.AnsiColor.White },
            new() { Level = Level.Warn, ForeColor = AnsiColorTerminalAppender.AnsiColor.Yellow },
            new() { Level = Level.Error, ForeColor = AnsiColorTerminalAppender.AnsiColor.Red },
            new() { Level = Level.Fatal, ForeColor = AnsiColorTerminalAppender.AnsiColor.White, BackColor = AnsiColorTerminalAppender.AnsiColor.Red },
        ];

        foreach (AnsiColorTerminalAppender.LevelColors mapping in colours)
            console.AddMapping(mapping);

        console.ActivateOptions();

        // configure the root logger with a treshold
        var hierarchy = (Hierarchy)LogManager.GetRepository();
        hierarchy.Root.AddAppender(console);
        hierarchy.Configured = true;

        if (LogDebug())
            hierarchy.Root.Level = log4net.Core.Level.Debug;
        else
            hierarchy.Root.Level = log4net.Core.Level.Info;

        // configure the repository with the appender
        BasicConfigurator.Configure(console);
    }

    public static ILog GetLogger(this object obj) => LogManager.GetLogger(obj.GetType());
}
