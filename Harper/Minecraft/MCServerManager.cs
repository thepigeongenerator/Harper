using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Harper.Minecraft.Data;
using Harper.Util;
using log4net;
using Newtonsoft.Json;

namespace Harper.Minecraft;

public class MCServerManager : IDisposable
{
    private readonly ILog log = null;
    private readonly Dictionary<string, MCServer> servers = null;
    private readonly string backupDir = null;

    public MCServerManager()
    {
        log = LogManager.GetLogger(typeof(MCServerManager));

        // init or get the file
        FileUtil.CopyTemplateIfNotExists(FilePath.SETTING_MCSERVER_SETTINGS, FilePath.TEMPLATE_MCSERVER_SETTINGS);
        var settings = JsonConvert.DeserializeObject<MCServerManagerSettings>(File.ReadAllText(FilePath.SETTING_MCSERVER_SETTINGS));

        // define the dictionary, with the capacity set to the servers length
        servers = new(settings.servers.Length);

        // initialize each server, and add it to the dictionary
        foreach (MCServerSettings server in settings.servers)
        {
            if (servers.ContainsKey(server.name))
                Throw(log, new ConfigurationErrorsException($"a server with the name '{server.name}' has already been defined!"));

            // creates a new instance of the minecraft server, and adds it to the dictionary
            servers.Add(server.name, new MCServer(server));
        }
    }

    public void Dispose()
    {
        // clean up the server list (dispose of all servers, and remove their references from the dict)
        foreach (string name in servers.Keys)
        {
            servers[name].Dispose();
            servers.Remove(name);
        }
    }
}
