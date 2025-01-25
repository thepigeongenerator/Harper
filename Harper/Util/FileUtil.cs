using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Harper.Util;

public static class FileUtil
{
    // efficiently processes lines in a file
    // allows you to customise when you want to stop the data flow, what items to skip with skipIf
    // and when you've found the item you want with breakIf
    public static void ForEachLine(string path, Action<string> action) => ForEachLine(path, s => true, action);
    public static void ForEachLine(string path, Predicate<string> skipIf, Action<string> action) => ForEachLine(path, skipIf, s => false, action);
    public static void ForEachLine(string path, Predicate<string> skipIf, Predicate<string> breakIf, Action<string> action)
    {
        using FileStream fs = File.OpenRead(path);
        using StreamReader reader = new(fs);

        uint16 c = 0;       // for keeping track of the loop count
        string ln = null;   // stores the current line

        do
        {
            ln = reader.ReadLine(); // read a singular line

            // break if the line is null
            if (ln == null)
                break;

            // skip if the predicate returns 'true'
            if (skipIf.Invoke(ln))
                continue;

            // invoke the action
            action.Invoke(ln);

            // if the predicate is matched, break
            if (breakIf.Invoke(ln))
                break;
        } while (c++ < uint16.MaxValue);
    }

    // deserializes a list of objects, ignoring lines that start with '#'
    public static Dictionary<T1, T2> DeserializeDict<T1, T2>(string path, Func<string, (bool, T1)> parser1, Func<string, (bool, T2)> parser2)
    {
        // use a linked list to make allocation of new data faster
        LinkedList<(T1, T2)> data = new();

        // call ForEachLine, checking that the line isn't a comment
        ForEachLine(path, ln => (string.IsNullOrWhiteSpace(ln) || ln.StartsWith('#')), ln =>
        {
            string[] def = ln.Split(':', StringSplitOptions.TrimEntries);

            // use the given parser to get a result. Add to the data if successful
            (bool success, T1 res) a = parser1.Invoke(def[0]);
            (bool success, T2 res) b = parser2.Invoke(def[1]);
            if (a.success && b.success)
                data.AddFirst((a.res, b.res));
        });

        // convert the data to an array and return it.
        return data.ToDictionary<T1, T2>();
    }

    // throws an exception if the file doesn't exist
    public static void EnsureFileExists(string path)
    {
        if (File.Exists(path))
            return;

        throw new FileNotFoundException($"could not find a file at '{Path.GetFullPath(path)}'", Path.GetFileName(path));
    }

    // copies the template file to the path location, if the path location doesn't exist
    public static void CopyTemplateIfNotExists(string path, string templatePath)
    {
        EnsureFileExists(templatePath);
        if (File.Exists(path))
            return;

        {
            string dirPath = Path.GetDirectoryName(path);
            if (Directory.Exists(dirPath) == false)
                Directory.CreateDirectory(dirPath);
        }

        File.Copy(templatePath, path);
    }

    public static string GetFirstLine(string path, Predicate<string> comp)
    {
        string str = null;
        ForEachLine(path, s => false, comp, ln => str = ln);
        return str;
    }
}
