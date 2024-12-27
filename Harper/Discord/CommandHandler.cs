using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Harper;

namespace MinecraftServerApplication.Discord;
public abstract class CommandHandler : InteractionModuleBase
{
    // sends a message as a response to the user who executed the command edits the original message
    private async Task CreateResponse(string msg, Color colour)
    {
        IUserMessage originalMessage = await GetOriginalResponseAsync();
        Embed newEmbed = new EmbedBuilder()
            .WithDescription(msg)
            .WithColor(colour)
            .Build();

        if (originalMessage != null)
            await DeleteOriginalResponseAsync();

        await Context.Channel.SendMessageAsync(embed: newEmbed);


        //modify the original message
        //await originalMessage.ModifyAsync(properties =>
        //{
        //    properties.Embed = newEmbed;
        //});
    }

    public async Task Respond(string msg, Color colour) => await ErrorHandler.CatchError(async () => await CreateResponse(msg, colour));
    public async Task SetSuccess(string msg) => await Respond(msg, Color.Green);
    public async Task SetInfo(string msg) => await Respond(msg, Color.LighterGrey);
    public async Task SetWarning(string msg) => await Respond(msg, Color.Gold);
    public async Task SetError(string msg) => await Respond(msg, Color.Red);
    public async Task SetCritical(string msg) => await Respond(msg, Color.DarkRed);
}
