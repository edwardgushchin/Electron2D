namespace Electron2D;

/// <summary>Applies a color tint to its entire scene canvas.</summary>
/// <remarks>One visible node controls each canvas. Other visible modulators wait until the selected node
/// hides or leaves. Independent CanvasLayer and World canvases retain independent colors. This node
/// contributes no geometry; its transform does not limit the tinted area.</remarks>
public class CanvasModulate : Entity
{
    private Color _color = Colors.White;
    private RenderingCanvasRuntime? _canvas;
    private readonly WeakReference<CanvasModulate> _registration;
    internal Color AuthoredColor => _color;

    /// <summary>Creates a visible detached modulator with opaque white color.</summary>
    public CanvasModulate() { _registration = new(this); }

    /// <summary>Gets or sets the tint applied when this node is selected on its canvas.</summary>
    /// <value>Opaque white initially; all finite color components are accepted.</value>
    /// <remarks>Each assignment commits, including equal values. Hidden or waiting nodes retain their
    /// authored color without changing the selected canvas. No redraw of sibling commands is required.</remarks>
    /// <exception cref="ArgumentException">A color component is nonfinite.</exception>
    /// <remarks>Reads permit the owner or any active processing group; writes permit the owner/current group.
    /// Caller coordination is required for concurrent observations. Native rendering consumes the joined managed color state.</remarks>
    /// <exception cref="InvalidOperationException">An attached access is outside its permitted processing scope or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public Color Color
    {
        get { ThrowIfDisposed(); Tree?.EnsureProcessingReadAccess(); return _color; }
        set
        {
            EnsureProcessingAccess(); RenderingServer.NotifyRenderMutation();
            if (!value.IsFinite()) throw new ArgumentException("Canvas modulation must be finite.", nameof(value));
            if (_canvas is { } canvas)
            {
                lock (canvas.ModulationGate)
                {
                    _color = value;
                    if (canvas.Modulators.Count != 0 && ReferenceEquals(canvas.Modulators[0], _registration)) canvas.Modulate = value;
                }
            }
            else _color = value;
        }
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        List<Exception>? errors = null;
        try
        {
            if (what is NotificationVisibilityChanged or NotificationWorldChanged || what == NotificationEnterCanvas && IsInsideCanvas || what == NotificationExitCanvas && !IsInsideCanvas)
                ReconcileCanvas();
        }
        catch (Exception error) { CollectException(ref errors, error); }
        try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Canvas modulation lifecycle callbacks failed.", errors);
    }

    private void ReconcileCanvas()
    {
        Tree?.EnsureProcessingAccess(this);
        var next = IsInsideCanvas && !IsDisposed && IsVisibleInTree ? RenderingCanvasRegistry.Resolve(GetCanvas()) : null;
        if (ReferenceEquals(next, _canvas)) return;
        if (_canvas is { } previous)
        {
            lock (previous.ModulationGate)
            {
                var index = previous.Modulators.IndexOf(_registration); previous.Modulators.Remove(_registration);
                if (index == 0) previous.RestoreSceneModulation();
            }
        }
        _canvas = next;
        if (next is not null)
        {
            lock (next.ModulationGate)
            {
                next.Modulators.Add(_registration);
                if (next.Modulators.Count == 1) next.Modulate = _color;
            }
        }
        if (!IsDisposed) UpdateConfigurationWarnings();
    }

    /// <inheritdoc />
    /// <remarks>Reports multiple visible modulators on the same canvas; only one controls its color.</remarks>
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        bool duplicate = false; if (_canvas is { } canvas) lock (canvas.ModulationGate) duplicate = canvas.Modulators.Count > 1;
        return duplicate
            ? [.. warnings, "Only one visible CanvasModulate controls a canvas. Other visible modulators wait for its removal or hiding."] : warnings;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ModulationProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(CanvasModulate) ? CreateModulator : base.CreateSceneInstanceFactory();
    private static Node CreateModulator() => new CanvasModulate();
    private static readonly PropertyDescriptor[] ModulationProperties =
    [new PropertyDescriptor<CanvasModulate, Color>(nameof(Color), node => node.Color, (node, value) => node.Color = value, _ => Colors.White, (_, value) => value.IsFinite(), stored: true)];
}
