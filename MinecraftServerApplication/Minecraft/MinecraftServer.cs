using log4net;
using MinecraftServerApplication.Logging;
using MinecraftServerApplication.Minecraft.Settings;
using System.Diagnostics;
using System.IO.Compression;
using System.Reactive;
using System.Reflection;

namespace MinecraftServerApplication.Minecraft;
internal class MinecraftServer
{
    private State _state;
    private int _faultyShutdownCount;
    private bool _readingError;
    private object _serverProcessLock;
    private readonly ILog _log;
    private readonly int _maxRestartAttempts;
    private readonly int _maxBackups;
    private readonly bool _automaticStartup;
    private readonly Process _serverProcess;
    private readonly string _backupDirectory;
    private readonly string _worldDirectory;

    #region constructor
    public MinecraftServer(MinecraftServerSettings settings)
    {
        #region local functions
        static string GetJvmArguments(MinecraftServerSettings settings)
        {
            return
                $" -Xms{(int)MathF.Round(settings.minGB * 1024f)}M" +
                $" -Xmx{(int)MathF.Round(settings.maxGB * 1024f)}M" +
                $" -jar {Path.GetFileName(settings.jarPath)}" +
#if !DEBUG //only add the nogui argument if it's not a debug build
                $" nogui" +
#endif //!DEBUG
                $" {settings.additionalJvmArgs ?? string.Empty}";
        }

        static string GetWorldDirectory(string serverDirectory)
        {
            string propertiesPath = Path.Combine(serverDirectory, "server.properties");

            // if server.properties doesn't exist, just assume the default value
            if (File.Exists(propertiesPath) == false)
            {
                return Path.Combine(serverDirectory, "world"); ;
            }

            StreamReader reader = new(new FileStream(propertiesPath, FileMode.Open, FileAccess.Read));
            string? line;

            string? worldFolder = null;
            do
            {
                line = reader.ReadLine();

                if (line == null)
                {
                    // if no world folder is set in the server.properties; assume default
                    worldFolder = "world";
                    continue;
                }

                if (line.StartsWith("level-name"))
                {
                    //get everything after '='
                    int startIndex = line.IndexOf('=') + 1;
                    worldFolder = line[startIndex..];
                }
            }
            while (worldFolder == null);

            return Path.Combine(serverDirectory, worldFolder);
        }

        string pathExtension = Path.GetExtension(settings.jarPath);
        if ((pathExtension == ".jar" || pathExtension == ".sh") == false || File.Exists(settings.jarPath) == false)
        {
            throw new Exception(string.Format("no .jar or .sh detected at path: '{0}'", settings.jarPath));
        }
        #endregion //local functions

        //pre-process initialization, init
        _log = LogManager.GetLogger("McServer" + '.' + settings.name);
        _maxRestartAttempts = settings.maxRestartAttempts;
        _maxBackups = settings.maxBackups;
        _automaticStartup = settings.automaticStartup;
        _faultyShutdownCount = 0;
        _state = State.STOPPED;
        _readingError = false;
        _serverProcessLock = new object();

        //init directory
        string? serverDirectory = Path.GetDirectoryName(settings.jarPath);
        if (serverDirectory == null)
        {
            const string ERROR_STRING = "the file at '{0}' doesn't exist!";
            string error = string.Format(ERROR_STRING, settings.jarPath);
            //log error
            _log.Error(error);
            throw new NullReferenceException(string.Format(error));
        }

        //process startinfo init
        ProcessStartInfo startInfo;

        if (pathExtension == ".jar")
        {
            startInfo = new()
            {
                FileName = "java",                      //run with java
                Arguments = GetJvmArguments(settings),  //the jvm arguments
                WorkingDirectory = serverDirectory,     //working directory = folder containing jar
                UseShellExecute = false,                //makes the process start locally
                RedirectStandardInput = true,           //for preventing input to be written to the application
                RedirectStandardOutput = true,          //for preventing output to be written to the console
                RedirectStandardError = true,           //for preventing error output to be written to the console
            };
        }
        else //assumed extension is .sh now, since there is no way it's something else, also is unix-only, why tf would you want to run this on windows?
        {
            startInfo = new()
            {
                FileName = "/bin/bash",                 //run with bash
                Arguments = settings.jarPath,           //run the script
                WorkingDirectory = serverDirectory,     //working directory = folder containing script
                UseShellExecute = false,                //makes the process start locally
                RedirectStandardInput = true,           //for preventing input to be written to the application
                RedirectStandardOutput = true,          //for preventing output to be written to the console
                RedirectStandardError = true,           //for preventing error output to be written to the console
            };
        }

        //post-process initialization, init
        _worldDirectory = GetWorldDirectory(serverDirectory);

        //set the backup directory
        if (((string.IsNullOrWhiteSpace(settings.backupDir) == false) && Directory.Exists(settings.backupDir)))
        {
            _backupDirectory = settings.backupDir;
        }
        else
        {
            _backupDirectory = Path.Combine(serverDirectory, "backups");
        }

        _serverProcess = new()
        {
            StartInfo = startInfo,
        };
        _serverProcess.ErrorDataReceived += (sender, e) => _log.Error(e.Data ?? "null");

        //create the backup directory in the server directory
        Directory.CreateDirectory(_backupDirectory);
    }
    #endregion //constructor

