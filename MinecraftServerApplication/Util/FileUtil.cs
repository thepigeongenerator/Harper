using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MinecraftServerApplication.Util;

public static class FileUtil
{
    // deserializes a list of objects, ignoring lines that start with '#'
    public static T[] DeserializeList<T>(string path, Func<string, (bool, T)> parser)
    {
        LinkedList<T> data = new();

        {
            using FileStream fs = File.OpenRead(path);
            using StreamReader reader = new(fs);

            uint16 c = 0;       // for keeping track of the loop count
            string ln = null;   // stores the current line

            do
            {
                c++;                    // increase the count size
                ln = reader.ReadLine(); // read a singular line

                // break if the line is null
                if (ln == null)
                    break;

                // skip if the line is a comment
                if (ln[0] == '#')
                    continue;

                // use the given parser to get a result. Add to the data if successful
                (bool success, T res) = parser.Invoke(ln);
                if (success)
                    data.AddFirst(res);
            } while (c < uint16.MaxValue);
        }

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
}
