using System;

namespace MinecraftServerApplication.Util;

// exitcode is a signed 8 bit integer, because the available range of exit codes is 0-127.
// this puts negative exit codes to be reserved for operating system signals
[Flags]
public enum ExitCode : int8
{
    SUCCESS = 0,
    FAILURE = 1,
    RESTART = 2,
}
