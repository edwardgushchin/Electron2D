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
        try { ContractChecks.Run(); Console.WriteLine("ELECTRON2D_RESULT PASS"); }
        catch (Exception error) { Console.WriteLine("ELECTRON2D_RESULT FAIL " + error); code = 1; }
        Environment.Exit(code);
        return true;
    }
}
