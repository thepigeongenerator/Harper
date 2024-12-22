using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MinecraftServerApplication.Util;

namespace MinecraftServerApplication.Discord;

public class DiscordBot : IDisposable
{
    private const GatewayIntents INTENTS = GatewayIntents.Guilds | GatewayIntents.GuildIntegrations | GatewayIntents.GuildMessageReactions;

    private readonly DiscordSocketClient client = null;
    private readonly InteractionService interactionService = null;
    private readonly uint64[] allowedIds = [];
    private bool disposed = false;

    public DiscordBot()
    {
        // initialize the discord client
        client = new(new() { GatewayIntents = INTENTS });
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

    private async Task Start()
    {
        string token = Environment.GetEnvironmentVariable(ENV_HARPER_BOT_TOKEN);
        if (token == null)
        {
            Console.Error.WriteLine($"please set a bot token in the '{ENV_HARPER_BOT_TOKEN}' environment variable");
            throw new ConfigurationErrorsException();
        }

        await client.LoginAsync(TokenType.Bot, token);
        await client.SetStatusAsync(UserStatus.Online);
        await client.StartAsync();
    }

    private async Task Stop()
    {
        await client.SetStatusAsync(UserStatus.Offline);
        await client.LogoutAsync();
        await client.StopAsync();
    }

    // is called when the bot is in it's "ready" state
    private async Task ReadyHandler()
    {
        // transferred code from last version
        // await interactionService.AddModuleAsync<UtilCommands>(null);
        // await interactionService.AddModuleAsync<ServerCommands>(null);
        // await interactionService.AddModuleAsync<MinecraftCommmands>(null);
        await interactionService.RegisterCommandsGloballyAsync(true);
    }

    // handles executed commands
    private async Task CommandHandler(SocketSlashCommand command)
    {
        if (allowedIds.Contains(command.User.Id))
        {
            Console.WriteLine($"'{command.User.Username}' is executuing command '{command.CommandName}' in '{command.Channel.Name}'");
            await command.RespondAsync("harper is thinking...");
            var context = new InteractionContext(client, command, command.Channel);
            await interactionService.ExecuteCommandAsync(context, null);
        }
        else
        {
            Console.Error.Write($"'the user {command.User.Username}' had insufficient permissions to execute command: '{command.CommandName}'");
            await command.RespondAsync(":x: You don't have sufficient permissions to exectute commands!");
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
        switch (msg.Severity)
        {
            case LogSeverity.Info:
                //this.LogInfo(msg.Message ?? "null");
                break;
            case LogSeverity.Warning:
                //this.LogWarn(msg.Message ?? "null");
                break;
            case LogSeverity.Error:
                //this.LogError(msg.Message ?? "null");
                break;
            case LogSeverity.Critical:
                //this.LogFatal(msg.Message ?? "null");
                break;
            case LogSeverity.Debug:
                //this.LogDebug(msg.Message ?? "null");
                break;
        }

        return Task.CompletedTask;
    }

    // for cleaning up resources
    public void Dispose()
    {
        if (disposed)
            return;

        GC.SuppressFinalize(this);
        Stop().Wait();
    }
}
