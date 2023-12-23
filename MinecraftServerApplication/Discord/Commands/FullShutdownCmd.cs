using Discord;
using Discord.WebSocket;
using MinecraftServerApplication.Minecraft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerApplication.Discord.Commands;
internal class FullShutdownCmd : Command {
    private MCServerModule _mcServer;

    public FullShutdownCmd() : base(new SlashCommandBuilder()
    .WithName("full-shutdown")
    .WithDescription("fully shuts down the server WARN: SHALL REQUIRE A MANUAL RESTART")
    ) {
        _mcServer = Program.GetModuleOfType<MCServerModule>();
    }

    public override async Task Run(CommandHandler command) {
        await command.SetInfo("Server is shutting down, **The server must be restarted manually to go online again!**");
        Harper.keepAlive = true;
        await Program.Shutdown();
    }
}
