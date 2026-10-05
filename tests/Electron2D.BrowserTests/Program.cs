try
{
    Console.WriteLine("Browser SDL initialization");
    SDL3.SDL.SetMainReady();
    ContractChecks.Run();
    NativeFontPrecisionTests.Run(FontTestFixtures.OpenSans, FontTestFixtures.Arabic);
    NativeTextBreakTests.Run();
    ImageCodecTests.Run();
    await BrowserNativeChecks.RunAsync();
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}
