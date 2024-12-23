using System;
using Harper.Discord;
using Harper.Logging;
using Harper.Minecraft;
using log4net;

namespace Harper;

public class Core : IDisposable
{
    private readonly ILog log = null;
    private readonly DiscordBot discordBot = null;
    private readonly MCServerManager serverManager = null;
    private int8 exitCode = 1;       // assume an exit code of 1; failure
    private bool disposed = false;

    // properties
    public int8 ExitCode => exitCode;

    // constructor
    public Core()
    {
        Log.Initialize();
        log = this.GetLogger();
    }

    // called when the program is executed, keeps the thread until it's finished executing
    public void Run()
    { }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        GC.SuppressFinalize(this);

        discordBot.Dispose();
        serverManager.Dispose();
    }
}
