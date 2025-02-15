using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Harper.Util;
using log4net;

namespace Harper.Minecraft.Data;

public struct MCServerSettings
{
    public string name;
    public float minGB;
    public float maxGB;
    public string executablePath;
    public int32 maxRestartAttempts; // I hate how this is a signed integer due to JSON
    //public int32 maxBackups;
    public bool automaticStartup;
    public string additionalJvmArgs;

    // makes sure that all settings are correct
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("the server name cannot be null or whitespace!");
        if (minGB < 0.5F) throw new ArgumentOutOfRangeException($"{nameof(minGB)} may not be less than 0.5");
        if (maxGB < minGB) throw new ArgumentOutOfRangeException($"{nameof(maxGB)} is not allowed to be less than {nameof(minGB)}!");
        if (!File.Exists(executablePath)) throw new FileNotFoundException($"the given executable path: '{executablePath}' is invalid!");
        if (!(executablePath.EndsWith(".sh") | executablePath.EndsWith(".jar"))) throw new FileNotFoundException($"{nameof(executablePath)} is not a .sh or .jar file!");
        if (maxRestartAttempts < 0) throw new ArgumentOutOfRangeException(nameof(maxRestartAttempts), $"value {maxRestartAttempts} is not allowed to be less than 0");
        //if (maxBackups < -1) throw new ArgumentOutOfRangeException(nameof(maxBackups), $"value {maxBackups} is not allowed to be less than -1");
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
    public string GetWorldDir(string serverDir)
    {
        // get the server.properties file
        string propertiesPath = Path.Combine(serverDir, "server.properties");

        // if server.properties doesn't exist, just assume the default value; "world"
        if (File.Exists(propertiesPath) == false)
            return Path.Combine(serverDir, "world"); ;

        // otherwise, read the setting set in the file
        string ln = FileUtil.GetFirstLine(propertiesPath, ln => ln.StartsWith("level-name"));

        // get everything after '=', if the line has been found
        if (ln != null)
        {
            int32 startIndex = ln.IndexOf('=') + 1;               // get the index
            return Path.Combine(serverDir, ln[startIndex..]);   // the array slice of the line, and combine it with the server directory, return the result
        }

        // otherwise assume the default level name of "world"
        return Path.Combine(serverDir, "world");
    }
}
