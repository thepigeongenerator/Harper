using Discord;
using Discord.Interactions;
using Harper;
using Harper.Discord.Commands;
using Harper.Minecraft;
using Harper.Util;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerApplication.Discord.Commands;
internal class MinecraftCommmands : CommandHandler
{
    private readonly MCServerManager serverManager;

    public MinecraftCommmands()
    {
        serverManager ??= Core.GetModuleOfType<MCServerManager>();
    }

    // get the server manager, but catch some common issues
    public async Task<bool> ServerManagerNull()
    {
        //check whether the server module is available
        if (serverManager == null)
        {
            await SetCritical("Failed to find the module in charge for running Minecraft servers!");
            return true;
        }

        return false;
    }

    // validate the server and catch common issues when getting the server
    public async Task<MCServer> GetServer(string name, Predicate<MCServer> predicate)
    {
        //find the server & check whether the it was found
        MCServer server = serverManager.GetServer(name);
        if (server == null)
        {
            await SetError($"couldn't find a server with the name `{name}`");
            return null;
        }

        //check the server's state
        if (predicate.Invoke(server) == false)
        {
            await SetError($"`{name}` has an illegal state!");
            return null;
        }

        return server;
    }

    [SlashCommand("info", "gets the info of the Minecraft server")]
    public async Task InfoCmd()
    {
        if (await InsufficientPerms(CmdPerms.NONE)) return;
        if (await ServerManagerNull()) return;

        StringBuilder response = new(2000); // discord's character limit

        foreach (string name in serverManager.ServerNames)
        {
            MCServer server = serverManager.GetServer(name);
            response.Append(
                $"### {name}:\n" +
                $"> state: {server.State}\n" +
                (server.Running ?
                    $"> threads: {server.serverProcess.Threads.Count}\n" +
                    $"> memory used: {StringUtil.FormatBytes((uint64)server.serverProcess.WorkingSet64)}\n" +
                    $"> responding: {server.serverProcess.Responding}\n" : "> no runtime data\n")
            );
        }

        response.Remove(response.Length - 1, 1); // remove the last character from the string
        await SetInfo(response.ToString());
    }

    [SlashCommand("whitelist", "adds a player to the whitelist")]
    public async Task WhitelistCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(AutoCompleters.CanStopServers))] string name, [Summary("username", "specifies the minecraft javausername you want added to the whitelist")] string username)
    {
        if (await InsufficientPerms(CmdPerms.ADD_WHITELIST)) return;
        MCServer server = await GetServer(name, s => s.Running);
        if (server == null) return;

        server.SendCommand($"whitelist add {username}");
        await SetInfo($"executed the command to whitelist `{username}` on `{name}`!");
    }

    [SlashCommand("start", "starts the minecraft server")]
    public async Task StartCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(AutoCompleters.CanStartServers))] string name)
    {
        if (await InsufficientPerms(CmdPerms.MANAGE_MC_SERVER)) return;
        MCServer server = await GetServer(name, s => s.CanStart);
        if (server == null) return;

        await SetInfo($"starting `{name}`...");
        server.Start();
        await SetSuccess($"started `{name}`!");
    }

    [SlashCommand("stop", "stops the minecraft server, if it takes too long the process is killed instead.")]
    public async Task StopCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(AutoCompleters.CanStopServers))] string name)
    {
        if (await InsufficientPerms(CmdPerms.MANAGE_MC_SERVER)) return;
        MCServer server = await GetServer(name, s => s.CanStop);
        if (server == null) return;

        await SetInfo($"shutting down `{name}`...");
        await server.Stop();
        await SetSuccess($"`{name}` was shut down!");
    }

    [SlashCommand("restart", "restarts the minecraft server. If stopping takes too long the process is killed instead.")]
    public async Task RestartCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(AutoCompleters.CanStopServers))] string name)
    {
        if (await InsufficientPerms(CmdPerms.MANAGE_MC_SERVER)) return;
        MCServer server = await GetServer(name, s => s.CanStop);
        if (server == null) return;

        await SetInfo($"shutting down `{name}`...");
        await server.Stop();
        await SetInfo($"`{name}` was shut down! restarting...");
        server.Start();
        await SetSuccess("server has been restarted!");
    }

    [SlashCommand("kill", "forcefully kills a minecraft server *only run if there is an issue*")]
    public async Task KillCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(AutoCompleters.CanKillServers))] string name)
    {
        if (await InsufficientPerms(CmdPerms.MANAGE_MC_SERVER)) return;
        MCServer server = await GetServer(name, s => s.CanKill);
        if (server == null) return;

        await SetInfo($"killing `{name}`...");
        await server.Kill();
        await SetSuccess($"killed `{name}`!");
    }

    [SlashCommand("backup", "manually creates a backup of the current state of the server.")]
    public async Task BackupCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(AutoCompleters.AllServers))] string name)
    {
        if (await InsufficientPerms(CmdPerms.MANAGE_MC_SERVER)) return;
        MCServer server = await GetServer(name, s => true);
        if (server == null) return;

        TimeSpan time = default;
        string fpath = null;
        await SetInfo($"creating a backup for `{name}`, be patient, this might take a while. *note, the server will be restarted if it was running prior.*");
        bool failed = await ErrorHandler.CatchError(
            async () => (time, fpath) = await server.MakeBackup()
        );

        if (failed || string.IsNullOrWhiteSpace(fpath))
            await SetCritical($"something went terribly wrong when creating a backup for {name}! (busy for {Math.Round(time.TotalSeconds, 1)})");
        else
        {
            uint64 len = (uint64)(new FileInfo(fpath).Length);
            await SetSuccess($"a backup has successfully been made for {name}! It took {Math.Round(time.TotalSeconds, 1)}s to perform this task.\n"
                + $"Backup size: {StringUtil.FormatBytes(len)}");
        }
    }
}
