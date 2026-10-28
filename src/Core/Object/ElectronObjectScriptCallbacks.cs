namespace Electron2D;

/// <summary>Single-use typed object callbacks installed by a live Script binding.</summary>
/// <typeparam name="TObject">Compatible object receiver role.</typeparam>
/// <remarks>Install only during the owner's synchronous registered Bind. Each owner requires a fresh set.
/// Metadata callbacks are cold tooling/storage operations; prepared notifications and typed access need no discovery.</remarks>
public class ElectronObjectScriptCallbacks<TObject> where TObject : ElectronObject
{
    private int _issued;
    /// <summary>Creates an unused callback set.</summary>
    public ElectronObjectScriptCallbacks() { }
    /// <summary>Receives numeric notifications and their requested reverse-direction flag.</summary>
    public Action<TObject, int, bool>? Notification { get; init; }
    /// <summary>Appends script-owned typed descriptors to the ordinary CLR property list.</summary>
    /// <remarks>Uses the installed schema's collision, owner-validation and storage rules. Do not return null entries.</remarks>
    public Func<TObject, IEnumerable<PropertyDescriptor>>? PropertyDescriptors { get; init; }
    /// <summary>Customizes a property after native validation, or returns null to hide it.</summary>
    public Func<TObject, PropertyDescriptor, PropertyDescriptor?>? ValidateProperty { get; init; }
    /// <summary>Overrides the owner's revert-availability query; typed descriptors still own revert values and writes.</summary>
    public Func<TObject, PropertyDescriptor, bool>? CanRevertProperty { get; init; }
    /// <summary>Supplies the owner's string representation while this binding is installed.</summary>
    public Func<TObject, string>? StringRepresentation { get; init; }
    /// <summary>Releases callback state once after project Clear and removal of all installed slots.</summary>
    /// <remarks>Also runs after partial binding failure and committed disposal; cancelled pre-delete retains the set.
    /// A throwing callback cannot restore retired slots.</remarks>
    public Action<TObject>? Retired { get; init; }

    internal void Issue()
    {
        if (Interlocked.CompareExchange(ref _issued, 1, 0) != 0) throw new InvalidOperationException("A script callback set can be installed only once.");
    }
    internal virtual ObjectScriptBinding CreateBinding() => new();
    internal virtual void Populate(ObjectScriptBinding target, TObject owner)
    {
        target.Notification = Notification is { } notification ? (what, reversed) => notification(owner, what, reversed) : null;
        target.PropertyDescriptors = PropertyDescriptors is { } properties ? () => properties(owner) : null;
        target.ValidateProperty = ValidateProperty is { } validate ? property => validate(owner, property) : null;
        target.CanRevertProperty = CanRevertProperty is { } revert ? property => revert(owner, property) : null;
        target.StringRepresentation = StringRepresentation is { } stringify ? () => stringify(owner) : null;
        target.Retired = Retired is { } retired ? () => retired(owner) : null;
    }
}

internal class ObjectScriptBinding
{
    internal Action<int, bool>? Notification;
    internal Func<IEnumerable<PropertyDescriptor>>? PropertyDescriptors;
    internal Func<PropertyDescriptor, PropertyDescriptor?>? ValidateProperty;
    internal Func<PropertyDescriptor, bool>? CanRevertProperty;
    internal Func<string>? StringRepresentation;
    internal Action? Retired, SetupLocalToScene;
    internal Func<RID>? GetRID;
    internal Action? ResetState;
    internal Action<string>? SetPathCache;
}
