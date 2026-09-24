namespace Electron2D;

public partial class Control
{
    private static readonly PropertyDescriptor[] FocusProperties =
    [
        new PropertyDescriptor<Control, string>(nameof(FocusNext), n => n.FocusNext, (n, v) => n.FocusNext = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<Control, string>(nameof(FocusPrevious), n => n.FocusPrevious, (n, v) => n.FocusPrevious = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<Control, string>(nameof(FocusNeighborLeft), n => n.FocusNeighborLeft, (n, v) => n.FocusNeighborLeft = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<Control, string>(nameof(FocusNeighborTop), n => n.FocusNeighborTop, (n, v) => n.FocusNeighborTop = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<Control, string>(nameof(FocusNeighborRight), n => n.FocusNeighborRight, (n, v) => n.FocusNeighborRight = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<Control, string>(nameof(FocusNeighborBottom), n => n.FocusNeighborBottom, (n, v) => n.FocusNeighborBottom = v, _ => string.Empty, stored: true),
    ];

    private readonly string[] _focusNeighbors = new string[4];
    private string _focusNext = string.Empty;
    private string _focusPrevious = string.Empty;

    /// <summary>Gets or sets the relative path used before automatic forward focus traversal.</summary>
    /// <value>An empty path by default.</value>
    public string FocusNext { get { ThrowIfDisposed(); return _focusNext; } set { EnsureMutable(); _focusNext = value ?? throw new ArgumentNullException(nameof(value)); } }

    /// <summary>Gets or sets the relative path used before automatic backward focus traversal.</summary>
    /// <value>An empty path by default.</value>
    public string FocusPrevious { get { ThrowIfDisposed(); return _focusPrevious; } set { EnsureMutable(); _focusPrevious = value ?? throw new ArgumentNullException(nameof(value)); } }

    /// <summary>Gets or sets the relative path used before automatic leftward focus search.</summary>
    /// <value>An empty path by default.</value>
    public string FocusNeighborLeft { get => GetFocusNeighbor(Side.Left); set => SetFocusNeighbor(Side.Left, value); }
    /// <summary>Gets or sets the relative path used before automatic upward focus search.</summary>
    /// <value>An empty path by default.</value>
    public string FocusNeighborTop { get => GetFocusNeighbor(Side.Top); set => SetFocusNeighbor(Side.Top, value); }
    /// <summary>Gets or sets the relative path used before automatic rightward focus search.</summary>
    /// <value>An empty path by default.</value>
    public string FocusNeighborRight { get => GetFocusNeighbor(Side.Right); set => SetFocusNeighbor(Side.Right, value); }
    /// <summary>Gets or sets the relative path used before automatic downward focus search.</summary>
    /// <value>An empty path by default.</value>
    public string FocusNeighborBottom { get => GetFocusNeighbor(Side.Bottom); set => SetFocusNeighbor(Side.Bottom, value); }

    /// <summary>Gets the relative path for a directional focus neighbor.</summary>
    /// <param name="side">The side to query.</param>
    /// <returns>The configured path, or an empty string.</returns>
    public string GetFocusNeighbor(Side side) { ThrowIfDisposed(); return _focusNeighbors[FocusSideIndex(side)] ?? string.Empty; }

    /// <summary>Sets the relative path for a directional focus neighbor.</summary>
    /// <param name="side">The side to configure.</param>
    /// <param name="neighbor">A relative node path, or empty for automatic search.</param>
    public void SetFocusNeighbor(Side side, string neighbor)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(neighbor);
        _focusNeighbors[FocusSideIndex(side)] = neighbor;
    }

    /// <summary>Finds the next visible keyboard-navigable control, wrapping within the viewport.</summary>
    /// <returns>The next focus target, or null when none exists.</returns>
    public Control? FindNextValidFocus() => FindSequentialFocus(forward: true);

    /// <summary>Finds the previous visible keyboard-navigable control, wrapping within the viewport.</summary>
    /// <returns>The previous focus target, or null when none exists.</returns>
    public Control? FindPrevValidFocus() => FindSequentialFocus(forward: false);

    /// <summary>Finds a visible focus target in one direction, checking explicit paths first.</summary>
    /// <param name="side">The search direction.</param>
    /// <returns>The nearest eligible control, or null when none exists.</returns>
    public Control? FindValidFocusNeighbor(Side side)
    {
        ThrowIfDisposed();
        var index = FocusSideIndex(side);
        var viewport = GetViewport();
        if (viewport is null) return null;
        var visited = new HashSet<Control>(ReferenceEqualityComparer.Instance);
        for (Control? current = this; current is not null && visited.Add(current);)
        {
            var path = current._focusNeighbors[index];
            if (string.IsNullOrEmpty(path)) break;
            current = current.GetNodeOrNull(path) as Control;
            if (current is null) return null;
            if (visited.Contains(current)) break;
            if (current.IsFocusCandidate(viewport, explicitPath: true)) return current;
        }

        var source = GetGlobalRect().Abs();
        var sourceCenter = source.GetCenter();
        var sourceEdge = side switch
        {
            Side.Left => source.Position.X,
            Side.Top => source.Position.Y,
            Side.Right => source.End.X,
            _ => source.End.Y,
        };
        Control? best = null;
        var bestDistance = float.PositiveInfinity;
        var bestAlignment = float.PositiveInfinity;
        foreach (var candidate in NavigationControls(viewport))
        {
            if (ReferenceEquals(candidate, this) || !candidate.IsFocusCandidate(viewport, explicitPath: false)) continue;
            var area = candidate.GetGlobalRect().Abs();
            var candidateEdge = side switch
            {
                Side.Left => area.Position.X,
                Side.Top => area.Position.Y,
                Side.Right => area.End.X,
                _ => area.End.Y,
            };
            if (side is Side.Left or Side.Top ? candidateEdge >= sourceEdge : candidateEdge <= sourceEdge) continue;
            var delta = area.GetCenter() - sourceCenter;
            var gapX = Math.Max(0f, Math.Abs(delta.X) - (source.Size.X + area.Size.X) * 0.5f);
            var gapY = Math.Max(0f, Math.Abs(delta.Y) - (source.Size.Y + area.Size.Y) * 0.5f);
            var distance = gapX * gapX + gapY * gapY;
            var alignment = side is Side.Left or Side.Right ? Math.Abs(delta.Y) : Math.Abs(delta.X);
            if (distance < bestDistance || distance == bestDistance && alignment < bestAlignment)
            { best = candidate; bestDistance = distance; bestAlignment = alignment; }
        }
        return best;
    }

    private Control? FindSequentialFocus(bool forward)
    {
        ThrowIfDisposed();
        var viewport = GetViewport();
        if (viewport is null) return null;
        var path = forward ? _focusNext : _focusPrevious;
        if (!string.IsNullOrEmpty(path))
        {
            if (GetNodeOrNull(path) is not Control explicitTarget) return null;
            if (explicitTarget.IsFocusCandidate(viewport, explicitPath: true)) return explicitTarget;
        }

        var controls = NavigationControls(viewport);
        var start = controls.IndexOf(this);
        if (start < 0) return null;
        for (var offset = 1; offset <= controls.Count; offset++)
        {
            var index = (start + (forward ? offset : -offset) + controls.Count) % controls.Count;
            if (controls[index].IsFocusCandidate(viewport, explicitPath: false)) return controls[index];
        }
        return null;
    }

    private bool IsFocusCandidate(Viewport viewport, bool explicitPath) =>
        !IsDisposed && IsVisibleInTree && ReferenceEquals(GetViewport(), viewport) &&
        (EffectiveFocusMode == ControlFocusMode.All || explicitPath && EffectiveFocusMode == ControlFocusMode.Click);

    private static List<Control> NavigationControls(Viewport viewport)
    {
        var controls = new List<Control>();
        static void Visit(Node node, List<Control> output)
        {
            if (node is Control control)
            {
                if (!control.IsVisibleInTree || control.TopLevel) return;
                output.Add(control);
            }
            for (var index = 0; index < node.ChildCount; index++) Visit(node.GetChild(index), output);
        }
        Visit(viewport, controls);
        return controls;
    }

    private static int FocusSideIndex(Side side) => side switch
    {
        Side.Left => 0,
        Side.Top => 1,
        Side.Right => 2,
        Side.Bottom => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };
}
