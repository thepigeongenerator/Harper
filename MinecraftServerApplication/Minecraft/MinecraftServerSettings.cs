using System.ComponentModel.DataAnnotations;

namespace MinecraftServerApplication.Minecraft;
internal struct MinecraftServerSettings {
    public string name;
    public float minGB;
    public float maxGB;
    public string jarPath;
    public int maxRestartAttempts;
    public int maxBackups;
    public string additionalJvmArgs;
}
