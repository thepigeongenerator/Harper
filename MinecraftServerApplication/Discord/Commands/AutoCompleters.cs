using Discord.Interactions;
using Discord;
using MinecraftServerApplication.Minecraft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerApplication.Discord.Commands;
internal static class AutoCompleters {
    private static MCServerModule? _mcServer; //stores the minecraft server instance

    #region
    public class PreprogrammedFunctions : AutocompleteHandler {
        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services) {
            _mcServer ??= Program.GetModuleOfType<MCServerModule>();

            //TODO: read the different function options from _mcServer
            throw new NotImplementedException();
        }
    }
    #endregion

    #region server autocompleters
    #region base
    public abstract class ServerNameAutocompleteHandler : AutocompleteHandler {
        //contains the state that the autocompleter needs to match (uses OR comparison)
        protected abstract State MatchState {
            get;
        }

        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services) {
            //contains the suggestions for the servers to be started
            List<AutocompleteResult> suggestions = new();

            _mcServer ??= Program.GetModuleOfType<MCServerModule>(); //get the minecraft server if it hasn't been found yet

            //if the minecraft server was found
            if (_mcServer != null) {
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
            }

            //rate limit of 25 on the api
            return Task<AutocompletionResult>.FromResult(AutocompletionResult.FromSuccess(suggestions.Take(25)));
        }
    }
    #endregion //base
    public class AllServers : ServerNameAutocompleteHandler {
        protected override State MatchState => State.ANY;
    }

    public class CanStopServers : ServerNameAutocompleteHandler {
        protected override State MatchState => State.CAN_STOP;
    }

    public class CanStartServers : ServerNameAutocompleteHandler {
        protected override State MatchState => State.CAN_START;
    }
    #endregion //server autocompleters
}
