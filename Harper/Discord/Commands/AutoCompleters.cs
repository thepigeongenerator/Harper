using Discord.Interactions;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harper.Minecraft;
using Harper.Minecraft.Data;
using Harper;

namespace MinecraftServerApplication.Discord.Commands;
internal static class AutoCompleters
{
    private static MCServerManager serverManager;

    public class AllServers : ServerAutocompleter
    {
        protected override Predicate<MCServer> Predicate => server => true;
    }

    public class CanStopServers : ServerAutocompleter
    {
        protected override Predicate<MCServer> Predicate => server => server.CanStop;
    }

    public class CanKillServers : ServerAutocompleter
    {
        protected override Predicate<MCServer> Predicate => server => server.CanKill;
    }

    public class CanStartServers : ServerAutocompleter
    {
        protected override Predicate<MCServer> Predicate => server => server.CanStart;
    }

    // base class for autocompleters because for some reason it's class-based with no parameters, and I hate it.
    public abstract class ServerAutocompleter : AutocompleteHandler
    {
        // the predicate that needs to be matched for the server to be included by the suggestion results
        protected abstract Predicate<MCServer> Predicate { get; }

        // generates the suggestions
        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            List<AutocompleteResult> suggestions = new();
            serverManager ??= Core.GetModuleOfType<MCServerManager>();

            //loop through the initialized server names
            foreach (string name in serverManager.ServerNames)
            {
                // check the predicate
                if (Predicate.Invoke(serverManager.GetServer(name)))
                {
                    //add the result to the auto complete result (use name for both the underlying value and display value)
                    suggestions.Add(new AutocompleteResult(name, name));
                }
            }

            //rate limit of 25 on the api
            return Task<AutocompletionResult>.FromResult(AutocompletionResult.FromSuccess(suggestions.Take(25)));
        }
    }
}
