namespace Electron2D;

public sealed partial class NavigationAgent
{
    private NavigationAgentDebugCanvas? _debugCanvas;
    private bool _debugEnabled, _debugUseCustom;
    private Color _debugPathCustomColor = Colors.White;
    private float _debugPathCustomLineWidth = -1, _debugPathCustomPointSize = 4;
    /// <summary>Gets or sets whether this agent's current path is drawn.</summary>
    /// <value>False initially; stored in scenes. Works in every build configuration.</value>
    /// <remarks>Uses a lazily created internal retained canvas on the parent's canvas, at MaximumZIndex minus one.
    /// Paths stay in world coordinates as the parent moves. Parent visibility applies; parent modulation and materials do not.
    /// Default path enablement/color/point size are sampled by NavigationServer from project settings on first diagnostics use.
    /// Drawing observes the current result without calculating or advancing navigation and does not depend on DebugNavigationHint.</remarks>
    /// <exception cref="InvalidOperationException">Scene mutation, capture or navigation delivery prevents this edit.</exception>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="AggregateException">Canvas attachment or cleanup callbacks fail.</exception>
    public bool DebugEnabled
    {
        get { Read(); return _debugEnabled; }
        set
        {
            EnsureMutable();
            if (value && _debugCanvas is null)
            {
                var canvas = new NavigationAgentDebugCanvas(this);
                try { AddChild(canvas, InternalMode.Back); _debugCanvas = canvas; }
                catch (Exception addError)
                {
                    try { canvas.Dispose(); }
                    catch (Exception cleanupError) { throw new AggregateException("Navigation debug canvas creation and cleanup failed.", addError, cleanupError); }
                    throw;
                }
            }
            _debugEnabled = value; _debugCanvas?.InvalidateCanvas();
        }
    }
    /// <summary>Gets or sets whether custom path color and waypoint size replace the defaults.</summary>
    /// <value>False initially; stored in scenes. Line width always uses DebugPathCustomLineWidth.</value>
    /// <exception cref="InvalidOperationException">Scene mutation, capture or navigation delivery prevents this edit.</exception>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    public bool DebugUseCustom { get { Read(); return _debugUseCustom; } set { EnsureMutable(); _debugUseCustom = value; _debugCanvas?.InvalidateCanvas(); } }
    /// <summary>Gets or sets the finite path and waypoint color used with DebugUseCustom.</summary>
    /// <value>Opaque white initially; stored in scenes.</value>
    /// <exception cref="ArgumentException">The color is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Scene mutation, capture or navigation delivery prevents this edit.</exception>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    public Color DebugPathCustomColor { get { Read(); return _debugPathCustomColor; } set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Debug path color must be finite.", nameof(value)); _debugPathCustomColor = value; _debugCanvas?.InvalidateCanvas(); } }
    /// <summary>Gets or sets the finite path line width in canvas units.</summary>
    /// <value>Minus one initially; negative widths use framebuffer-width lines. Zero omits the line. Stored in scenes.</value>
    /// <exception cref="InvalidOperationException">Scene mutation, capture or navigation delivery prevents this edit.</exception>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float DebugPathCustomLineWidth { get { Read(); return _debugPathCustomLineWidth; } set { EnsureMutable(); _debugPathCustomLineWidth = Number(value); _debugCanvas?.InvalidateCanvas(); } }
    /// <summary>Gets or sets the custom square waypoint size.</summary>
    /// <value>Four initially; finite negative values clamp to zero. Stored in scenes.</value>
    /// <remarks>Zero hides waypoints even when DebugUseCustom is false; otherwise the selected default/custom point size is used.</remarks>
    /// <exception cref="InvalidOperationException">Scene mutation, capture or navigation delivery prevents this edit.</exception>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float DebugPathCustomPointSize { get { Read(); return _debugPathCustomPointSize; } set { EnsureMutable(); _debugPathCustomPointSize = Math.Max(0, Number(value)); _debugCanvas?.InvalidateCanvas(); } }
    internal bool DebugPathVisible => _debugEnabled && IsInsideTree && Parent is Entity { IsVisibleInTree: true };
    internal NavigationPathQueryData DebugPathData => _result.Snapshot();
    private static readonly PropertyDescriptor[] DebugProperties =
    [
        new PropertyDescriptor<NavigationAgent, bool>(nameof(DebugEnabled), n => n.DebugEnabled, (n, v) => n.DebugEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<NavigationAgent, bool>(nameof(DebugUseCustom), n => n.DebugUseCustom, (n, v) => n.DebugUseCustom = v, _ => false, stored: true),
        new PropertyDescriptor<NavigationAgent, Color>(nameof(DebugPathCustomColor), n => n.DebugPathCustomColor, (n, v) => n.DebugPathCustomColor = v, _ => Colors.White, stored: true),
        new PropertyDescriptor<NavigationAgent, float>(nameof(DebugPathCustomLineWidth), n => n.DebugPathCustomLineWidth, (n, v) => n.DebugPathCustomLineWidth = v, _ => -1f, stored: true),
        new PropertyDescriptor<NavigationAgent, float>(nameof(DebugPathCustomPointSize), n => n.DebugPathCustomPointSize, (n, v) => n.DebugPathCustomPointSize = v, _ => 4f, stored: true)
    ];
}

/// <summary>Retains an agent's world-space path without adding a game-visible child or advancing navigation.</summary>
internal sealed class NavigationAgentDebugCanvas : Entity
{
    private readonly NavigationAgent _agent;
    private Vector2[]? _path;
    private bool _visible;
    internal NavigationAgentDebugCanvas(NavigationAgent agent) { _agent = agent; ZIndex = MaximumZIndex - 1; ZAsRelative = false; }
    internal override bool CanvasUsesWorldCoordinates => true;
    internal void Refresh()
    {
        var visible = _agent.DebugPathVisible;
        var path = _agent.DebugPathData.Path;
        if (_visible != visible || !ReferenceEquals(_path, path)) { _visible = visible; _path = path; InvalidateCanvas(); }
    }
    /// <inheritdoc />
    protected override void OnDraw()
    {
        var settings = NavigationServer.Service.AgentDebugSettings;
        if (!_agent.DebugPathVisible || !settings.Enabled || _path is not { Length: > 1 } path) return;
        var color = _agent.DebugUseCustom ? _agent.DebugPathCustomColor : settings.Color;
        DrawPolyline(path, color, _agent.DebugPathCustomLineWidth);
        if (_agent.DebugPathCustomPointSize <= 0) return;
        var size = _agent.DebugUseCustom ? _agent.DebugPathCustomPointSize : settings.PointSize;
        if (size <= 0) return;
        var half = new Vector2(size * .5f, size * .5f);
        foreach (var point in path) DrawRect(new(point - half, new(size, size)), color);
    }
}
