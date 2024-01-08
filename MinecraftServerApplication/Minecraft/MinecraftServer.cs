using System.Diagnostics;
using System.IO.Compression;

namespace MinecraftServerApplication.Minecraft;
internal class MinecraftServer {
    private State _state;
    private int _faultyShutdownCount;
    private readonly int _maxRestartAttempts;
    private readonly int _maxBackups;
    private readonly bool _automaticStartup;
    private readonly Process _serverProcess;
    private readonly string _backupDirectory;
    private readonly string _worldDirectory;

    #region constructor
    public MinecraftServer(MinecraftServerSettings settings) {
        #region local functions
        static string GetJvmArguments(MinecraftServerSettings settings) {
            return
                $" -Xms{(int)MathF.Round(settings.minGB * 1024f)}M" +
                $" -Xmx{(int)MathF.Round(settings.maxGB * 1024f)}M" +
                $" -jar {Path.GetFileName(settings.jarPath)}" +
#if !DEBUG //only add the nogui argument if it's not a debug build
                $" -nogui" +
#endif //!DEBUG
                $" {settings.additionalJvmArgs ?? string.Empty}";
        }

        static string GetWorldDirectory(string serverDirectory) {
            StreamReader reader = new(new FileStream(Path.Combine(serverDirectory, "server.properties"), FileMode.Open, FileAccess.Read));
            string? line;

            string? worldFolder = null;
            do {
                line = reader.ReadLine();

                if (line == null) {
                    throw new NullReferenceException($"couldn't find the world folder in server.properties from '{serverDirectory}'");
                }

                if (line.StartsWith("level-name")) {
                    //get everything after '='
                    int startIndex = line.IndexOf('=') + 1;
                    worldFolder = line[startIndex..];
                }
            }
            while (worldFolder == null);

            return Path.Combine(serverDirectory, worldFolder);
        }

        if (Path.GetExtension(settings.jarPath) != ".jar" || File.Exists(settings.jarPath) == false) {
            throw new Exception($"no .jar detected at path: '{settings.jarPath}'");
        }
        #endregion //local functions

        string? serverDirectory = Path.GetDirectoryName(settings.jarPath);
        if (serverDirectory == null) {
            const string ERROR_STRING= "the file at '{0}' doesn't exist!";
            string error = string.Format(ERROR_STRING, settings.jarPath);
            Debug.Fail(error);
            throw new NullReferenceException(string.Format(error));
        }

        ProcessStartInfo startInfo = new() {
            FileName = "java",                      //run with java
            Arguments = GetJvmArguments(settings),  //the jvm arguments
            WorkingDirectory = serverDirectory,     //working directory = folder containing jar
            UseShellExecute = false,                //makes the process start locally
            RedirectStandardInput = true,           //for preventing input to be written to the application
            RedirectStandardOutput = true,          //for preventing output to be written to the console
            RedirectStandardError = true,           //for preventing error output to be written to the console
        };

        _maxRestartAttempts = settings.maxRestartAttempts; //add one to make inclusive
        _maxBackups = settings.maxBackups;
        _automaticStartup = settings.automaticStartup;
        _faultyShutdownCount = 0;
        _state = State.STOPPED;
        _worldDirectory = GetWorldDirectory(serverDirectory);
        _backupDirectory = Path.Combine(serverDirectory, "backups");
        _serverProcess = new() {
            StartInfo = startInfo,
        };

        Directory.CreateDirectory(_backupDirectory);
    }
    #endregion //constructor

    public State State {
        get => _state;
    }

    public bool AutomaticStartup {
        get => _automaticStartup;
    }

    public Process ServerProcess {
        get => _serverProcess;
    }

    #region startup & shutdown
    public Task Run() {
        if (_state is State.RUNNING or State.STARTING) {
            throw new Exception($"the server is already running");
        }

        async void Run() {
            do {
                Start();

                if (_state is State.ERROR) {
                    break;
                }

                await _serverProcess.WaitForExitAsync();
                await Task.Delay(30);

                await Task.Run(CreateBackup);

                //if running is false, it means that the shutdown was intended; no need for restarting.
                if (_state is State.STOPPED) {
                    _faultyShutdownCount = 0;
                    break;
                }

                _faultyShutdownCount++;
                _state = State.ERROR;
            }
            while (_faultyShutdownCount <= _maxRestartAttempts || _maxRestartAttempts < 0); //do lower than 0 check to allow for restart attempts to be disabled

            //if an error occured;
            //this means that either the server was automatically restarted too many times
            //or an error occured whilst running
            if (_state is State.ERROR) {
                Debug.WriteLine($"the server either automatically restarted too many times or an error occured! {_maxRestartAttempts}");
            }
        }

        Thread runServerThread = new(Run);
        runServerThread.Start();
        return Task.CompletedTask;
    }

    public void Start() {
        //if the state is starting or running; ignore
        if (_state is State.RUNNING or State.STARTING) {
            Debug.WriteLine("start was called whilst the server wasn't stopped, ignoring call.");
            return;
        }

        _state = State.STARTING;
        bool success =  _serverProcess.Start();
        _state = success ? State.RUNNING : State.ERROR;
    }

    public async Task Stop() {
        //if the state is not starting or running; ignore
        if (_state is not State.STARTING or State.RUNNING) {
            Debug.WriteLine("start was called whilst the server wasn't starting, ignoring call.");
            return;
        }

        _state = State.STOPPING;
        SendCommand("kick @a Server has shut down");
        SendCommand("stop"); //send the 'stop' command to the server
        await _serverProcess.WaitForExitAsync();
        _state = State.STOPPED;
    }
    #endregion //startup & shutdown

    public void SendCommand(string command) {
        _serverProcess.StandardInput.WriteLine(command);
    }

    private void CreateBackup() {
        #region sorter
        static int SortPaths(string a, string b) {
            //isolate the file name; assuming all files have the same directory
            string[] aSplit = Path.GetFileNameWithoutExtension(a).Split('_'); //split the file name to before and after `_`
            string[] bSplit = Path.GetFileNameWithoutExtension(b).Split('_');

            {
                int defaultCompare = aSplit[0].CompareTo(bSplit[0]);
                if (defaultCompare != 0) {
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

        if (_maxBackups == 0) {
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

        ZipFile.CreateFromDirectory(_worldDirectory, filePath);

        //delete old backups if there are too many backups
        List<string> backups = Directory.GetFiles(_backupDirectory, "*.zip").ToList();
        backups.Sort(SortPaths);
        while (_maxBackups > 0 && backups.Count > _maxBackups) { //greater than 0 check
            File.Delete(backups[0]);
            backups.RemoveAt(0);
        }
    }
}
