﻿using Discord;
using Discord.WebSocket;
using MinecraftServerApplication.Minecraft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerApplication.Discord.Commands;
internal class FullRestartCmd : Command {
    public FullRestartCmd() {
        CommandBuilder = new SlashCommandBuilder()
            .WithName("full-restart")
            .WithDescription("fully restarts the server WARN: HARPER AND SERVERS WILL BE OFFLINE FOR A WHILE");
    }

    public override async Task Run(CommandHandler command) {
        await command.SetInfo("Server is restarting, it will be back online shortly!");
        Program.Shutdown();
    }
}
