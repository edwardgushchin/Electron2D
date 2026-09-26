namespace Electron2D;

/// <summary>Selects the enabled processing policy of a screen visibility target.</summary>
public enum ScreenEnableMode
{
    /// <summary>Enables inherited processing.</summary>
    Inherit = 0,
    /// <summary>Enables processing regardless of scene pause.</summary>
    Always = 1,
    /// <summary>Enables processing only while the scene is paused.</summary>
    WhenPaused = 2
}

/// <summary>Enables a borrowed node while this rectangle participates in render visibility.</summary>
/// <remarks>Entry and path changes cache the target weakly. Off-screen targets use ProcessMode.Disabled.
/// Clearing/changing the path or exiting does not restore the old target's processing policy.</remarks>
public class VisibleOnScreenEnabler : VisibleOnScreenNotifier
{
    private ScreenEnableMode _enableMode;
    private string _enableNodePath = "..";
    private WeakReference<Node>? _target;
    private static readonly PropertyDescriptor[] EnableProperties =
    [
        new PropertyDescriptor<VisibleOnScreenEnabler, ScreenEnableMode>(nameof(EnableMode), node => node.EnableMode, (node, value) => node.EnableMode = value,
            _ => ScreenEnableMode.Inherit, stored: true),
        new PropertyDescriptor<VisibleOnScreenEnabler, string>(nameof(EnableNodePath), node => node.EnableNodePath, (node, value) => node.EnableNodePath = value,
            _ => "..", stored: true)
    ];

    /// <summary>Creates an enabler targeting its parent with inherited processing when visible.</summary>
    public VisibleOnScreenEnabler() { }

    /// <summary>Gets or sets the target's processing policy while on screen.</summary>
    /// <value>Inherit initially; Always and WhenPaused are also supported.</value>
    /// <remarks>Every assignment updates the current cached target immediately, even if the value is unchanged.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or the target cannot change physics participation.</exception>
    /// <exception cref="ObjectDisposedException">The enabler is disposed.</exception>
    public ScreenEnableMode EnableMode
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _enableMode; }
        set
        {
            EnsureMutable();
            if (value is < ScreenEnableMode.Inherit or > ScreenEnableMode.WhenPaused) throw new ArgumentOutOfRangeException(nameof(value));
            _enableMode = value;
            if (IsInsideTree) ApplyScreenState();
        }
    }

    /// <summary>Gets or sets the relative path of the weakly cached processing target.</summary>
    /// <value>".." initially; empty disables targeting.</value>
    /// <remarks>A changed nonempty path resolves once while attached. An unresolved path throws after committing
    /// the path and clearing the cache. The old target's mode is not restored; equal paths do not rebind.</remarks>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="InvalidOperationException">The path cannot resolve or scene/target mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The enabler is disposed.</exception>
    public string EnableNodePath
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _enableNodePath; }
        set
        {
            EnsureMutable(); ArgumentNullException.ThrowIfNull(value);
            if (_enableNodePath == value) return;
            _enableNodePath = value; _target = null;
            if (IsInsideTree) { CacheTarget(); ApplyScreenState(); }
        }
    }

    private void CacheTarget()
    {
        _target = null;
        if (_enableNodePath.Length == 0) return;
        var target = GetNodeOrNull(_enableNodePath) ?? throw new InvalidOperationException("EnableNodePath does not resolve to a node.");
        _target = new(target);
    }

    internal override void ApplyScreenState()
    {
        if (_target is null || !_target.TryGetTarget(out var target) || target.IsDisposed) return;
        target.ProcessMode = !IsOnScreen() ? ProcessMode.Disabled : _enableMode switch
        {
            ScreenEnableMode.Inherit => ProcessMode.Inherit,
            ScreenEnableMode.Always => ProcessMode.Always,
            _ => ProcessMode.WhenPaused
        };
    }

    internal override void OnTreeMembershipChanged(bool entering)
    {
        _target = null;
        base.OnTreeMembershipChanged(entering);
        if (entering && !IsDisposed && IsInsideTree) { CacheTarget(); ApplyScreenState(); }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(EnableProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(VisibleOnScreenEnabler) ? CreateEnabler : base.CreateSceneInstanceFactory();
    private static Node CreateEnabler() => new VisibleOnScreenEnabler();
}
