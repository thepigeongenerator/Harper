
using Discord;

namespace MinecraftServerApplication.Discord.Commands;
internal class PingCmd : Command {
    public PingCmd() {
        CommandBuilder = new SlashCommandBuilder()
            .WithName("ping")
            .WithDescription("generic command for testing commands");
    }

    public override async Task Run(CommandHandler command) {
        await command.SetInfo("pong!");
    }
}
