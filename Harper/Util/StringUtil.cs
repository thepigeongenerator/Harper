using System;

namespace Harper.Util;

public static class StringUtil
{
        public static string FormatBytes(uint64 bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int32 suffixIndex = 0;

            while (bytes >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                bytes /= 1024;
                suffixIndex++;
            }

            return $"{bytes} {suffixes[suffixIndex]}";
        }
}
