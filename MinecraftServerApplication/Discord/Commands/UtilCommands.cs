using Discord.Interactions;

namespace MinecraftServerApplication.Discord.Commands;
internal class UtilCommands : CommandHandler {
    [SlashCommand("ping", "generic command which responds with 'pong'")]
    public async Task PingCmd() {
        await RespondAsync("pong!");
    }
}
