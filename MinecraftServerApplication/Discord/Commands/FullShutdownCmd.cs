using Discord;

namespace MinecraftServerApplication.Discord.Commands;
internal class FullShutdownCmd : Command {
    public FullShutdownCmd() {
        CommandBuilder = new SlashCommandBuilder()
            .WithName("full-shutdown")
            .WithDescription("fully shuts down the server WARN: SHALL REQUIRE A MANUAL RESTART");
    }

    public override async Task Run(CommandHandler command) {
        await command.SetInfo("Server is shutting down, **The server must be restarted manually to go online again!**");
        Harper.keepAlive = true;
        Program.Shutdown();
    }
}
