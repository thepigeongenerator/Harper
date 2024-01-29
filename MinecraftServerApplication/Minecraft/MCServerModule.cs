using QUtilities;

namespace MinecraftServerApplication.Minecraft;
internal class MCServerModule : IModule {
    private readonly Dictionary<string, MinecraftServer> _servers;

    public MCServerModule() {
        _servers = [];
        var serverSettings = JsonUtils.InitFile<MinecraftServerSettings[]>(Program.SETTINGS_PATH + "/server_settings.json", true);
        serverSettings ??= [];

        foreach (MinecraftServerSettings settings in serverSettings) {
            if (_servers.ContainsKey(settings.name)) {
                string error = $"a server with the name {settings.name} already exists!";
                throw new Exception(error);
            }
            _servers.Add(settings.name, new MinecraftServer(settings));
        }
    }

    public string[] ServerNames {
        get => _servers.Keys.ToArray();
    }

    #region startup / shutdown
    public async Task Run() {
        List<Task> runServers = [];
        foreach (string serverName in _servers.Keys) {
            //if the server needs to automatically start
            if (_servers[serverName].AutomaticStartup) {
                Task task = _servers[serverName].Run();
                runServers.Add(task);
            }
        }

        await Task.WhenAll(runServers);
    }

    public async Task Shutdown() {
        List<Task> shutdownServers = new();
        foreach (string key in _servers.Keys) {
            if (_servers[key].State is State.RUNNING or State.STARTING) {
                shutdownServers.Add(_servers[key].Stop());
            }
        }

        await Task.WhenAll(shutdownServers);
    }
    #endregion

    public MinecraftServer? TryGetServer(string name) {
        if (_servers.ContainsKey(name) == false) {
            return null;
        }

        return _servers[name];
    }
}
