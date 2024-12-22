using System;

namespace Harper.Minecraft.Data;

public enum ServerState : uint8
{
    ERROR = 0,
    TRANSITION = 1, // 🏳‍⚧
    RUNNING = 2,
    STOPPED = 4,
    KILLED = 8,
    STARTING = RUNNING | TRANSITION,
    STOPPING = STOPPED | TRANSITION,
    CAN_START = ERROR | STOPPED | KILLED,
    CAN_STOP = RUNNING | STARTING,
    CAN_KILL = CAN_STOP | STOPPING,
    ANY = 0xFF,
}
