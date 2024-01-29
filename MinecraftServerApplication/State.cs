namespace MinecraftServerApplication;
[Flags]
internal enum State : byte {
    ERROR = 1,
    STOPPED = 2,
    RUNNING = 4,
    STARTING = 8,
    STOPPING = 16,
}
