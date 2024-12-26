using System;
using System.IO;
using log4net;

namespace Harper.Minecraft.Data;

public struct MCServerManagerSettings
{
    public static readonly ILog log;
    public string backupDir;
    public MCServerSettings[] servers;

    static MCServerManagerSettings()
    {
        log = LogManager.GetLogger(typeof(MCServerManagerSettings));
    }

    // makes sure that all settings are correct
    public void Validate()
    {
        if (!Directory.Exists(backupDir)) Throw(log, new FileNotFoundException($"{nameof(backupDir)} of value '{backupDir}' does not exist!"));
    }
}
