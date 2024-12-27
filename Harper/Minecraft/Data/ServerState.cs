using System;

namespace Harper.Minecraft.Data;

public enum ServerState : uint8
{
    ERROR = 0,
    RUNNING = 1,
    STOPPED = 2,
    KILLED = 4,
    // 8: unused
    STARTING = RUNNING << 4,
    STOPPING = STOPPED << 4,
    // 64: unused
    // 128: unused
    CAN_START = ERROR | STOPPED | KILLED,
    CAN_STOP = RUNNING | STARTING,
    CAN_KILL = CAN_STOP | STOPPING,
    ANY = 0xFF,
}
