using Discord;
using Discord.WebSocket;

namespace MinecraftServerApplication.Discord;
internal abstract class Command {
    private HarperModule? _harper;
    private readonly SlashCommandBuilder _commandBuilder;

    public Command(SlashCommandBuilder commandBuilder) {
        _harper = null;
        _commandBuilder = commandBuilder;
    }

    public SlashCommandBuilder CommandBuilder {
        get => _commandBuilder;
    }

    public HarperModule Harper {
        get => _harper ?? throw new NullReferenceException("harper has not been defined yet");
        set => _harper ??= value;
    }

    public abstract Task Run(CommandHandler command);
}
