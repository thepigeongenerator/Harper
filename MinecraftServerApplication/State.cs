namespace MinecraftServerApplication;
[Flags]
internal enum State : byte
{
    ERROR = 1,
    TRANSITION = 2,
    STOPPED = 4,
    RUNNING = 8,
    KILLED = 16,
    STOPPING = STOPPED | TRANSITION,
    STARTING = RUNNING | TRANSITION,
    CAN_STOP = RUNNING | STARTING,
    CAN_START = ERROR | STOPPED | KILLED,
    CAN_KILL = CAN_STOP | STOPPING,
    ANY = 0xFF, //0b11111111
    NONE = 0, //0b00000000
}
