using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Harper;
using Harper.Discord;
using Harper.Discord.Commands;

namespace MinecraftServerApplication.Discord;
public abstract class CommandHandler : InteractionModuleBase
{
    private static DiscordBot harper;

    private static DiscordBot GetHarper()
    {
        harper ??= Core.GetModuleOfType<DiscordBot>();
        return harper;
    }

    // edits the response to the original response of the slash command
    private async Task CreateResponse(string msg, Color colour)
    {
        Embed embed = new EmbedBuilder()
            .WithDescription(msg)
            .WithColor(colour)
            .Build();

        await FollowupAsync(embed: embed);
    }

    public async Task Respond(string msg, Color colour) => await ErrorHandler.CatchError(async () => await CreateResponse(msg, colour));
    public async Task SetSuccess(string msg) => await Respond(msg, Color.Green);
    public async Task SetInfo(string msg) => await Respond(msg, Color.LighterGrey);
    public async Task SetWarning(string msg) => await Respond(msg, Color.Gold);
    public async Task SetError(string msg) => await Respond(msg, Color.Red);
    public async Task SetCritical(string msg) => await Respond(msg, Color.DarkRed);

    public async Task<bool> EnsurePermissions(CmdPerms reqperms)
    {
        DiscordBot harper = GetHarper();
        if (harper.HasPermissions(Context.User.Id, reqperms))
            return true;

        await SetCritical(":x: You don't have sufficient permissions to execute this command!");
        return false;
    }
}
