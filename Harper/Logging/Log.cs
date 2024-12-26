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
    public static void Initialize()
    {
        // create the pattern layout
        PatternLayout patternLayout = new("[%date{HH:mm:ss}] %-5level %-20.20logger - %message%newline");
        patternLayout.ActivateOptions();

        // create the console appender with the pattern layout
        ConsoleAppender console = new()
        {
            Layout = patternLayout
        };
        console.ActivateOptions();

        // configure the root logger with a treshold
        var hierarchy = (Hierarchy)LogManager.GetRepository();
        hierarchy.Root.AddAppender(console);
#if DEBUG
        hierarchy.Root.Level = log4net.Core.Level.Debug;
#else
        hierarchy.Root.Level = log4net.Core.Level.Info;
#endif
        hierarchy.Configured = true;

        // configure the repository with the appender
        BasicConfigurator.Configure(console);
    }

    public static ILog GetLogger(this object obj) => LogManager.GetLogger(obj.GetType());
}
