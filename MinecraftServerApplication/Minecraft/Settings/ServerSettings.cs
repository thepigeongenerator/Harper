#pragma warning disable CS0649 //struct is initialised using JSON. I don't care about this warning
namespace MinecraftServerApplication.Minecraft.Settings;
internal struct ServerSettings
{
    public MinecraftServerSettings[] servers;
    public MinecraftFunctionSettings[] functions;
}
