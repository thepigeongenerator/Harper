namespace MinecraftServerApplication;
internal interface IModule {
    public abstract Task Run();
    public abstract Task Shutdown();
}
