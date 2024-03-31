namespace MinecraftServerApplication;
[Flags]
internal enum State : byte
{
    ERROR = 1,
    STOPPED = 2,
    RUNNING = 4,
    STARTING = 8,
    STOPPING = 16,
    CAN_STOP = RUNNING | STARTING,
    CAN_START = ERROR | STOPPED,
    ANY = 0xFF, //0b11111111
    NONE = 0, //0b00000000
}
