int8 exitCode = 1;

do
{
    using Harper.Core application = new();
    application.Run();
    exitCode = application.ExitCode;
}
while (exitCode != 2);

return unchecked((uint8)(exitCode));
