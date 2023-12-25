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

    public StartCmd() {
        _mcServer = Program.GetModuleOfType<MCServerModule>();

        CommandBuilder = new SlashCommandBuilder()
            .WithName("start")
            .WithDescription("starts the minecraft server");

        List<ApplicationCommandOptionChoiceProperties> serverOptions = new();
        foreach (string name in _mcServer.ServerNames) {
            ApplicationCommandOptionChoiceProperties choice = new();
            choice.Name = name;
            choice.Value = name;
            serverOptions.Add(choice);
        }

        CommandBuilder.AddOption("server-name", ApplicationCommandOptionType.String, "specefies the name of the server to target", true, choices: serverOptions.ToArray());
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
        await command.SetSuccess($"started {serverName}!");
    }
}
