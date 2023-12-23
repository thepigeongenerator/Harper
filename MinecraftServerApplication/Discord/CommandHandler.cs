
using Discord.Rest;
using Discord;
using Discord.WebSocket;

namespace MinecraftServerApplication.Discord;
internal class CommandHandler {
    private readonly SocketSlashCommand _slashCommand;

    public CommandHandler(SocketSlashCommand command) {
        _slashCommand = command;
    }

    public SocketSlashCommand SlashCommand {
        get => _slashCommand;
    }

    public SocketSlashCommandDataOption? TryGetOption(string optionName) {
        return (from option in SlashCommand.Data.Options
                where option.Name == optionName
                select option).FirstOrDefault();
    }

    public async Task Initialize() => await SlashCommand.RespondAsync("Harper is thinking...", ephemeral: false);
    public async Task SetSuccess(string message) => await Respond(message, Color.Green);
    public async Task SetInfo(string message) => await Respond(message, Color.LighterGrey);
    public async Task SetWarning(string message) => await Respond(message, Color.Gold);
    public async Task SetError(string message) => await Respond(message, Color.Red);
    public async Task SetCritical(string message) => await Respond(message, Color.DarkRed);

    private async Task Respond(string message, Color color) {
        RestInteractionMessage originalMessage;
        Embed newEmbed;

        //unpack the original response
        originalMessage = SlashCommand.GetOriginalResponseAsync().Result;

        //build embed to add
        newEmbed = new EmbedBuilder()
            .WithDescription(message)
            .WithColor(color)
            .Build();

        //modify the original message
        await originalMessage.ModifyAsync(properties => {
            properties.Content = string.Empty;
            properties.Embed = newEmbed;
        });
    }
}
