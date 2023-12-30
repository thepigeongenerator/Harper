using Discord;
using MinecraftServerApplication.Minecraft;

namespace MinecraftServerApplication.Discord.Commands;
internal class StopCmd : Command {
    private MCServerModule _mcServer;

    public StopCmd() {
        _mcServer = Program.GetModuleOfType<MCServerModule>();

        CommandBuilder = new SlashCommandBuilder()
            .WithName("stop")
            .WithDescription("stops the minecraft server");

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

        if (server.Running == false) {
            await command.SetError($"{serverName} is already shut down!");
            return;
        }

        await command.SetInfo($"shutting down {serverName}...");
        await server.Stop();
        await command.SetSuccess($"{serverName} was shut down!");
    }
}
