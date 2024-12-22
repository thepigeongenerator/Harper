using System;
using System.Diagnostics;
using System.IO;
using Harper.Minecraft.Data;

namespace Harper.Minecraft;

public class MinecraftServer
{
    public readonly MinecraftServerSettings settings = default;
    public readonly Process serverProcess = null;
    private readonly string serverDir = null;
    private object serverProcessLock = null;
    private uint32 faultyShutdownCount = 0;

    // constructor
    public MinecraftServer(MinecraftServerSettings settings)
    {
        this.settings = settings;
        serverProcessLock = new object();

        // validate the server settings
        settings.Validate();

        // get & validate the server directory
        serverDir = Path.GetDirectoryName(settings.executablePath);
        if (string.IsNullOrEmpty(serverDir)) throw new FileNotFoundException($"can't parse the server directory from path '{settings.executablePath}'");

        ProcessStartInfo startInfo = new()
        {
            FileName = Path.GetExtension(settings.executablePath) == ".jar"
                ? "/bin/java" : "/bin/bash",        // execute with java or bash, depending on the extension
            Arguments = settings.GetJvmArguments(), // the jvm arguments
            WorkingDirectory = serverDir,           // working directory = folder containing jar
            UseShellExecute = false,                // makes the process start locally
            RedirectStandardInput = true,           // prevent input being written to the application
            RedirectStandardOutput = false,         // don't redirect the stdout to an internal buffer
            RedirectStandardError = false,          // don't redirect the stderr to an internal buffer
            CreateNoWindow = true,                  // don't start the process in a new window
        };
    }
}
