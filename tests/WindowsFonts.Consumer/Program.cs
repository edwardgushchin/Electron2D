var mode = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_WINDOWS_FONTS");
if (mode is null) { mode = args[0]; args = args[1..]; }
WindowsSystemFontChecks.Run(mode, args);
