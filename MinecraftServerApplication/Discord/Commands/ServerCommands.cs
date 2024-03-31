using Discord.Interactions;

namespace MinecraftServerApplication.Discord.Commands;
internal class ServerCommands : CommandHandler
{
    private readonly HarperModule _harper;

    public ServerCommands()
    {
        _harper = Program.GetModuleOfType<HarperModule>() ?? throw new NullReferenceException("couldn't find harper module");
    }

    [SlashCommand("full-restart", "fully restarts the server WARN: HARPER AND SERVERS WILL BE OFFLINE FOR A WHILE")]
    public async Task FullRestartCmd()
    {
        await SetInfo("Server is restarting, it will be back online shortly!");
        Program.Shutdown(1); //give an exit code of 1; meaning the program exited with no faults, but should still restart
    }

    [SlashCommand("full-shutdown", "fully shuts down the server WARN: SHALL REQUIRE A MANUAL RESTART")]
    public async Task FullShutdownCmd()
    {
        await SetInfo("Server is shutting down, **The server must be restarted manually to go online again!**");
        Program.Shutdown(0); //give an exit code of 0; meaning the program exited with no faults
    }
}