    //finalizer
    ~MinecraftServer()
    {
        if (_serverProcess != null)
        {
            _serverProcess.Dispose();
        }
    }

    public State State => _state;
    public bool AutomaticStartup => _automaticStartup;
    public Process ServerProcess => _serverProcess;

    #region startup & shutdown
    public Task Run()
    {
        if (_state is State.RUNNING or State.STARTING)
        {
            _log.Error("the server is already running");
            throw new Exception("the server is already running");
        }

        //local function
        async void ExecuteServer()
        {
            do
            {
                Start();

                if (_state is State.ERROR)
                {
                    break;
                }

                await _serverProcess.WaitForExitAsync();
                await Task.Delay(30);


                if (_state is not State.KILLED && _serverProcess.ExitCode == 143) //check whether the server has received the SIGTERM signal (to catch from an outside source)
                {
                    _log.Error("the server was killed from an outside source");
                    _state = State.KILLED;
                }
                else if (_state is not State.ERROR && _serverProcess.ExitCode is not 0 or 143) //if the exit code isn't success
                {
                    _log.Error("the server incountered an error");
                    _state = State.ERROR;
                }
                else if ((_state & (State.TRANSITION | State.STARTING)) != 0)
                {
                    _state = State.STOPPED;
                }

                //don't create a backup if the process was killed; something definately went wrong this time
                if (_state is not State.KILLED)
                {
                    lock (_serverProcessLock)
                    {
                        CreateBackup();
                    }
                }

                //the shutdown was intended (includes KILLED, but if something went wrong you'd prefer not having theserver automatically restart)
                if (_state is not State.ERROR)
                {
                    _faultyShutdownCount = 0;
                    break;
                }

                _faultyShutdownCount++;
                _state = State.ERROR;
                if (_faultyShutdownCount <= _maxRestartAttempts)
                {
                    _log.Warn($"an unexpected shutdown occured! restarting ({_faultyShutdownCount}/{_maxRestartAttempts})");
                }
                else
                {
                    _log.Warn($"max amount of restart attempts reached! ({_maxRestartAttempts})");
                }
            }
            while (_faultyShutdownCount <= _maxRestartAttempts || _maxRestartAttempts < 0); //do lower than 0 check to allow for restart attempts to be disabled

            //if an error occured;
            //this means that either the server was automatically restarted too many times
            //or an error occured whilst running
            if (_state is State.ERROR && ((_faultyShutdownCount <= _maxRestartAttempts) == false))
            {
                _log.Warn($"an error occured!");
            }
        }

        Thread runServerThread = new(ExecuteServer);
        runServerThread.Start();
        Task.Delay(100).Wait(); //wait for 100ms until returning the method to insure the states are correct
        return Task.CompletedTask;
    }

    public void Start()
    {
        //if the state doesn't conain a state that can be started; ignore
        if ((_state & State.CAN_START) == 0)
        {
            _log.Warn($"{nameof(Start)}() was called whilst the server state was '{_state}', ignoring call.");
            return;
        }

        lock (_serverProcessLock)
        {
            _log.Info($"starting server...");

            _state = State.STARTING;
            bool success = _serverProcess.Start();
            _state = success ? State.RUNNING : State.ERROR;

            if (success && _readingError == false)
            {
                _serverProcess.BeginErrorReadLine();
                _readingError = true;
            }
        }
    }

