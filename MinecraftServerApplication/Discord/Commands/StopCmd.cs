using Discord;
using Discord.WebSocket;
using MinecraftServerApplication.Minecraft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerApplication.Discord.Commands;
internal class StopCmd : Command {
    private MCServerModule _mcServer;

    public StopCmd() : base(new SlashCommandBuilder()
        .WithName("stop")
        .WithDescription("stops the minecraft server")
        .AddOption("server-name", ApplicationCommandOptionType.String, "specefies the name of the server to target", true)
        ) {
        _mcServer = Program.GetModuleOfType<MCServerModule>();
    }

    public override async Task Run(CommandHandler command) {
        var option = command.TryGetOption("server-name") ?? throw new NullReferenceException();
        string serverName = (string)option.Value;

        MinecraftServer? server = _mcServer.TryGetServer(serverName);
        if (server == null) {
            await command.SetError($"couldn't find a server with the name `{serverName}`");
            return;
        }

        if (server.Running == false) {
            await command.SetError($"{serverName} is already shut down!");
            return;
        }

        await command.SetInfo($"shutting down {serverName}...");
        await server.Stop();
        await command.SetSuccess($"{serverName} was shut down!");
    }
}
