namespace Electron2D;

public sealed partial class SceneTree
{
    internal PanelContainer? TooltipPanel => _gui.TooltipPanel;

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
        _gui.TooltipGeneration++; _gui.TooltipScheduled = false; _gui.TooltipControl = null; _gui.TooltipOwner = null;
        _gui.ShownTooltipText = string.Empty;
        var layer = _gui.TooltipLayer; _gui.TooltipLayer = null; _gui.TooltipPanel = null;
        if (layer is not { IsDisposed: false }) return;
        if (!rootDisposing && !IsClosing) QueueDelete(layer);
        else if (!Root.IsAncestorOf(layer)) layer.Dispose();
    }

    private bool IsLiveTooltipTarget(Control? control) => control is { IsDisposed: false } &&
        ReferenceEquals(control.Tree, this) && control.IsVisibleInTree && control.EffectiveMouseFilter != MouseFilter.Ignore;

    private void UpdateTooltipInput(InputEvent input)
    {
        if (input is InputEventMouseButton { Pressed: true } or InputEventGesture)
        { CancelTooltip(); return; }
        if (input is not InputEventMouse && input.IsActionType() && InputMap.HasAction("ui_cancel") && input.IsActionPressed("ui_cancel"))
        {
            var shown = _gui.TooltipPanel is not null; CancelTooltip();
            if (shown) SetInputAsHandled();
            return;
        }
        if (input is not InputEventMouseMotion motion) return;
        if (motion.ButtonMask != MouseButtonMask.None) { CancelTooltip(); return; }
        var target = _gui.GuiHoverTarget;
        if (!IsLiveTooltipTarget(target)) { CancelTooltip(); return; }
        if (_gui.TooltipLayer is not null)
        {
            var text = TrimTooltipEdges(FindTooltip(target!, motion.Position, out _, out _));
            if (text.SequenceEqual(_gui.ShownTooltipText.AsSpan())) return;
            CancelTooltip();
        }
        if (!target!.CanProcess()) return;
        if (!ReferenceEquals(target, _gui.TooltipControl) || _gui.TooltipPosition.DistanceSquaredTo(motion.Position) > 25)
        {
            _gui.TooltipControl = target; _gui.TooltipPosition = motion.Position;
            _gui.TooltipRemaining = ProjectSettings.GetWithOverride(ProjectSettings.TooltipDelaySeconds);
            _gui.TooltipScheduled = true; _gui.TooltipGeneration++;
        }
    }

    private void ProcessTooltip(double unscaledDelta, ref List<Exception>? errors)
    {
        try
        {
            if (_gui.TooltipControl is not null && !IsLiveTooltipTarget(_gui.TooltipControl)) { CancelTooltip(); return; }
            if (!_gui.TooltipScheduled) return;
            _gui.TooltipRemaining -= unscaledDelta;
            if (_gui.TooltipRemaining >= 0) return;
            _gui.TooltipScheduled = false;
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
        var target = _gui.TooltipControl;
        if (!IsLiveTooltipTarget(target) || _gui.Viewport is not { } viewport) return;
        var generation = _gui.TooltipGeneration;
        var rawText = FindTooltip(target!, _gui.GuiHoverKnown ? _gui.GuiHoverPosition : _gui.TooltipPosition, out var owner, out var localPosition);
        var trimmed = TrimTooltipEdges(rawText);
        var text = trimmed.Length == rawText.Length ? rawText : trimmed.ToString();
        if (generation != _gui.TooltipGeneration || !IsLiveTooltipTarget(target)) return;
        Control? content = owner.CreateTooltipControl(text);
        if (content is not null && (content.IsDisposed || content.Parent is not null || content.Tree is not null ||
            ReferenceEquals(content, owner) || content.IsAncestorOf(owner)))
            throw new InvalidOperationException("A custom tooltip must return a fresh detached control.");
        if (generation != _gui.TooltipGeneration || !IsLiveTooltipTarget(target)) { content?.Dispose(); return; }
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
            _gui.TooltipLayer = layer; _gui.TooltipPanel = panel; _gui.TooltipOwner = owner; _gui.ShownTooltipText = text;
            owner.AddChild(layer);
            if (generation != _gui.TooltipGeneration || layer.IsDisposed || !IsLiveTooltipTarget(target))
            { if (!layer.IsDisposed) layer.Dispose(); return; }

            if (!ReferenceEquals(content.Parent, panel)) throw new InvalidOperationException("Tooltip content left its presenter during entry.");
            var size = panel.GetCombinedMinimumSize().Ceil();
            if (generation != _gui.TooltipGeneration || layer.IsDisposed || !IsLiveTooltipTarget(target)) return;
            panel.Size = size;
            if (generation != _gui.TooltipGeneration || layer.IsDisposed || !IsLiveTooltipTarget(target)) return;
            var bounds = viewport.GetVisibleRect();
            var offset = ProjectSettings.GetWithOverride(ProjectSettings.TooltipPositionOffset);
            var position = _gui.TooltipPosition + offset;
            for (var axis = 0; axis < 2; axis++)
            {
                if (position[axis] + size[axis] > bounds.End[axis])
                {
                    position[axis] = _gui.TooltipPosition[axis] - size[axis] - offset[axis];
                    if (position[axis] < bounds.Position[axis]) position[axis] = bounds.End[axis] - size[axis];
                }
                else if (position[axis] < bounds.Position[axis]) position[axis] = bounds.Position[axis];
            }
            panel.Position = new(MathF.Truncate(position.X), MathF.Truncate(position.Y));
            panel.QueueSort();
        }
        catch (Exception error)
        {
            if (ReferenceEquals(_gui.TooltipLayer, layer)) { _gui.TooltipLayer = null; _gui.TooltipPanel = null; _gui.TooltipOwner = null; }
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