    public async Task Stop()
    {
        //if the state doesn't conain a state that can be stopped; ignore
        if ((_state & State.CAN_STOP) == 0)
        {
            _log.Warn($"{nameof(Stop)}() was called whilst the server state was '{_state}', ignoring call.");
            return;
        }

        _log.Info($"stopping server...");

        _state = State.STOPPING;
        SendCommand("kick @a Server has shut down");
        SendCommand("stop"); //send the 'stop' command to the server

        // wait till the server has fully shut down, if it takes longer than X seconds, kill the server
        Task timeout = Task.Delay(60 * 1000);
        Task exiting = _serverProcess.WaitForExitAsync();
        await Task.WhenAny(timeout, exiting);

        // check whether the timeout has completed and the exiting task hasn't.
        if (timeout.IsCompleted && (exiting.IsCompleted == false))
        {
            _serverProcess.Kill();
            await exiting;
            _state = State.KILLED;
        }
        else
        {
            // the exit task has succeeded.
            _state = State.STOPPED;
        }
    }

    public void Kill()
    {
        _log.Warn("Forcefully killing server!");
        ServerProcess.Kill(true); //kill the entire process tree
        _state = State.KILLED;
    }
    #endregion //startup & shutdown

    public void SendCommand(string command)
    {
        _serverProcess.StandardInput.WriteLine(command);
    }

    public void RunFunction(string functionName)
    {
        var mcServerMod = Program.GetModuleOfType<MCServerModule>() ?? throw new Exception("this can't happen, this won't exist without McServerModule");

        string[]? function = mcServerMod.TryGetFunction(functionName);

        if (function == null)
        {
            _log.Warn($"couldn't find a function with the name: '{functionName}', ignoring function call");
            return;
        }

        _log.Info($"executuing function: '{functionName}'");
        foreach (string cmd in function)
        {
            SendCommand(cmd);
        }
    }

    private void CreateBackup()
    {
        #region sorter
        static int SortPaths(string a, string b)
        {
            //isolate the file name; assuming all files have the same directory
            string[] aSplit = Path.GetFileNameWithoutExtension(a).Split('_'); //split the file name to before and after `_`
            string[] bSplit = Path.GetFileNameWithoutExtension(b).Split('_');

            {
                int defaultCompare = aSplit[0].CompareTo(bSplit[0]);
                if (defaultCompare != 0)
                {
                    return defaultCompare;
                }
            }

            int aInt = int.Parse(aSplit[1]);
            int bInt = int.Parse(bSplit[1]);
            return aInt < bInt ? -1 : 1; //the values shouldn't be able to be equal, otherwise that'd suggest an equal file name, which is impossible
        }
        #endregion //sorter
        const string EXTENSION = ".zip";
        string filePath;

        if (_maxBackups == 0)
        {
            return;
        }

        //get the file path
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd"); //format the date of the file name

            //get the backups
            string[] backupsToday = Directory.GetFiles(_backupDirectory, $"{date}*.zip"); //get all of the backups today
            Array.Sort(backupsToday, SortPaths);

            //get the index
            int backupIndex = backupsToday.Length == 0 ? 0 : //if there are 0 backups today, index = 0
                int.Parse(Path.GetFileNameWithoutExtension( //otherwise, get the index from the latest backup
                backupsToday[^1] //get the last backup of today
                ).Split('_')[1]) + 1; //add 1 to the index

            //get the file path
            string fileName = date + '_' + backupIndex + EXTENSION;
            filePath = Path.Combine(_backupDirectory, fileName);
        }

        //backup creation
        {
            DateTime start = DateTime.Now;
            _log.Info($"creating a backup ({Path.GetFileName(filePath)})...");
            ZipFile.CreateFromDirectory(_worldDirectory, filePath);
            _log.Info($"finished creating a backup! (took {Math.Round((DateTime.Now - start).TotalSeconds, 1)}s)");
        }

        //delete old backups if there are too many backups
        List<string> backups = Directory.GetFiles(_backupDirectory, "*.zip").ToList();
        backups.Sort(SortPaths);
        while (_maxBackups > 0 && backups.Count > _maxBackups)
        { //greater than 0 check
            _log.Warn($"too many backups! deleting backup: '{Path.GetFileName(backups[0])}'");
            File.Delete(backups[0]);
            backups.RemoveAt(0);
        }
    }
}
