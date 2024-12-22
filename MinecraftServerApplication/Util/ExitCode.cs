using System;

namespace MinecraftServerApplication.Util;

[Flags]
public enum ExitCode : int8
{
    SUCCESS = 0,
    FAILURE = 1,
    RESTART = 2,
}
