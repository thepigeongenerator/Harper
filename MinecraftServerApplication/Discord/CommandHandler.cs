using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

namespace MinecraftServerApplication.Discord;
internal abstract class CommandHandler : InteractionModuleBase {
    public async Task SetSuccess(string message) => await Respond(message, Color.Green);
    public async Task SetInfo(string message) => await Respond(message, Color.LighterGrey);
    public async Task SetWarning(string message) => await Respond(message, Color.Gold);
    public async Task SetError(string message) => await Respond(message, Color.Red);
    public async Task SetCritical(string message) => await Respond(message, Color.DarkRed);

    public async Task Respond(string message, Color color) {
        IUserMessage originalMessage;
        Embed newEmbed;


        //unpack the original response
        originalMessage = await GetOriginalResponseAsync();

        //build embed to add
        newEmbed = new EmbedBuilder()
            .WithDescription(message)
            .WithColor(color)
            .Build();


        if (originalMessage == null) {
            await RespondAsync(string.Empty, embed:newEmbed);
            return;
        }
        
        //modify the original message
        await originalMessage.ModifyAsync(properties => {
            properties.Content = string.Empty;
            properties.Embed = newEmbed;
        });
    }
}
