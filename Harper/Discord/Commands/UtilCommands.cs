using System;
using System.Threading.Tasks;
using Discord.Interactions;

namespace MinecraftServerApplication.Discord.Commands;
internal class UtilCommands : CommandHandler
{
    [SlashCommand("ping", "generic command which responds with 'pong'")]
    public async Task PingCmd()
    {
        TimeSpan latency = DateTime.UtcNow - Context.Interaction.CreatedAt.ToUniversalTime();
        await SetInfo($"pong! **({Math.Round(latency.TotalSeconds, 2)}ms)**");
    }
}
