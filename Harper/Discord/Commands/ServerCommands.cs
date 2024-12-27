using System.Threading.Tasks;
using Discord.Interactions;
using Harper;

namespace MinecraftServerApplication.Discord.Commands;
internal class ServerCommands : CommandHandler
{
    private static Task exitTask;

    [SlashCommand("full-restart", "fully restarts the server WARN: HARPER AND SERVERS WILL BE OFFLINE FOR A WHILE")]
    public async Task FullRestartCmd()
    {
        await SetInfo("Server is restarting, it will be back online shortly!");
        exitTask = Core.Instance.Restart();
    }

    [SlashCommand("full-shutdown", "fully shuts down the server WARN: SHALL REQUIRE A MANUAL RESTART")]
    public async Task FullShutdownCmd()
    {
        await SetInfo("Server is shutting down, **The server must be restarted manually to go online again!**");
        exitTask = Core.Instance.Quit();
    }
}
