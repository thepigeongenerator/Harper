using Discord.Interactions;

namespace MinecraftServerApplication.Discord.Commands;
internal class ServerCommands : CommandHandler {
    private readonly HarperModule _harper;

    public ServerCommands()
    {
        _harper = Program.GetModuleOfType<HarperModule>();
    }

    [SlashCommand("full-restart", "fully restarts the server WARN: HARPER AND SERVERS WILL BE OFFLINE FOR A WHILE")]
    public async Task FullRestartCmd() {
        await SetInfo("Server is restarting, it will be back online shortly!");
        Program.Shutdown(); //
    }

    [SlashCommand("full-shutdown", "fully shuts down the server WARN: SHALL REQUIRE A MANUAL RESTART")]
    public async Task FullShutdownCmd() {
        await SetInfo("Server is shutting down, **The server must be restarted manually to go online again!**");
        _harper.keepAlive = true;
        Program.Shutdown();
    }
}
