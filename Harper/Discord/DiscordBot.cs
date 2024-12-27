using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Harper.Logging;
using Harper.Util;
using log4net;
using MinecraftServerApplication.Discord.Commands;

namespace Harper.Discord;

public class DiscordBot : IModule
{
    private const GatewayIntents INTENTS = GatewayIntents.Guilds | GatewayIntents.GuildIntegrations | GatewayIntents.GuildMessageReactions;

    private readonly ILog log = null;
    private readonly DiscordSocketClient client = null;
    private readonly InteractionService interactionService = null;
    private readonly uint64[] allowedIds = [];
    private bool running = false;
    private bool disposed = false;

    public DiscordBot()
    {
        // initialize the logger
        log = this.GetLogger();

        // initialize the discord client
        client = new(new() { GatewayIntents = INTENTS, LogLevel = LogSeverity.Debug });
        interactionService = new(client.Rest);

        // init configuration
        FileUtil.CopyTemplateIfNotExists(FilePath.SETTING_HARPER_ALLOWED_USERS, FilePath.TEMPLATE_HARPER_ALLOWED_USERS);
        allowedIds = FileUtil.DeserializeList<uint64>(FilePath.SETTING_HARPER_ALLOWED_USERS, str => (uint64.TryParse(str, out uint64 res), res));

        // subscribe to events
        client.Ready += ReadyHandler;
        client.SlashCommandExecuted += CommandHandler;
        client.AutocompleteExecuted += AutoCompleteHandler;
        client.Log += LogHandler;
    }

    private async Task<bool> ValidateBotToken(string token)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("Authorization", $"Bot {token}");
        try
        {
            HttpResponseMessage response = await client.GetAsync("https://discord.com/api/v10/users/@me");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // starts the discord bot
    public async Task Start()
    {
        running = true;

        string token = Environment.GetEnvironmentVariable(ENV_HARPER_BOT_TOKEN);
        if (token == null)
            throw new ConfigurationErrorsException($"please set a bot token in the '{ENV_HARPER_BOT_TOKEN}' environment variable");

        log.Info("validating bot token...");
        if (await ValidateBotToken(token) == false)
            throw new HttpRequestException($"the supplied bot token in '{ENV_HARPER_BOT_TOKEN}' is not valid! Ensure you set a correct bot token!");
        log.Info("bot token validation was successful!");

        await client.LoginAsync(TokenType.Bot, token);
        await client.SetStatusAsync(UserStatus.Online);
        await client.StartAsync();
    }

    // stops the discord bot
    public async Task Stop()
    {
        await client.SetStatusAsync(UserStatus.Offline);
        await client.LogoutAsync();
        await client.StopAsync();
        running = false;
    }

    // is called when the bot is in it's "ready" state
    private async Task ReadyHandler()
    {
        await interactionService.AddModuleAsync<UtilCommands>(null);
        await interactionService.AddModuleAsync<ServerCommands>(null);
        await interactionService.AddModuleAsync<MinecraftCommmands>(null);
        await interactionService.RegisterCommandsGloballyAsync(true);
    }

    // handles executed commands
    private async Task CommandHandler(SocketSlashCommand command)
    {
        if (allowedIds.Contains(command.User.Id))
        {
            log.Info($"'{command.User.Username}' is executuing command '{command.CommandName}' in '{command.Channel.Name}'");
            await command.RespondAsync("harper is thinking...");
            var context = new InteractionContext(client, command, command.Channel);
            await interactionService.ExecuteCommandAsync(context, null);
        }
        else
        {
            log.Warn($"'the user {command.User.Username}' had insufficient permissions to execute command: '{command.CommandName}'");
            await command.RespondAsync(":x: You don't have sufficient permissions to execute commands!");
        }
    }

    // for handling autocompletions
    private async Task AutoCompleteHandler(SocketAutocompleteInteraction interaction)
    {
        var context = new InteractionContext(client, interaction, interaction.Channel);
        await interactionService.ExecuteCommandAsync(context, null);
    }

    // for converting the discord logs into the current application runtime's logs
    private Task LogHandler(LogMessage msg)
    {
        string entry = msg.Message ?? null;
        Action act = msg.Severity switch
        {
            LogSeverity.Info => () => log.Info(entry),
            LogSeverity.Warning => () => log.Warn(entry),
            LogSeverity.Error => () => log.Error(entry),
            LogSeverity.Critical => () => log.Fatal(entry),
            LogSeverity.Debug => () => log.Debug(entry),
            _ => () => log.Debug(entry),
        };
        act.Invoke();

        return Task.CompletedTask;
    }

    // for cleaning up resources
    public void Dispose()
    {
        if (disposed)
            return;

        GC.SuppressFinalize(this);
        disposed = true;

        if (running)
            Stop().Wait();
    }
}
