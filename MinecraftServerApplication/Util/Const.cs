using System;

namespace MinecraftServerApplication.Util;

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
    public const string TEMPLATE = "/usr/share/harper/settings/";
#else
#error platform is unsupported
#endif

        // set other paths
        public const string SETTING_HARPER_ALLOWED_USERS = SETTING + "allowed_users";
        public const string TEMPLATE_HARPER_ALLOWED_USERS = TEMPLATE + "allowed_users";
    }

    public const string ENV_HARPER_BOT_TOKEN = "HARPER_BOT_TOKEN";
}
