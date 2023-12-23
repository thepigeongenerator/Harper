using Discord;
using Discord.WebSocket;
using MinecraftServerApplication.Minecraft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerApplication.Discord.Commands;
internal class FullRestartCmd : Command {
    private MCServerModule _mcServer;

    public FullRestartCmd() : base(new SlashCommandBuilder()
        .WithName("full-restart")
        .WithDescription("fully restarts the server WARN: HARPER AND SERVERS WILL BE OFFLINE FOR A WHILE")
        ) {
        _mcServer = Program.GetModuleOfType<MCServerModule>();
    }

    public override async Task Run(CommandHandler command) {
        await command.SetInfo("Server is restarting, it will be back online shortly!");
        await Program.Shutdown();
    }
}
