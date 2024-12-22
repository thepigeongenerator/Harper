using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Harper.Util;

public static class FileUtil
{
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
            if (ln == null || breakIf.Invoke(ln))
                break;

            // skip if the predicate returns 'false'
            if (skipIf.Invoke(ln) == false)
                continue;

            // invoke the action
            action.Invoke(ln);
        } while (c++ < uint16.MaxValue);
    }

    // deserializes a list of objects, ignoring lines that start with '#'
    public static T[] DeserializeList<T>(string path, Func<string, (bool, T)> parser)
    {
        // use a linked list to make allocation of new data faster
        LinkedList<T> data = new();

        // call ForEachLine, checking that the line isn't a comment
        ForEachLine(path, ln => (ln[0] != '#'), ln =>
        {
            // use the given parser to get a result. Add to the data if successful
            (bool success, T res) = parser.Invoke(ln);
            if (success)
                data.AddFirst(res);
        });

        // convert the data to an array and return it.
        return data.ToArray<T>();
    }

    // throws an exception if the file doesn't exist
    public static void EnsureFileExists(string path)
    {
        if (File.Exists(path))
            return;

        throw new FileNotFoundException($"could not find a file at '{path}'", Path.GetFileName(path));
    }

    // copies the template file to the path location, if the path location doesn't exist
    public static void CopyTemplateIfNotExists(string path, string templatePath)
    {
        EnsureFileExists(templatePath);
        if (File.Exists(path))
            return;

        File.Copy(templatePath, path);
    }

    public static string GetFirstLine(string path, Predicate<string> comp)
    {
        string str = null;
        ForEachLine(path, s => false, comp, ln => str = ln);
        return str;
    }
}
