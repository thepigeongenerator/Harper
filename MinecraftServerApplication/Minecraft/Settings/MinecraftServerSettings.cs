#pragma warning disable CS0649 //struct is initialised using JSON. I don't care about this warning
namespace MinecraftServerApplication.Minecraft.Settings;
internal struct MinecraftServerSettings
{
    public string name;
    public float minGB;
    public float maxGB;
    public string executablePath;
    public int maxRestartAttempts;
    public int maxBackups;
    public string backupDir;
    public bool automaticStartup;
    public string additionalJvmArgs;
}
