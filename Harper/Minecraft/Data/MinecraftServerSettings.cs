using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Harper.Util;

namespace Harper.Minecraft.Data;

public readonly struct MinecraftServerSettings
{
    public readonly string name;
    public readonly float minGB;
    public readonly float maxGB;
    public readonly string executablePath;
    public readonly int32 maxRestartAttempts;
    public readonly int32 maxBackups;
    public readonly string backupDir;
    public readonly bool automaticStartup;
    public readonly string additionalJvmArgs;

    // makes sure that all settings are correct
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("the server name cannot be null or whitespace!");
        if (minGB > maxGB) throw new ArgumentOutOfRangeException($"{nameof(minGB)} is not allowed to be less than {nameof(maxGB)}!");
        if (minGB >= 0.5F) throw new ArgumentOutOfRangeException($"{nameof(minGB)} must be more than or equal to 0.5");
        if (!File.Exists(executablePath)) throw new FileNotFoundException($"the given executable path: '{executablePath}' is invalid!");
        if (executablePath.EndsWith(".sh") | executablePath.EndsWith(".jar")) throw new FileNotFoundException($"{nameof(executablePath)} is not a .sh or .jar file!");
        if (maxRestartAttempts < -1) throw new ArgumentOutOfRangeException(nameof(maxRestartAttempts), $"value {maxRestartAttempts} is not allowed to be less than -1");
        if (maxBackups < -1) throw new ArgumentOutOfRangeException(nameof(maxBackups), $"value {maxBackups} is not allowed to be less than -1");
        if (!Directory.Exists(backupDir)) throw new FileNotFoundException($"{nameof(backupDir)} of value '{backupDir}' does not exist!");
        // automatic startup will default to 'false'
        // additionalJvmArgs will default to 'null', which is valid
    }

    // builds the java virtual machine arguments
    public string GetJvmArguments()
    {
        return
            $" -Xms{(int32)MathF.Round(minGB * 1024.0F)}M" +
            $" -Xmx{(int32)MathF.Round(maxGB * 1024.0F)}M" +
            $" -jar {Path.GetFileName(executablePath)}" +
#if !DEBUG  //only add the nogui argument if it's not a debug build
            $" nogui" +
#endif
            $" {additionalJvmArgs ?? string.Empty}";
    }

    // gets the world directory of this world from the server directory using server.properties
    public string GetWorldPath(string serverDirectory)
    {
        // get the server.properties file
        string propertiesPath = Path.Combine(serverDirectory, "server.properties");

        // if server.properties doesn't exist, just assume the default value; "world"
        if (File.Exists(propertiesPath) == false)
            return Path.Combine(serverDirectory, "world"); ;

        // otherwise, read the setting set in the file
        string ln = FileUtil.GetFirstLine(propertiesPath, ln => ln.StartsWith("level-name"));

        //get everything after '='
        int startIndex = ln.IndexOf('=') + 1;                       // get the index
        return Path.Combine(serverDirectory, ln[startIndex..]);     // get the array slice of line
    }
}
