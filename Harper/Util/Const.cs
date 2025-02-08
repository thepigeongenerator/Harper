using System;

namespace Harper.Util;

public static class Const
{
    public static class FilePath
    {
        // set the setting and template path based on whether we're debugging or not
#if DEBUG
        public const string SETTING = "./settings/";
        public const string TEMPLATE = "./templates/";
#elif _LINUX
    public const string SETTING = "/etc/harper/";
    public const string TEMPLATE = "/usr/share/harper/templates/";
#else
#error platform is unsupported
#endif

        // set other paths
        public const string SETTING_HARPER_COMMAND_PERMS = SETTING + "command_perms";
        public const string TEMPLATE_HARPER_COMMAND_PERMS = TEMPLATE + "command_perms";
        public const string SETTING_MCSERVER_SETTINGS = SETTING + "mcserver_settings.jsonc";
        public const string TEMPLATE_MCSERVER_SETTINGS = TEMPLATE + "mcserver_settings.jsonc";
    }

    public const string ENV_HARPER_BOT_TOKEN = "HARPER_BOT_TOKEN";
    public const string ENV_HARPER_DEBUG = "HARPER_DEBUG";
    public const int32 MC_SERVER_SHUTDOWN_TIMEOUT_MS = 1000 * 60;
}
