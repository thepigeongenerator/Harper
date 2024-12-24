using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Harper.Minecraft;

public static class BackupManager
{
    private static readonly ILog log;

    static BackupManager()
    {
        log = LogManager.GetLogger(typeof(BackupManager));
    }

    // for ensuring that the path we're trying to process is a valid one
    private static bool IsValidPath(string path)
    {
        Regex mat = new(@"^\d{4}-\d{2}-\d{2}_\d+_.*\.tar\.gz$");
        return mat.Match(path).Success;
    }

    // orders the backup paths, mainly due to the index otherwise sorting incorrectly if we hit double digits.
    private static int32 CompBackupPaths(string a, string b)
    {
        // discarding the directory location; assumed all files have the same directory
        // get all components seperated by underscores
        string[] splitA = Path.GetFileNameWithoutExtension(a).Split('_');
        string[] splitB = Path.GetFileNameWithoutExtension(b).Split('_');

        // if the date portion (yyyy-mm-dd) does not match use the default comparer
        if (splitA[0] != splitB[0])
            return splitA[0].CompareTo(splitB[0]);

        // get the file index
        int32 aIndx = int32.Parse(splitA[1]);
        int32 bIndx = int32.Parse(splitB[1]);
        return aIndx < bIndx ? -1 : 1;        // it should be impossible for the values to be equal, so this assumption is carried here. Otherwise one will appear
    }

    // get the file path (for the .tar file)
    private static string GetFilePath(string backupDir, string name, bool indexIsZero)
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd"); //format the date of the file name

        // get a list of all the backups made today
        string[] backupsToday = Directory.GetFiles(backupDir, $"{date}_*_{name}.tar.gz");       // use a glob pattern to get anything that matches today's and the world name
        backupsToday = Array.FindAll(backupsToday, IsValidPath);                                // filter this array using a regex, just to make sure we didn't catch any exceptions
        Array.Sort(backupsToday, CompBackupPaths);                                              // sort the final result using the custom sorter

        // get the index that this backup should be
        int32 index = backupsToday.Length == 0 || indexIsZero
            ? 0                                                                                 // if there are 0 backups today or the override is set; index = 0
            : int.Parse(Path.GetFileNameWithoutExtension(backupsToday[^1]).Split('_')[1]) + 1;  // otherwise, get the index from the latest backup, and add 1

        // return the final file path
        return Path.Combine(backupDir, $"{date}_{index}_{name}.tar.gz");
    }

    public static async Task CreateBackup(MCServer server, MCServerManager serverManager)
    {
        string backupDir = Path.Combine(serverManager.backupDir, server.settings.name);             // combine the backup directory and name
        bool mkBackupDir = Directory.Exists(backupDir) == false;                                    // whether the backup directory needs to be made
        string pathgz = GetFilePath(serverManager.backupDir, server.settings.name, mkBackupDir);    // the .tar.gz file path
        string pathtar = pathgz[^3..];                                                              // use an index from end slice to exclude ".gz" and not re-allocate the string

        // if the backup directory doesn't exist, create it
        if (mkBackupDir == false)
            Directory.CreateDirectory(backupDir);

        // IDisposables
        using FileStream fsin = new(pathtar, FileMode.CreateNew, FileAccess.ReadWrite);     // the filestream for the .tar file containing all the data
        using FileStream fsout = new(pathgz, FileMode.CreateNew, FileAccess.Write);         // the filestream for the .tar.gz file, containing the compressed data
        using GZipStream gzip = new(fsout, CompressionLevel.SmallestSize);                  // gzip compressor

        // compression
        log.Info($"({server.settings.name}) creating '{pathtar}' from '{server.worldDir}'");
        await TarFile.CreateFromDirectoryAsync(server.worldDir, fsin, true);    // create the tar file

        log.Info($"({server.settings.name}) compressing '{pathtar}' to '{pathgz}'");
        await fsin.CopyToAsync(gzip);                                           // compress it using gzip

        log.Info($"({server.settings.name}) deleting '{pathtar}'");
        File.Delete(pathtar);                                                   // delete the created tar file
    }
}
