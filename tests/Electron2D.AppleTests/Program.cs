using Foundation;
using UIKit;

namespace Electron2DAppleTests;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_RESULT_PATH") is { } path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            System.IO.File.WriteAllText(path + ".started", "Program.Main");
        }
        UIApplication.Main(args, null, typeof(TestDelegate));
    }
}

[Register("Electron2DTestDelegate")]
public sealed class TestDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? options)
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_RESULT_PATH") is { } path)
            System.IO.File.AppendAllText(path + ".started", "\nFinishedLaunching");
        application.BeginInvokeOnMainThread(Run);
        return true;
    }

    private static void Run()
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
    }
}
