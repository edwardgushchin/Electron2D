using Android.App;
using Android.Runtime;

namespace Electron2D.Examples;

[Application]
public sealed class GameApplication(nint handle, JniHandleOwnership ownership) : Application(handle, ownership)
{
    public override async void OnCreate()
    {
        base.OnCreate();
        try { await CharacterMovementGame.RunAsync(); }
        catch (Exception error) { Android.Util.Log.Error("CharacterMovement", error.ToString()); }
    }
}
