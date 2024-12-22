using System;
using log4net;

namespace Harper.Util;

public static class ExceptionHelper
{
    public static void Throw(ILog log, Exception exception)
    {
        log.Fatal(exception.Message);
        throw exception;
    }
}
