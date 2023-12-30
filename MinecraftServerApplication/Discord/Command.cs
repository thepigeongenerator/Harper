using Discord;

namespace MinecraftServerApplication.Discord;
internal abstract class Command {
    private HarperModule? _harper;
    private readonly SlashCommandBuilder? _commandBuilder;


    public Command() {
        _harper = null;
        _commandBuilder = null;
    }

    public SlashCommandBuilder CommandBuilder {
        get => _commandBuilder ?? throw new NullReferenceException("you need to initialize a command builder!");
        protected init => _commandBuilder = value;
    }

    public HarperModule Harper {
        get => _harper ?? throw new NullReferenceException("harper has not been defined yet");
        set => _harper ??= value;
    }

    public abstract Task Run(CommandHandler command);
}
