using Foundation;
using UIKit;

namespace Electron2D.Examples;

internal static class Program
{
    private static void Main(string[] args) => UIApplication.Main(args, null, typeof(GameDelegate));
}

[Register("CharacterMovementDelegate")]
public sealed class GameDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? options)
    {
        application.BeginInvokeOnMainThread(async () =>
        {
            try { await CharacterMovementGame.RunAsync(); }
            catch (Exception error) { Console.Error.WriteLine(error); }
        });
        return true;
    }
}
