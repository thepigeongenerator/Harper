using Discord;
using Discord.Interactions;
using MinecraftServerApplication.Minecraft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerApplication.Discord.Commands;
internal class MinecraftCommmands : CommandHandler {
    private readonly MCServerModule _mcServer;

    public MinecraftCommmands() {
        _mcServer = Program.GetModuleOfType<MCServerModule>();
    }

    [SlashCommand("info", "gets the info of the Minecraft server")]
    public async Task InfoCmd() {
        static string FormatBytes(long bytes) {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;

            while (bytes >= 1024 && suffixIndex < suffixes.Length - 1) {
                bytes /= 1024;
                suffixIndex++;
            }

            return $"{bytes} {suffixes[suffixIndex]}";
        }

        string response = string.Empty;

        foreach (string name in _mcServer.ServerNames) {
            MinecraftServer server = _mcServer.TryGetServer(name) ?? throw new NullReferenceException($"couldn't find a server with the name {name}");
            response += $"### {name}:\n" +
                $"> running: {server.Running}\n";
            if (server.Running) {
                Process serverProcess = server.ServerProcess;
                response +=
                    $"> threads: {serverProcess.Threads.Count}\n" +
                    $"> memory used: {FormatBytes(serverProcess.WorkingSet64)}\n" +
                    $"> responding: {serverProcess.Responding}\n" +
                    $"> running: `{(DateTime.Now - serverProcess.StartTime).ToString(@"hh\:mm")}`\n";
            }
        }

        response = response[..^1]; //exclude the last character
        await SetInfo(response);
    }

    //TODO: make shit less repetitive
    [SlashCommand("start", "starts the minecraft server")]
    public async Task StartCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(ServerNameAutocomplete))] string serverName) {
        MinecraftServer? server = _mcServer.TryGetServer(serverName);
        if (server == null) {
            await SetError($"couldn't find a server with the name `{serverName}`");
            return;
        }

        if (server.Running == true) {
            await SetError($"{serverName} is already running!");
            return;
        }

        await SetInfo($"starting {serverName}...");
        await server.Run();
        await SetSuccess($"started {serverName}!");
    }

    [SlashCommand("stop", "stops the minecraft server")]
    public async Task StopCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(ServerNameAutocomplete))] string serverName) {
        MinecraftServer? server = _mcServer.TryGetServer(serverName);
        if (server == null) {
            await SetError($"{serverName} is already running!");
            return;
        }

        if (server.Running == false) {
            await SetError($"`{serverName}` is already shut down!");
            return;
        }

        await SetInfo($"shutting down `{serverName}`...");
        await server.Stop();
        await SetSuccess($"`{serverName}` was shut down!");
    }

    [SlashCommand("restart", "restarts the minecraft server")]
    public async Task RestartCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(ServerNameAutocomplete))] string serverName) {
        MinecraftServer? server = _mcServer.TryGetServer(serverName);
        if (server == null) {
            await SetError($"couldn't find a server with the name `{serverName}`");
            return;
        }

        if (server.Running == false) {
            await SetError($"{serverName} is already running!");
            return;
        }

        await SetInfo($"shutting down {serverName}...");
        await server.Stop();
        await SetSuccess($"{serverName} was shut down! restarting...");
        await server.Run();
    }


    public class ServerNameAutocomplete : AutocompleteHandler {
        private static MCServerModule? _mcServer;

        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services) {
            _mcServer ??= Program.GetModuleOfType<MCServerModule>();

            List<AutocompleteResult> suggestions = new();
            foreach (string name in _mcServer.ServerNames) {
                suggestions.Add(new AutocompleteResult(name, name));
            }

            //rate limit of 25 on the api
            return Task<AutocompletionResult>.FromResult(AutocompletionResult.FromSuccess(suggestions.Take(25)));
        }
    }
}
