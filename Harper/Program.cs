int8 exitCode = 1;

{
    using Harper.Core application = new();
    application.Run();
    exitCode = application.ExitCode;
}

return unchecked((uint8)(exitCode));
