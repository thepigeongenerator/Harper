using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MinecraftServerApplication.Discord.Commands;
using System.Diagnostics;
using System.Reflection;

namespace MinecraftServerApplication.Discord;
internal class HarperModule : IModule {
    public bool keepAlive;
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;

    public HarperModule() {
        DiscordSocketConfig config = new() {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildIntegrations | GatewayIntents.GuildMessageReactions
        };

        keepAlive = false;
        _client = new DiscordSocketClient(config);
        _client.SlashCommandExecuted += CommandHandler;
        _client.Ready += ReadyHandler;
#if DEBUG
        _client.Log += (entry) => Task.Run(() => Debug.WriteLine(entry.ToString()));
#endif

        _interactionService = new(_client.Rest);
        _client.AutocompleteExecuted += async (SocketAutocompleteInteraction arg) => {
            var context = new InteractionContext(_client, arg, arg.Channel);
            await _interactionService.ExecuteCommandAsync(context, null);
        };
    }

    #region startup / shutdown
    public async Task Run() {
        {
            ValueTask<string> getToken = File.ReadLinesAsync(Path.Combine(Program.SETTINGS_PATH, "bot_token.txt")).FirstAsync();
            await getToken;

            await _client.LoginAsync(TokenType.Bot, getToken.Result);
        }

        await _client.StartAsync();

        await Program.WaitShutdownAsync();

        if (keepAlive) {
            await Task.Delay(-1);
        }
    }

    public async Task Shutdown() {
        await _client.LogoutAsync();
    }
    #endregion //startup / shutdown

    #region event listeners
    private async Task ReadyHandler() {
        await _interactionService.AddModuleAsync<UtilCommands>(null);
        await _interactionService.AddModuleAsync<ServerCommands>(null);
        await _interactionService.AddModuleAsync<MinecraftCommmands>(null);
        await _interactionService.RegisterCommandsGloballyAsync(true);
    }

    private async Task CommandHandler(SocketSlashCommand command) {
        await command.RespondAsync("harper is thinking...");
        var context = new InteractionContext(_client, command, command.Channel);
        await _interactionService.ExecuteCommandAsync(context, null);
    }
    #endregion //event listeners
}
