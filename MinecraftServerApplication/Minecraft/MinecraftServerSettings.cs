namespace MinecraftServerApplication.Minecraft;
internal struct MinecraftServerSettings {
    //TODO: add initial online state (whether to start or stop the server on startup)
    public string name;
    public float minGB;
    public float maxGB;
    public string jarPath;
    public int maxRestartAttempts;
    public int maxBackups;
    public string additionalJvmArgs;
}
