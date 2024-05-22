using MinecraftServerApplication.Logging;
using MinecraftServerApplication.Minecraft.Settings;
using QUtilities;
using System.Reactive;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MinecraftServerApplication.Minecraft;
internal class MCServerModule : IModule
{
    private readonly Dictionary<string, MinecraftServer> _servers;
    private readonly Dictionary<string, string[]> _functions;

    #region constructor
    public MCServerModule()
    {
        _servers = [];
        _functions = [];
        //HACK: kinda dirty, but ey, it works!
        const string PATH = Program.SETTINGS_PATH + "/server_settings.jsonc";
        var serverSettings = JsonUtils.InitFile<ServerSettings>(PATH, true);
        File.WriteAllText(PATH, "//note: jarPath can be a .sh file, if this is the case arguments are not automatically built.\n" + File.ReadAllText(PATH));

        //init minecraft servers
        serverSettings.servers ??= [];
        foreach (MinecraftServerSettings server in serverSettings.servers)
        {
            if (_servers.ContainsKey(server.name))
            {
                this.LogError($"a server with the name {server.name} already exists! Ignoring...");
                continue;
            }
            try
            {
                _servers.Add(server.name, new MinecraftServer(server));
                this.LogInfo($"loaded minecraft server '{server.name}' from '{server.jarPath}'");
            }
            catch (Exception ex)
            {
                this.LogError($"failed to load minecraft server '{server.name}'");
                this.LogDebug(ex.ToString());
            }
        }

        //init minecraft functions
        serverSettings.functions ??= [];
        foreach (MinecraftFunctionSettings function in serverSettings.functions)
        {
            if (_functions.ContainsKey(function.name))
            {
                this.LogError($"a function with the name {function.name} already exists! Ignoring...");
                continue;
            }
            _functions.Add(function.name, function.commands);
        }
    }
    #endregion //constructor

    public string[] ServerNames => _servers.Keys.ToArray();
    public string[] FunctionNames => _functions.Keys.ToArray();

    #region startup / shutdown
    public async Task Run()
    {
        List<Task> runServers = [];
        foreach (string serverName in _servers.Keys)
        {
            //if the server needs to automatically start
            if (_servers[serverName].AutomaticStartup)
            {
                Task task = _servers[serverName].Run();
                runServers.Add(task);
            }
        }

        await Task.WhenAll(runServers);
    }

    public async Task Shutdown()
    {
        List<Task> shutdownServers = new();
        foreach (string key in _servers.Keys)
        {
            if (_servers[key].State is State.RUNNING or State.STARTING)
            {
                shutdownServers.Add(_servers[key].Stop());
            }
        }

        await Task.WhenAll(shutdownServers);
    }
    #endregion

    public MinecraftServer? TryGetServer(string name)
    {
        _servers.TryGetValue(name, out MinecraftServer? server);
        return server;
    }

    public string[]? TryGetFunction(string name)
    {
        _functions.TryGetValue(name, out string[]? function);
        return function;
    }
}
