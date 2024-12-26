using System;
using System.IO;
using log4net;

namespace Harper.Minecraft.Data;

public struct MCServerManagerSettings
{
    public string backupDir;
    public MCServerSettings[] servers;

    // makes sure that all settings are correct
    public void Validate()
    {
        if (!Directory.Exists(backupDir)) throw new FileNotFoundException($"{nameof(backupDir)} of value '{backupDir}' does not exist!");
    }
}
