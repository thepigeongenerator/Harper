namespace MinecraftServerApplication;
internal enum State : sbyte {
    ERROR = -1,
    STOPPED = 0,
    RUNNING = 1,
    STARTING = 2,
    STOPPING = 3,
}
