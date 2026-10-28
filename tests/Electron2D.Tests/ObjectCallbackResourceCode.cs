using System.Runtime.CompilerServices;
using Electron2D;

internal sealed class ObjectCallbackResourceCode : ObjectCallbackCode
{
    internal new static readonly string SourcePath = Source();
    internal static bool FailSetup;
    internal static void BindResource(Resource owner)
    {
        var state = Install(owner);
        owner.SetScriptCallbacks(new ResourceScriptCallbacks<Resource>
        {
            Notification = Notify,
            PropertyDescriptors = Discover,
            ValidateProperty = Validate,
            CanRevertProperty = CanRevert,
            StringRepresentation = _ => "resource-code",
            Retired = _ => Retire(state),
            SetupLocalToScene = resource => { state.Setups++; state.SetupValue = state.Value; state.Scene = resource.GetLocalScene(); if (FailSetup) throw Error; }
        });
    }
    internal static void ClearResource(Resource owner) => Clear(owner);
    private static string Source([CallerFilePath] string path = "") => path;
}
