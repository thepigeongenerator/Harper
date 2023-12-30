using Discord;
using MinecraftServerApplication.Minecraft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerApplication.Discord.Commands;
internal class InfoCmd : Command {
    private MCServerModule _mcServer;

    public InfoCmd() {
        CommandBuilder = new SlashCommandBuilder()
            .WithName("info")
            .WithDescription("gets info of the minecraft servers that are running");

        _mcServer = Program.GetModuleOfType<MCServerModule>();
    }

    public override async Task Run(CommandHandler command) {
        static string FormatBytes(long bytes) {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;

            while (bytes >= 1024 && suffixIndex < suffixes.Length - 1) {
                bytes /= 1024;
                suffixIndex++;
            }

            return $"{bytes} {suffixes[suffixIndex]}";
        }

        string response = string.Empty;

        foreach (string name in _mcServer.ServerNames) {
            MinecraftServer server = _mcServer.TryGetServer(name) ?? throw new NullReferenceException($"couldn't find a server with the name {name}");
            response += $"### {name}:\n" +
                $"> running: {server.Running}\n";
            if (server.Running) {
                Process serverProcess = server.ServerProcess;
                response +=
                    $"> threads: {serverProcess.Threads.Count}\n" +
                    $"> memory used: {FormatBytes(serverProcess.WorkingSet64)}\n" +
                    $"> responding: {serverProcess.Responding}\n" +
                    $"> running: `{(DateTime.Now - serverProcess.StartTime).ToString(@"hh\:mm")}`\n";
            }
        }

        response = response[..^1]; //exclude the last character
        await command.SetInfo(response);
    }
}
