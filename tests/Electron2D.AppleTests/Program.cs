using Foundation;
using UIKit;

namespace Electron2DAppleTests;

internal static class Program
{
    private static void Main(string[] args) => UIApplication.Main(args, null, typeof(TestDelegate));
}

[Register("Electron2DTestDelegate")]
internal sealed class TestDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? options)
    {
        var code = 0;
        var status = "PASS";
        try { ContractChecks.Run(); }
        catch (Exception error) { status = "FAIL " + error; code = 1; }
        Console.WriteLine("ELECTRON2D_RESULT " + status);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_RESULT_PATH") is { } path &&
            Environment.GetEnvironmentVariable("ELECTRON2D_RUN_TOKEN") is { } token)
            System.IO.File.WriteAllText(path, "RESULT " + token + " " + status + "\n");
        Environment.Exit(code);
        return true;
    }
}
