using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Harper.Logging;
using Harper.Minecraft.Data;
using log4net;

namespace Harper.Minecraft;

public class MCServer : IDisposable
{
    private readonly ILog log = null;
    private readonly MCServerManager serverManager;
    public readonly MCServerSettings settings = default;
    public readonly Process serverProcess = null;
    public readonly string serverDir = null;
    public readonly string worldDir = null;
    public readonly string worldName = null;

    private ServerState state = ServerState.ERROR;
    private object serverProcessLock = null;
    private uint32 faultyShutdownCount = 0;
    private bool creatingBackup = false;
    private bool disposed = false;

    // boolean shothands
    public bool CanStart => (state & ServerState.CAN_START) != 0 && (creatingBackup == false);
    public bool CanStop => (state & ServerState.CAN_STOP) != 0;
    public bool CanKill => (state & ServerState.CAN_KILL) != 0;
    public bool Running => (state & ServerState.RUNNING) != 0;

    // constructor
    public MCServer(MCServerManager serverManager, MCServerSettings settings)
    {
        this.serverManager = serverManager;
        this.settings = settings;
        serverProcessLock = new object();

        // validate the server settings
        settings.Validate();

        // get & validate the server directory
        serverDir = Path.GetDirectoryName(settings.executablePath);
        if (string.IsNullOrEmpty(serverDir)) throw new FileNotFoundException($"can't parse the server directory from path '{settings.executablePath}'");

        // init other fields
        worldDir = settings.GetWorldDir(serverDir);
        log = this.GetLogger();

        // get the start info for the server process
        ProcessStartInfo startInfo = new()
        {
            FileName = Path.GetExtension(settings.executablePath) == ".jar"
                ? "/bin/java" : "/bin/bash",        // execute with java or bash, depending on the extension
            Arguments = settings.GetJvmArguments(), // the jvm arguments
            WorkingDirectory = serverDir,           // working directory = folder containing jar
            UseShellExecute = false,                // makes the process start locally
            RedirectStandardInput = true,           // redirect stdin, so we can send commands to the minecraft server
            RedirectStandardOutput = false,         // don't redirect the stdout to an internal buffer
            RedirectStandardError = false,          // don't redirect the stderr to an internal buffer
            CreateNoWindow = true,                  // don't start the process in a new window
        };

        // create the server process
        serverProcess = new Process() { StartInfo = startInfo };
        serverProcess.Exited += OnShutdown;
        state = ServerState.STOPPED;
    }

    // sends a command to the server (shorthand)
    public void SendCommand(string command)
    {
        serverProcess.StandardInput.WriteLine(command);
    }

    public async Task MakeBackup()
    {
        log.Info($"a backup has been requested for '{settings.name}', creating one now!");
        bool wasrunning = Running;

        if (Running)
        {
            SendCommand("kick @a a backup was requested, the server has automatically been shut down for this process. It will be up again in a bit.");
            await Stop();
            log.Warn($"server '{settings.name}' has been shut down due to a backup having been requested and it being unsafe to leave the server on during this process.");
        }

        lock (serverProcessLock)
            creatingBackup = true;

        DateTime start = DateTime.Now;
        await BackupManager.CreateBackup(this, serverManager);
        log.Info($"the backup has concluded! it took {Math.Round((DateTime.Now - start).TotalSeconds, 1)}s");

        lock (serverProcessLock)
            creatingBackup = false;

        if (wasrunning)
            Start();
    }

    // starts the server
    public void Start()
    {
        lock (serverProcessLock)
        {
            if (CanStart == false)
                Throw(log, new InvalidOperationException($"attempted to start server '{settings.name}' whilst it was in an invalid state!"));

            log.Info($"starting server...");
            bool success = serverProcess.Start();
            state = !success ? ServerState.ERROR : ServerState.RUNNING;
        }
    }

    // stops the server and awaits exit, if it takes too long the server is killed instead
    public async Task Stop()
    {
        if (CanStop == false)
            Throw(log, new InvalidOperationException($"attempted to stop server '{settings.name}' whilst it was in an invalid state!"));

        log.Info($"stopping server...");
        state = ServerState.STOPPING;

        // send shutdown commands
        SendCommand("kick @a Server has shut down");
        SendCommand("stop");

        // wait for the process to exit, or the timeout to be reached
        Task timeout = Task.Delay(MC_SERVER_SHUTDOWN_TIMEOUT_MS);
        Task exiting = serverProcess.WaitForExitAsync();
        await Task.WhenAny(timeout, exiting);

        // if the process still hasn't exited, kill the server instead.
        if (timeout.IsCompleted && exiting.IsCompleted == false)
        {
            log.Error("shutdown took too long! Killing the server instead.");
            await Kill();
        }
    }

    // forcefully kills the server, awaits exit
    public async Task Kill()
    {
        if (CanKill == false)
            Throw(log, new InvalidOperationException($"attempted to kill server '{settings.name}' whilst it was in an invalid state!"));

        log.Warn("Forcefully killing server!");
        serverProcess.Kill(true); // kill the entire process tree
        state = ServerState.KILLED;

        // wait for the process to exit, or for the timeout to be reached
        Task timeout = Task.Delay(MC_SERVER_SHUTDOWN_TIMEOUT_MS);
        Task exiting = serverProcess.WaitForExitAsync();
        await Task.WhenAny(timeout, exiting);

        // if the process still hasn't exited, throw an exception
        if (exiting.IsCompleted == false)
            Throw(log, new TimeoutException($"attempted to kill '{settings.name}', but took too long."));
    }

    // called when the process has exited
    private void OnShutdown(object sender, System.EventArgs e)
    {
        int8 exitcode = unchecked((int8)serverProcess.ExitCode); // truncate the exit code (we aren't on windows, so more isn't needed)
        log.Info($"the server '{settings.name}' has exited! With code {exitcode:X2}"); // show the exit code in 2 base-16 digits.

        // if the exit code is negative (ergo 127<), return; this exit code has been caused by the operating system.
        // in the case that it's 0; also just return as this indicates success.
        if (exitcode <= 0)
            return;

        // restart the server if we still can
        if (faultyShutdownCount >= settings.maxRestartAttempts)
        {
            faultyShutdownCount++;
            log.Warn($"abnormal exit code detected, restarting server '{settings.name}'... ({faultyShutdownCount}/{settings.maxRestartAttempts})");
            Start();
        }

        log.Error($"abnormal exit code detected, won't restart server '{settings.name}', as the maximum number of restart attempts have has reached. ({settings.maxRestartAttempts})");
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        GC.SuppressFinalize(this);

        // kill the process if it's still running
        // don't allow for a graceful exit, as we ain't got time for that.
        if (serverProcess.HasExited == false)
            Kill().Wait();

        // dispose the process itself.
        serverProcess.Dispose();
    }
}
