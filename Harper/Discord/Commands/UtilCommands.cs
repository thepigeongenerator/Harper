using System;
using System.Threading.Tasks;
using Discord.Interactions;
using Harper.Discord.Commands;

namespace MinecraftServerApplication.Discord.Commands;
internal class UtilCommands : CommandHandler
{
    [SlashCommand("ping", "generic command which responds with 'pong'")]
    public async Task PingCmd()
    {
        if (await EnsurePermissions(CmdPerms.NONE)) return;
        TimeSpan latency = DateTime.UtcNow - Context.Interaction.CreatedAt;
        await SetInfo($"pong! **({Math.Round(latency.TotalMilliseconds, 2)}ms)**");
    }
}
