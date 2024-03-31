#nullable disable
namespace MinecraftServerApplication.Minecraft.Settings;
internal struct MinecraftServerSettings {
    public string name;
    public float minGB;
    public float maxGB;
    public string jarPath;
    public int maxRestartAttempts;
    public int maxBackups;
    public bool automaticStartup;
    public string additionalJvmArgs;
}
