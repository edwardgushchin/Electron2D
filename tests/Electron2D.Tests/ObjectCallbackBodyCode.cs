using System.Runtime.CompilerServices;
using Electron2D;

internal sealed class ObjectCallbackBodyCode : ObjectCallbackCode
{
    internal new static readonly string SourcePath = Source();
    internal static void BindBody(RigidBody owner)
    {
        var state = Install(owner);
        owner.SetScriptCallbacks(new RigidBodyScriptCallbacks<RigidBody>
        {
            Notification = Notify,
            PropertyDescriptors = Discover,
            ValidateProperty = Validate,
            CanRevertProperty = CanRevert,
            StringRepresentation = _ => "body-code",
            Retired = _ => Retire(state),
            PhysicsProcess = (body, _) => body.LinearVelocity = new(state.Value, 0)
        });
    }
    internal static void ClearBody(RigidBody owner) => Clear(owner);
    private static string Source([CallerFilePath] string path = "") => path;
}
