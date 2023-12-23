﻿using MinecraftServerApplication.Discord;
using MinecraftServerApplication.Minecraft;
using System.Diagnostics;

namespace MinecraftServerApplication;

internal static class Program {
    public const string SETTINGS_PATH = "./settings";
    public const string DATA_PATH = "./data";
    public const string BACKUP_PATH = "./backups";
    public const string LOG_PATH = "./logs";
    private static readonly ManualResetEvent shutdownEvent = new(false);
    private static readonly List<IModule> _modules = [
        new MCServerModule(),
        new HarperModule(),
    ];

    public static void Main() {
        MainAsync().Wait();
    }

    public static async Task Shutdown() {
        shutdownEvent.Set();

        List<Task> shutdown = new();
        foreach (IModule module in _modules) {
            shutdown.Add(module.Shutdown());
        }

        await Task.WhenAll(shutdown);
    }

    public static Task WaitShutdownAsync() {
        return Task.Run(shutdownEvent.WaitOne);
    }

    public static T GetModuleOfType<T>() where T : class, IModule {
        return (
            from module in _modules
            where module is T
            select module as T).First();
    }

    private static async Task MainAsync() {
        List<Task> runModules = [];

        for (int i = 0; i < _modules.Count; i++) {
            Debug.WriteLine($"{MathF.Round((float)i / _modules.Count * 100)}% running '{_modules[i].GetType().Name}'...");
            Task task = _modules[i].Run();
            runModules.Add(task);
        }

        Debug.WriteLine("100% done!");

        await WaitShutdownAsync(); //await the shutdown event
        await Task.WhenAll(runModules); //await the modules from compleding
    }
}
