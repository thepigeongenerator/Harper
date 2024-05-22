using Discord;
using Discord.Interactions;
using MinecraftServerApplication.Minecraft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerApplication.Discord.Commands;
internal class MinecraftCommmands : CommandHandler
{
    private const string FAIL_MC_SERVER_MODULE = "Failed to find the module in charge for running Minecraft servers!";
    private readonly MCServerModule? _mcServer;

    public MinecraftCommmands()
    {
        _mcServer = Program.GetModuleOfType<MCServerModule>();
    }

    public async Task<MinecraftServer?> GetServer(string serverName, State matchEither)
    {
        //check whether the server module is available
        if (_mcServer == null)
        {
            await SetCritical(FAIL_MC_SERVER_MODULE);
            return null;
        }

        //find the server & check whether the it was found
        MinecraftServer? server = _mcServer.TryGetServer(serverName);
        if (server == null)
        {
            await SetError($"couldn't find a server with the name `{serverName}`");
            return null;
        }

        //check the server's state
        if ((server.State & matchEither) == 0)
        {
            await SetError($"`{serverName}` has an illegal state `{server.State.ToString()}`!");
            return null;
        }

        return server;
    }

    #region commands
    #region info cmd
    [SlashCommand("info", "gets the info of the Minecraft server")]
    public async Task InfoCmd()
    {
        static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;

            while (bytes >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                bytes /= 1024;
                suffixIndex++;
            }

            return $"{bytes} {suffixes[suffixIndex]}";
        }

        //if the minecraft server can't be found
        if (_mcServer == null)
        {
            await SetCritical(FAIL_MC_SERVER_MODULE);
            return;
        }

        string response = string.Empty;

        foreach (string name in _mcServer.ServerNames)
        {
            MinecraftServer server = _mcServer.TryGetServer(name) ?? throw new NullReferenceException($"couldn't find a server with the name {name}");
            response += $"### {name}:\n" +
                $"> state: {server.State}\n";
            // if the server is running
            if (server.State is State.RUNNING)
            {
                Process serverProcess = server.ServerProcess;
                response +=
                    $"> threads: {serverProcess.Threads.Count}\n" +
                    $"> memory used: {FormatBytes(serverProcess.WorkingSet64)}\n" +
                    $"> responding: {serverProcess.Responding}\n";
            }
        }

        response = response[..^1]; //exclude the last character
        await SetInfo(response);
    }
    #endregion //info cmd

    #region run-function cmd
    [SlashCommand("run-function", "runs a pre-programmed function on the selected server")]
    public async Task RunFunctionCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(AutoCompleters.CanStopServers))] string serverName, [Summary("function-name", "the pre-programmed function"), Autocomplete(typeof(AutoCompleters.PreprogrammedFunctions))] string functionName)
    {
        MinecraftServer? server = await GetServer(serverName, State.CAN_STOP);

        if (server == null || _mcServer == null)
        {
            return;
        }

        if (_mcServer.TryGetFunction(functionName) == null)
        {
            await SetError($"couldn't find a function with the name `{functionName}`");
            return;
        }

        await SetInfo($"executing function `{functionName}`...");
        server.RunFunction(functionName);
        await SetSuccess($"successfully executed the function `{functionName}`!");
    }
    #endregion //run-function cmd

    #region start cmd
    [SlashCommand("start", "starts the minecraft server")]
    public async Task StartCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(AutoCompleters.CanStartServers))] string serverName)
    {
        MinecraftServer? server = await GetServer(serverName, State.CAN_START);

        if (server == null)
        {
            return;
        }

        //check whether the state is ERROR before starting (becomes ERROR if there were issues when starting it last or )
        if (server.State is State.ERROR)
        {
            await SetWarning($"an error occured when last starting `{serverName}`! Starting anyway...");
        }
        else
        {
            await SetInfo($"starting `{serverName}`...");
        }

        //run the actual server
        await server.Run();

        //if the server's state is now ERROR
        if (server.State is State.ERROR)
        {
            await SetError($"an error occured when starting `{serverName}`!");
            return;
        }

        await SetSuccess($"started `{serverName}`!");
    }
    #endregion //start cmd

    #region stop cmd
    [SlashCommand("stop", "stops the minecraft server")]
    public async Task StopCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(AutoCompleters.CanStopServers))] string serverName)
    {
        MinecraftServer? server = await GetServer(serverName, State.CAN_STOP);

        if (server == null)
        {
            return;
        }

        await SetInfo($"shutting down `{serverName}`... (this can take a while if a backup is being made)");
        DateTime start = DateTime.Now;
        await server.Stop();
        TimeSpan duration = DateTime.Now - start;
        await SetSuccess($"`{serverName}` was shut down! (took {Math.Round(duration.TotalSeconds, 1)}s)");
    }
    #endregion //stop cmd

    #region restart cmd
    [SlashCommand("restart", "restarts the minecraft server")]
    public async Task RestartCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(AutoCompleters.CanStopServers))] string serverName)
    {
        MinecraftServer? server = await GetServer(serverName, State.CAN_STOP);

        if (server == null)
        {
            return;
        }

        await SetInfo($"shutting down `{serverName}`... (this can take a while if a backup is being made)");
        DateTime start = DateTime.Now;
        await server.Stop();
        TimeSpan duration = DateTime.Now - start;
        await SetSuccess($"`{serverName}` was shut down! (took {Math.Round(duration.TotalSeconds, 1)}s) restarting...");
        await server.Run();
    }
    #endregion //restart cmd

    #region kill cmd
    [SlashCommand("kill", "forcefully kills a minecraft server *only run if there is an issue*")]
    public async Task KillCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(AutoCompleters.CanKillServers))] string serverName)
    {
        MinecraftServer? server = await GetServer(serverName, State.CAN_KILL);

        if (server == null)
        {
            return;
        }

        await SetInfo($"killing `{serverName}`...");
        server.Kill();
        await SetSuccess($"killed `{serverName}`!");
    }
    #endregion //restart cmd
    #endregion //commands
}
