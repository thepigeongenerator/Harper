using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MinecraftServerApplication.Discord;
internal class HarperModule : IModule {
    public bool keepAlive;
    //TODO: add info command
    private List<Command> _commands;
    private readonly DiscordSocketClient _client;

    public HarperModule() {
        DiscordSocketConfig config = new() {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildIntegrations | GatewayIntents.GuildMessageReactions
        };

        keepAlive = false;
        _commands = new List<Command>();
        _client = new DiscordSocketClient(config);
        _client.SlashCommandExecuted += CommandHandler;
        _client.Ready += ReadyHandler;
#if DEBUG
        _client.Log += (entry) => Task.Run(() => Debug.WriteLine(entry.ToString()));
#endif
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
        #region equality checker
        static bool AreCommandsEqual(SocketApplicationCommand command, SlashCommandBuilder builder) {
            static bool AreOptionsEqual(IReadOnlyCollection<SocketApplicationCommandOption> optionsA, List<SlashCommandOptionBuilder> optionsB) {
                if (optionsA.Count == 0 && optionsB == null) {
                    return true;
                }
                if (optionsA.Count != optionsB.Count) {
                    return false;
                }

                bool result = true;
                for (int i = 0; i < optionsA.Count; i++) {
                    if (result == false) {
                        break;
                    }

                    var optionA = optionsA.ElementAt(i);
                    var optionB = optionsB.ElementAt(i);

                    //warning: this code is incomplete as I can't be bothered to check for every fucking option
                    result =
                        optionA.Name == optionB.Name &&
                        optionA.Description == optionB.Description &&
                        optionA.Type == optionB.Type &&
                        optionA.IsDefault == optionB.IsDefault &&
                        optionA.IsRequired == optionB.IsRequired &&
                        optionA.Choices.Count == optionB.Choices.Count;
                }

                return result;
            }

            return command.Name == builder.Name &&
            command.Description == builder.Description &&
            command.IsNsfw == builder.IsNsfw &&
            AreOptionsEqual(command.Options, builder.Options);
        }
        #endregion //equality checker

        IReadOnlyCollection<SocketApplicationCommand> currentCommands;

        #region get commands
        {
            //get the commands that the bot already has
            var getApplicationCommands = _client.GetGlobalApplicationCommandsAsync();
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()) {
                if (!type.IsAbstract && type.IsClass && type.IsAssignableTo(typeof(Command))) {
                    //init command
                    Command command = Activator.CreateInstance(type) as Command ?? throw new NullReferenceException("wasn't able to create an instance of the command");
                    command.Harper = this;

                    //add the command
                    _commands.Add(command);
                }
            }

            //wait till the process is done
            await getApplicationCommands;
            currentCommands = getApplicationCommands.Result;
        }
        #endregion //get commands

        List<Command> buildCommands = _commands.ToList();
        List<Task> updateCommands = new();

        #region update commands
        await Task.Run(() => {
            //update the commands with a match
            foreach (var command in currentCommands) {
                //try to find a match for the command
                int matchIndex = buildCommands.FindIndex((cmd) => (command.Name == cmd.CommandBuilder.Name));

                //delete the command if no match was found
                if (matchIndex == -1) {
                    Debug.WriteLine($"deleting '{command.Name}' command...");
                    updateCommands.Add(command.DeleteAsync());
                    continue;
                }

                //check whether the old command isn't equal to the new one
                if (AreCommandsEqual(command, buildCommands[matchIndex].CommandBuilder) == false) {
                    Debug.WriteLine($"overwriting '{buildCommands[matchIndex].CommandBuilder.Name}' command...");
                    //override the command
                    updateCommands.Add(_client.CreateGlobalApplicationCommandAsync(buildCommands[matchIndex].CommandBuilder.Build()));
                    buildCommands.RemoveAt(matchIndex);
                    continue;
                }

                //no updating needed: remove the command from the build que
                buildCommands.RemoveAt(matchIndex);
            }

            //add the commands that didn't have a match
            while (buildCommands.Count > 0) {
                Debug.WriteLine($"adding '{buildCommands[0].CommandBuilder.Name}' command...");
                updateCommands.Add(_client.CreateGlobalApplicationCommandAsync(buildCommands[0].CommandBuilder.Build()));
                buildCommands.RemoveAt(0);
            }
        });
        #endregion //update commands

        await Task.WhenAll(updateCommands);
    }

    private async Task CommandHandler(SocketSlashCommand command) {
        Command? commandRunner = _commands.FirstOrDefault((cmd) => (command.CommandName == cmd.CommandBuilder.Name));

        if (commandRunner == null) {
            Debug.WriteLine($"Couldn't find a command with the name: '{command.CommandName}'!");
            return;
        }
        CommandHandler commandHandler = new CommandHandler(command);
        await commandHandler.Initialize();
        await commandRunner.Run(commandHandler);
    }
    #endregion //event listeners
}
