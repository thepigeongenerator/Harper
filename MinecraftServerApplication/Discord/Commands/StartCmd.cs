using Discord;
using Discord.WebSocket;
using MinecraftServerApplication.Minecraft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerApplication.Discord.Commands;
internal class StartCmd : Command {
    private MCServerModule _mcServer;

    public StartCmd() : base(new SlashCommandBuilder()
        .WithName("start")
        .WithDescription("starts the minecraft server")
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

        if (server.Running == true) {
            await command.SetError($"{serverName} is already running!");
            return;
        }

        await command.SetInfo($"starting {serverName}...");
        await server.Run();
    }
}
