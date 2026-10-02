namespace Electron2D;

public sealed partial class SceneTree
{
    private Control? _tooltipControl, _tooltipOwner;
    private CanvasLayer? _tooltipLayer;
    private PanelContainer? _tooltipPanel;
    private Vector2 _tooltipPosition;
    private string _shownTooltipText = string.Empty;
    private double _tooltipRemaining;
    private bool _tooltipScheduled;
    private ulong _tooltipGeneration;
    internal PanelContainer? TooltipPanel => _tooltipPanel;

    internal bool IsTooltipControl(Control control) => IsTooltipNode(control);
    internal static bool IsTooltipNode(Node node)
    {
        for (Node? current = node; current is not null; current = current.Parent)
            if (current is CanvasLayer { IsTooltipLayer: true }) return true;
        return false;
    }

    internal void CancelTooltip(bool rootDisposing = false)
    {
        EnsureOwnerThread();
        _tooltipGeneration++; _tooltipScheduled = false; _tooltipControl = null; _tooltipOwner = null;
        _shownTooltipText = string.Empty;
        var layer = _tooltipLayer; _tooltipLayer = null; _tooltipPanel = null;
        if (layer is not { IsDisposed: false }) return;
        if (!rootDisposing) QueueDelete(layer);
        else if (!Root.IsAncestorOf(layer)) layer.Dispose();
    }

    private bool IsLiveTooltipTarget(Control? control) => control is { IsDisposed: false } &&
        ReferenceEquals(control.Tree, this) && control.IsVisibleInTree && control.EffectiveMouseFilter != MouseFilter.Ignore;

    private void UpdateTooltipInput(InputEvent input)
    {
        if (input is InputEventMouseButton { Pressed: true } or InputEventGesture)
        { CancelTooltip(); return; }
        if (input is not InputEventMouse && input.IsActionType() && InputMap.Instance.HasAction("ui_cancel") && input.IsActionPressed("ui_cancel"))
        {
            var shown = _tooltipPanel is not null; CancelTooltip();
            if (shown) SetInputAsHandled();
            return;
        }
        if (input is not InputEventMouseMotion motion) return;
        if (motion.ButtonMask != MouseButtonMask.None) { CancelTooltip(); return; }
        var target = _guiHoverTarget;
        if (!IsLiveTooltipTarget(target)) { CancelTooltip(); return; }
        if (_tooltipLayer is not null)
        {
            var text = TrimTooltipEdges(FindTooltip(target!, motion.Position, out _, out _));
            if (text.SequenceEqual(_shownTooltipText.AsSpan())) return;
            CancelTooltip();
        }
        if (!target!.CanProcess()) return;
        if (!ReferenceEquals(target, _tooltipControl) || _tooltipPosition.DistanceSquaredTo(motion.Position) > 25)
        {
            _tooltipControl = target; _tooltipPosition = motion.Position;
            _tooltipRemaining = ProjectSettings.Instance.GetWithOverride(ProjectSettings.TooltipDelaySeconds);
            _tooltipScheduled = true; _tooltipGeneration++;
        }
    }

    private void ProcessTooltip(double unscaledDelta, ref List<Exception>? errors)
    {
        try
        {
            if (_tooltipControl is not null && !IsLiveTooltipTarget(_tooltipControl)) { CancelTooltip(); return; }
            if (!_tooltipScheduled) return;
            _tooltipRemaining -= unscaledDelta;
            if (_tooltipRemaining >= 0) return;
            _tooltipScheduled = false;
            ShowScheduledTooltip();
        }
        catch (Exception error) { CollectException(ref errors, error); }
    }

    private static string FindTooltip(Control target, Vector2 viewportPosition, out Control owner, out Vector2 localPosition)
    {
        owner = target; localPosition = target.MakeCanvasPositionLocal(viewportPosition);
        for (var current = target; ;)
        {
            owner = current;
            var text = current.GetTooltip(localPosition);
            if (text.Length != 0 || current.EffectiveMouseFilter == MouseFilter.Stop || current.TopLevel ||
                current.GetParentItem() is not Control parent) return text;
            localPosition = current.GetTransform() * localPosition;
            current = parent;
        }
    }

