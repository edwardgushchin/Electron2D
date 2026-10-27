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

    /// <summary>Gets or sets the node path used before automatic forward focus traversal.</summary>
    /// <value>An empty path by default.</value>
    public string FocusNext { get { ThrowIfDisposed(); return _focusNext; } set { EnsureMutable(); _focusNext = value ?? throw new ArgumentNullException(nameof(value)); } }

    /// <summary>Gets or sets the node path used before automatic backward focus traversal.</summary>
    /// <value>An empty path by default.</value>
    public string FocusPrevious { get { ThrowIfDisposed(); return _focusPrevious; } set { EnsureMutable(); _focusPrevious = value ?? throw new ArgumentNullException(nameof(value)); } }

    /// <summary>Gets or sets the node path used before automatic leftward focus search.</summary>
    /// <value>An empty path by default.</value>
    public string FocusNeighborLeft { get => GetFocusNeighbor(Side.Left); set => SetFocusNeighbor(Side.Left, value); }
    /// <summary>Gets or sets the node path used before automatic upward focus search.</summary>
    /// <value>An empty path by default.</value>
    public string FocusNeighborTop { get => GetFocusNeighbor(Side.Top); set => SetFocusNeighbor(Side.Top, value); }
    /// <summary>Gets or sets the node path used before automatic rightward focus search.</summary>
    /// <value>An empty path by default.</value>
    public string FocusNeighborRight { get => GetFocusNeighbor(Side.Right); set => SetFocusNeighbor(Side.Right, value); }
    /// <summary>Gets or sets the node path used before automatic downward focus search.</summary>
    /// <value>An empty path by default.</value>
    public string FocusNeighborBottom { get => GetFocusNeighbor(Side.Bottom); set => SetFocusNeighbor(Side.Bottom, value); }

    /// <summary>Gets the node path for a directional focus neighbor.</summary>
    /// <param name="side">The side to query.</param>
    /// <returns>The configured path, or an empty string.</returns>
    public string GetFocusNeighbor(Side side) { ThrowIfDisposed(); return _focusNeighbors[FocusSideIndex(side)] ?? string.Empty; }

    /// <summary>Sets the node path for a directional focus neighbor.</summary>
    /// <param name="side">The side to configure.</param>
    /// <param name="neighbor">A node path, or empty for automatic search.</param>
    public void SetFocusNeighbor(Side side, string neighbor)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(neighbor);
        _focusNeighbors[FocusSideIndex(side)] = neighbor;
    }

    /// <summary>Finds the next keyboard focus target in Control scene order.</summary>
    /// <returns>The explicit target or next eligible control, wrapping within the UI root and its direct Window siblings; otherwise null.</returns>
    /// <remarks>Neutral nodes and top-level controls start separate UI roots. Explicit targets may use Click focus and cross viewport boundaries.</remarks>
    public Control? FindNextValidFocus() => FindSequentialFocus(forward: true);

    /// <summary>Finds the previous keyboard focus target in Control scene order.</summary>
    /// <returns>The explicit target or previous eligible control, wrapping within the UI root and its direct Window siblings; otherwise null.</returns>
    /// <remarks>Neutral nodes and top-level controls start separate UI roots. Explicit targets may use Click focus and cross viewport boundaries.</remarks>
    public Control? FindPrevValidFocus() => FindSequentialFocus(forward: false);

    /// <summary>Finds a directional focus target within this UI root, checking explicit neighbor chains first.</summary>
    /// <param name="side">The search direction.</param>
    /// <returns>The eligible neighbor, or null for an invalid path, an exhausted chain or no spatial candidate.</returns>
    /// <remarks>Explicit targets may cross viewport boundaries and use Click focus. Ineligible explicit targets continue the search from their own geometry.
    /// Automatic search ranks rectangle gaps, breaks ties by alignment, and clips scroll contents unless an ancestor follows focus.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The side is unknown.</exception>
    public Control? FindValidFocusNeighbor(Side side)
    {
        ThrowIfDisposed();
        var index = FocusSideIndex(side);
        var from = this;
        for (var depth = 0; depth < 512; depth++)
        {
            var path = from._focusNeighbors[index];
            if (string.IsNullOrEmpty(path)) return from.FindSpatialFocus(side);
            var target = from.ResolveFocusPath(path, "Neighbor");
            if (target is null) return null;
            if (target.IsVisibleInTree && target.IsNavigationFocusable(explicitPath: true)) return target;
            from = target;
        }
        return null;
    }

    private Control? ResolveFocusPath(string path, string direction)
    {
        var node = GetNodeOrNull(path);
        if (node is Control control) return control;
        OS.LogError(node is null ? $"{direction} focus node path is invalid: '{path}'." : $"{direction} focus node is not a control: '{node.Name}'.");
        return null;
    }

    private Control? FindSequentialFocus(bool forward)
    {
        ThrowIfDisposed();
        var path = forward ? _focusNext : _focusPrevious;
        if (!string.IsNullOrEmpty(path))
        {
            var target = ResolveFocusPath(path, forward ? "Next" : "Previous");
            if (target is null) return null;
            if (target.IsVisibleInTree && target.IsNavigationFocusable(explicitPath: true)) return target;
        }

        Control from = this, checkpoint = this;
        var windowIndex = -1;
        long power = 1, distance = 0;
        while (true)
        {
            var candidate = forward ? NextFocusControl(from, ref windowIndex) : PreviousFocusControl(from, ref windowIndex);
            if (candidate is null) return null;
            if (candidate.IsNavigationFocusable(explicitPath: false)) return candidate;
            // Brent's cycle check keeps hidden-source and empty-root traversal bounded without per-query storage.
            if (ReferenceEquals(candidate, checkpoint)) return null;
            if (++distance == power) { checkpoint = candidate; power *= 2; distance = 0; }
            from = candidate;
        }
    }

    private Control? NextFocusControl(Control from, ref int windowIndex)
    {
        for (var i = 0; i < from.GetChildCount(true); i++)
            if (from.GetChild(i, true) is Control { TopLevel: false, IsVisibleInTree: true } child) return child;
        for (var current = from; !current.TopLevel && current.Parent is Control parent; current = parent)
            for (var i = current.GetIndex(true) + 1; i < parent.GetChildCount(true); i++)
                if (parent.GetChild(i, true) is Control { TopLevel: false, IsVisibleInTree: true } sibling) return sibling;
        Control? root = this;
        while (root is not null && !root.TopLevel && !root.IsFocusRoot()) root = root.Parent as Control;
        return root?.Parent is Window window ? WindowFocusRoot(window, root, true, ref windowIndex) ?? root : root;
    }

    private static Control PreviousFocusControl(Control from, ref int windowIndex)
    {
        if (from.TopLevel || from.Parent is not Control parent)
        {
            var previous = from.Parent is Window window ? WindowFocusRoot(window, from, false, ref windowIndex) : null;
            return LastFocusDescendant(previous ?? from);
        }
        for (var i = from.GetIndex(true) - 1; i >= 0; i--)
            if (parent.GetChild(i, true) is Control { TopLevel: false, IsVisibleInTree: true } sibling) return LastFocusDescendant(sibling);
        return parent;
    }

    private static Control? WindowFocusRoot(Window window, Control root, bool forward, ref int index)
    {
        if (index < 0) index = root.GetIndex(true);
        var count = window.GetChildCount(true);
        for (var offset = 1; offset <= count; offset++)
        {
            var next = (index + (forward ? offset : -offset) + count) % count;
            if (window.GetChild(next, true) is not Control { TopLevel: false, IsVisibleInTree: true } candidate) continue;
            index = next; return candidate;
        }
        return null;
    }

    private static Control LastFocusDescendant(Control from)
    {
        while (true)
        {
            Control? child = null;
            for (var i = from.GetChildCount(true) - 1; i >= 0; i--)
                if (from.GetChild(i, true) is Control { TopLevel: false, IsVisibleInTree: true } candidate) { child = candidate; break; }
            if (child is null) return from;
            from = child;
        }
    }

    private bool IsNavigationFocusable(bool explicitPath) => !IsDisposed &&
        (EffectiveFocusMode == FocusMode.All || EffectiveFocusMode == FocusMode.Accessibility && AccessibilityServer.IsActive || explicitPath && EffectiveFocusMode == FocusMode.Click);

    private bool IsFocusRoot()
    {
        if (!IsInsideTree) return false;
        for (CanvasItem item = this; !item.TopLevel;)
        {
            if (item.Parent is not CanvasItem parent) break;
            if (parent is Control) return false;
            item = parent;
        }
        return true;
    }

    private Control? FindSpatialFocus(Side side)
    {
        var direction = side switch { Side.Left => new Vector2(-1, 0), Side.Top => new Vector2(0, -1), Side.Right => new Vector2(1, 0), _ => new Vector2(0, 1) };
        var area = GetGlobalRect();
        var minimum = Math.Max(direction.Dot(area.Position), direction.Dot(area.End));
        var clamp = new Rect2(-1e7f, -1e7f, 2e7f, 2e7f);
        var distance = 1e14f;
        Control? result = null;
        var resultArea = default(Rect2);
        Node? root = this;
        for (; root is not null; root = root.Parent)
        {
            if (root is ScrollContainer scroll)
            {
                var scrollArea = scroll.GetGlobalRect();
                if (result is not null && !scroll.FollowFocus && !scrollArea.Intersects(resultArea)) result = null;
                if (result is null)
                {
                    var begin = direction.Dot(scrollArea.Position); var end = direction.Dot(scrollArea.End);
                    var low = Math.Min(begin, end); var high = Math.Max(begin, end);
                    if (!scroll.FollowFocus && minimum < low) minimum = low;
                    if (scroll.FollowFocus || high > minimum) ScanFocusNeighbors(direction, root, area, clamp, minimum, ref distance, ref result);
                    if (result is null) minimum = high;
                    else
                    {
                        resultArea = result.GetGlobalRect();
                        if (scroll.FollowFocus)
                        {
                            begin = direction.Dot(resultArea.Position); end = direction.Dot(resultArea.End);
                            var near = Math.Min(begin, end); var far = Math.Max(begin, end);
                            if (far > high) resultArea.Position += (high - far) * direction;
                            else if (near < low) resultArea.Position += (low - near) * direction;
                        }
                    }
                }
            }
            if (root is Control control && control.IsFocusRoot()) break;
        }
        if (result is null && root is not null) ScanFocusNeighbors(direction, root, area, clamp, minimum, ref distance, ref result);
        return result;
    }

    private void ScanFocusNeighbors(Vector2 direction, Node node, Rect2 source, Rect2 clamp, float minimum, ref float distance, ref Control? result)
    {
        if (node is Viewport) return;
        var ancestorContainer = node is Container && node.IsAncestorOf(this);
        if (node is Control control && !ReferenceEquals(control, this) && control.IsNavigationFocusable(explicitPath: false) && !ancestorContainer)
        {
            var area = control.GetGlobalRect();
            if (clamp.Intersects(area))
            {
                area = area.Intersection(clamp);
                if (Math.Max(direction.Dot(area.Position), direction.Dot(area.End)) > minimum + 0.00001f)
                {
                    var delta = area.GetCenter() - source.GetCenter();
                    var gap = delta.Abs() - 0.5f * area.Size - 0.5f * source.Size;
                    var x = Math.Max(0, gap.X); var y = Math.Max(0, gap.Y); var squared = x * x + y * y;
                    if (result is null || squared < distance || squared == distance && Math.Abs(direction.Cross(delta)) < Math.Abs(direction.Cross(result.GetGlobalRect().GetCenter() - source.GetCenter())))
                    { distance = squared; result = control; }
                }
            }
        }
        if (node is ScrollContainer scroll && (!scroll.FollowFocus || !ancestorContainer))
        {
            clamp = clamp.Intersection(scroll.GetGlobalRect());
            if (!clamp.HasArea()) return;
        }
        for (var i = 0; i < node.GetChildCount(true); i++)
        {
            var child = node.GetChild(i, true);
            if (child is Control c && (c.IsFocusRoot() || !c.IsVisibleInTree || c is ScrollContainer && c.IsAncestorOf(this))) continue;
            ScanFocusNeighbors(direction, child, source, clamp, minimum, ref distance, ref result);
        }
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
