namespace Electron2D;

/// <summary>Composites same-Z canvas descendants as one object before applying its own appearance.</summary>
/// <remarks>The renderer fits a screen-space rectangle around submitted children and uses a native transparent
/// backbuffer. SelfModulate applies to the completed group, avoiding repeated opacity in overlapping children.
/// An assigned material replaces the default group shader. Nested groups share the backbuffer and are unsupported;
/// GetConfigurationWarnings reports that boundary. Custom shaders and mipmap generation require the GPU profile.</remarks>
public class CanvasGroup : Entity
{
    private float _fitMargin = 10, _clearMargin = 10;
    private bool _useMipmaps;
    /// <summary>Creates a group with ten-pixel fit/clear margins and mipmaps disabled.</summary>
    public CanvasGroup() { }
    /// <summary>Gets or sets the screen-pixel expansion of the fitted drawable rectangle.</summary>
    /// <value>Ten initially; finite and nonnegative.</value>
    /// <remarks>Every assignment commits and requests redraw. Larger margins allow material effects outside child bounds.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The margin is negative or nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public float FitMargin { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _fitMargin; } set { EnsureMutable(); Margin(value); _fitMargin = value; QueueRedraw(); } }
    /// <summary>Gets or sets the additional screen-pixel expansion of the transparent clearing rectangle.</summary>
    /// <value>Ten initially; finite and nonnegative.</value>
    /// <remarks>The extra cleared border prevents stale backbuffer data from entering filtered group samples.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The margin is negative or nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public float ClearMargin { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _clearMargin; } set { EnsureMutable(); Margin(value); _clearMargin = value; QueueRedraw(); } }
    /// <summary>Gets or sets whether native backbuffer mipmaps are generated before compositing.</summary>
    /// <value>False initially.</value>
    /// <remarks>Generation is useful for explicit LOD sampling in a custom group shader. The compatibility profile
    /// rejects a submitted mipmapped group before native drawing rather than ignoring this policy.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public bool UseMipmaps { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _useMipmaps; } set { EnsureMutable(); _useMipmaps = value; } }
    private static void Margin(float value) { if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(GroupProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(CanvasGroup) ? CreateGroup : base.CreateSceneInstanceFactory();
    private static Node CreateGroup() => new CanvasGroup();
    private static readonly PropertyDescriptor[] GroupProperties =
    [new PropertyDescriptor<CanvasGroup, float>(nameof(FitMargin), n => n.FitMargin, (n,v) => n.FitMargin=v, _ => 10, (_,v) => float.IsFinite(v) && v>=0, stored:true),
     new PropertyDescriptor<CanvasGroup, float>(nameof(ClearMargin), n => n.ClearMargin, (n,v) => n.ClearMargin=v, _ => 10, (_,v) => float.IsFinite(v) && v>=0, stored:true),
     new PropertyDescriptor<CanvasGroup, bool>(nameof(UseMipmaps), n => n.UseMipmaps, (n,v) => n.UseMipmaps=v, _ => false, stored:true)];
}