    private void ShowScheduledTooltip()
    {
        var target = _tooltipControl;
        if (!IsLiveTooltipTarget(target) || Root is not Viewport viewport) return;
        var generation = _tooltipGeneration;
        var rawText = FindTooltip(target!, _guiHoverKnown ? _guiHoverPosition : _tooltipPosition, out var owner, out var localPosition);
        var trimmed = TrimTooltipEdges(rawText);
        var text = trimmed.Length == rawText.Length ? rawText : trimmed.ToString();
        if (generation != _tooltipGeneration || !IsLiveTooltipTarget(target)) return;
        Control? content = owner.CreateTooltipControl(text);
        if (content is not null && (content.IsDisposed || content.Parent is not null || content.Tree is not null ||
            ReferenceEquals(content, owner) || content.IsAncestorOf(owner)))
            throw new InvalidOperationException("A custom tooltip must return a fresh detached control.");
        if (generation != _tooltipGeneration || !IsLiveTooltipTarget(target)) { content?.Dispose(); return; }
        if (content is { Visible: false }) { content.Dispose(); return; }
        if (content is null && text.Length == 0) return;

        CanvasLayer? layer = null;
        try
        {
            content ??= new Label
            {
                Name = "TooltipLabel",
                ThemeTypeVariation = "TooltipLabel",
                Text = text,
                AutoTranslateMode = owner.GetTooltipTranslationMode(localPosition)
            };
            var name = "Tooltip";
            for (var suffix = 2; owner.GetNodeOrNull(name) is not null; suffix++) name = "Tooltip" + suffix;
            layer = new CanvasLayer { Name = name, Layer = int.MaxValue, IsTooltipLayer = true };
            var panel = new PanelContainer
            {
                Name = "TooltipPanel",
                ThemeTypeVariation = "TooltipPanel",
                MouseFilter = MouseFilter.Ignore,
                MouseBehaviorRecursive = RecursiveBehavior.Disabled,
                FocusBehaviorRecursive = RecursiveBehavior.Disabled
            };
            layer.AddChild(panel); panel.AddChild(content);
            _tooltipLayer = layer; _tooltipPanel = panel; _tooltipOwner = owner; _shownTooltipText = text;
            owner.AddChild(layer);
            if (generation != _tooltipGeneration || layer.IsDisposed || !IsLiveTooltipTarget(target))
            { if (!layer.IsDisposed) layer.Dispose(); return; }

            if (!ReferenceEquals(content.Parent, panel)) throw new InvalidOperationException("Tooltip content left its presenter during entry.");
            var size = panel.GetCombinedMinimumSize().Ceil();
            if (generation != _tooltipGeneration || layer.IsDisposed || !IsLiveTooltipTarget(target)) return;
            panel.Size = size;
            if (generation != _tooltipGeneration || layer.IsDisposed || !IsLiveTooltipTarget(target)) return;
            var bounds = viewport.GetVisibleRect();
            var offset = ProjectSettings.Instance.GetWithOverride(ProjectSettings.TooltipPositionOffset);
            var position = _tooltipPosition + offset;
            for (var axis = 0; axis < 2; axis++)
            {
                if (position[axis] + size[axis] > bounds.End[axis])
                {
                    position[axis] = _tooltipPosition[axis] - size[axis] - offset[axis];
                    if (position[axis] < bounds.Position[axis]) position[axis] = bounds.End[axis] - size[axis];
                }
                else if (position[axis] < bounds.Position[axis]) position[axis] = bounds.Position[axis];
            }
            panel.Position = new(MathF.Truncate(position.X), MathF.Truncate(position.Y));
            panel.QueueSort();
        }
        catch (Exception error)
        {
            if (ReferenceEquals(_tooltipLayer, layer)) { _tooltipLayer = null; _tooltipPanel = null; _tooltipOwner = null; }
            Exception? cleanupError = null;
            try
            {
                if (layer is { IsDisposed: false }) layer.Dispose();
                else if (content is { IsDisposed: false, Parent: null }) content.Dispose();
            }
            catch (Exception cleanup) { cleanupError = cleanup; }
            Resource.ThrowCombined(error, cleanupError);
            throw;
        }
    }

    private static ReadOnlySpan<char> TrimTooltipEdges(string text)
    {
        var span = text.AsSpan();
        while (!span.IsEmpty && span[0] <= ' ') span = span[1..];
        while (!span.IsEmpty && span[^1] <= ' ') span = span[..^1];
        return span;
    }
}
