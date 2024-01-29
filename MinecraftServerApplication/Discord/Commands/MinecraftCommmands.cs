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
    private readonly MCServerModule? _mcServer;

    public MinecraftCommmands() {
        _mcServer = Program.GetModuleOfType<MCServerModule>();
    }

    public async Task<MinecraftServer?> GetServer(string serverName, State matchEither) {
        //check whether the server module is available
        if (_mcServer == null) {
            await SetCritical("Could not find the module in charge for running Minecraft Servers!");
            return null;
        }

        //find the server & check whether the it was found
        MinecraftServer? server = _mcServer.TryGetServer(serverName);
        if (server == null) {
            await SetError($"couldn't find a server with the name `{serverName}`");
            return null;
        }

        //check the server's state
        if ((server.State & matchEither) == 0) {
            await SetError($"`{serverName}` has an illegal state: `{server.State.ToString()}`!");
            return null;
        }

        return server;
    }

    #region commands
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
                $"> state: {server.State}\n";
            // if the server is running
            if (server.State is State.RUNNING) {
                Process serverProcess = server.ServerProcess;
                response +=
                    $"> threads: {serverProcess.Threads.Count}\n" +
                    $"> memory used: {FormatBytes(serverProcess.WorkingSet64)}\n" +
                    $"> responding: {serverProcess.Responding}\n" +
                    $"> running: `{(DateTime.Now - serverProcess.StartTime).ToString(@"hh\:mm")}` (hh:mm)\n";
            }
        }

        response = response[..^1]; //exclude the last character
        await SetInfo(response);
    }

    //TODO: make shit less repetitive
    [SlashCommand("start", "starts the minecraft server")]
    public async Task StartCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(CanStartServerAutocomplete))] string serverName) {
        MinecraftServer? server = await GetServer(serverName, State.CAN_START);

        if (server == null) {
            return;
        }

        //check whether the state is ERROR before starting (becomes ERROR if there were issues when starting it last or )
        if (server.State is State.ERROR) {
            await SetWarning($"an error occured when last starting `{serverName}`! Starting anyway...");
        }
        else {
            await SetInfo($"starting `{serverName}`...");
        }

        //run the actual server
        await server.Run();

        //if the server's state is now ERROR
        if (server.State is State.ERROR) {
            await SetError($"an error occured when starting `{serverName}`!");
            return;
        }

        await SetSuccess($"started `{serverName}`!");
    }

    [SlashCommand("stop", "stops the minecraft server")]
    public async Task StopCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(CanStopServerAutocomplete))] string serverName) {
        MinecraftServer? server = await GetServer(serverName, State.CAN_STOP);

        if (server == null) {
            return;
        }

        await SetInfo($"shutting down `{serverName}`...");
        await server.Stop();
        await SetSuccess($"`{serverName}` was shut down!");
    }

    [SlashCommand("restart", "restarts the minecraft server")]
    public async Task RestartCmd([Summary("server-name", "specifies the server to target"), Autocomplete(typeof(CanStopServerAutocomplete))] string serverName) {
        MinecraftServer? server = await GetServer(serverName, State.CAN_STOP);

        if (server == null) {
            return;
        }

        await SetInfo($"shutting down {serverName}...");
        await server.Stop();
        await SetSuccess($"`{serverName}` was shut down! restarting...");
        await server.Run();
    }
    #endregion //commands

    #region autocompleters
    #region base
    public abstract class ServerNameAutocomplete : AutocompleteHandler {
        private static MCServerModule? _mcServer; //stores the minecraft server instance

        //contains the state that the autocompleter needs to match (uses OR comparison)
        protected abstract State MatchState {
            get;
        }

        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services) {
            _mcServer ??= Program.GetModuleOfType<MCServerModule>(); //get the minecraft server if it hasn't been gathered yet

            //contains the suggestions for the servers to be started
            List<AutocompleteResult> suggestions = new();

            //loop through the initialized server names
            foreach (string name in _mcServer.ServerNames) {
                //extract the minecraft server's state
                State serverState = _mcServer.TryGetServer(name)?.State ?? throw new NullReferenceException();

                //check whether the server's state matches with the flags provided
                if ((serverState & MatchState) != 0) {
                    //add the result to the auto complete result (use name for both the underlying value and display value)
                    suggestions.Add(new AutocompleteResult(name, name));
                }
            }

            //rate limit of 25 on the api
            return Task<AutocompletionResult>.FromResult(AutocompletionResult.FromSuccess(suggestions.Take(25)));
        }
    }
    #endregion //base
    public class CanStopServerAutocomplete : ServerNameAutocomplete {
        protected override State MatchState => State.CAN_STOP;
    }

    public class CanStartServerAutocomplete : ServerNameAutocomplete {
        protected override State MatchState => State.CAN_START;
    }
    #endregion //autocompleters
}
