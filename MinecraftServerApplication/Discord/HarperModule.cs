//TODO: add permission system so users can't run commands all willy-nilly
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using log4net;
using MinecraftServerApplication.Discord.Commands;
using MinecraftServerApplication.Logging;
using QUtilities;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace MinecraftServerApplication.Discord;
internal class HarperModule : IModule {
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly ulong[] _allowedUserIds;

    public HarperModule() {
        DiscordSocketConfig config = new() {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildIntegrations | GatewayIntents.GuildMessageReactions
        };

        _client = new DiscordSocketClient(config);
        _client.SlashCommandExecuted += CommandHandler;
        _client.Ready += ReadyHandler;
        _client.Log += (entry) => Task.Run(entry.Severity switch {
            LogSeverity.Info => () => this.LogInfo(entry.Message),
            LogSeverity.Warning => () => this.LogWarn(entry.Message),
            LogSeverity.Error => () => this.LogError(entry.Message),
            LogSeverity.Critical => () => this.LogFatal(entry.Message),
            LogSeverity.Debug => () => this.LogDebug(entry.Message),
            _ => () => this.LogInfo(entry.Message),
        });

        _interactionService = new(_client.Rest);
        _client.AutocompleteExecuted += async (SocketAutocompleteInteraction arg) => {
            var context = new InteractionContext(_client, arg, arg.Channel);
            await _interactionService.ExecuteCommandAsync(context, null);
        };

        _allowedUserIds = JsonUtils.InitFile<ulong[]>(Path.Combine(Program.SETTINGS_PATH, "harper_allowed_users.json")) ?? new ulong[0];
    }

    #region startup / shutdown
    public async Task Run() {
        {
            ValueTask<string> getToken = File.ReadLinesAsync(Path.Combine(Program.SETTINGS_PATH, "bot_token.txt")).FirstAsync();
            await getToken;

            await _client.LoginAsync(TokenType.Bot, getToken.Result);
            await _client.SetStatusAsync(UserStatus.Online);
        }

        await _client.StartAsync();

        await Program.WaitShutdownAsync();
    }

    public async Task Shutdown() {
        await _client.SetStatusAsync(UserStatus.Offline);
        await _client.LogoutAsync();
        await _client.StopAsync();
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
        if (_allowedUserIds.Contains(command.User.Id)) {
            this.LogInfo($"'{command.User.Username}' is executuing command '{command.CommandName}' in '{command.Channel.Name}'");
            await command.RespondAsync("harper is thinking...");
            var context = new InteractionContext(_client, command, command.Channel);
            await _interactionService.ExecuteCommandAsync(context, null);
        }
        else {
            this.LogWarn($"'the user {command.User.Username}' had insufficient permissions to execute command: '{command.CommandName}'");
            await command.RespondAsync(":x: You don't have sufficient permissions to exectute commands!");
        }
    }
    #endregion //event listeners
}
