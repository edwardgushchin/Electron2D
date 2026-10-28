namespace Electron2D;

/// <summary>Adds control queries, routed input and typed dragging to a live Script callback set.</summary>
/// <typeparam name="TControl">Compatible control receiver.</typeparam>
/// <remarks>Install a fresh set in the owner's synchronous Bind. Queries replace native query hooks when supplied;
/// null or false results do not select the native hook. Drag forwarding retains precedence. GUI input runs after
/// the signal and before the native hook; accepting the event or detaching the control stops later delivery.
/// Native validation, borrowed input/payload lifetime and tooltip ownership remain authoritative.</remarks>
public class ControlScriptCallbacks<TControl> : CanvasItemScriptCallbacks<TControl> where TControl : Control
{
    /// <summary>Creates an unused control callback set.</summary>
    public ControlScriptCallbacks() { }
    /// <summary>Supplies the intrinsic minimum size; components must be finite.</summary>
    public Func<TControl, Vector2>? GetMinimumSize { get; init; }
    /// <summary>Supplies the intrinsic maximum size; finite negative components mean unbounded.</summary>
    public Func<TControl, Vector2>? GetMaximumSize { get; init; }
    /// <summary>Tests a control-local point, including native button press/release checks.</summary>
    public Func<TControl, Vector2, bool>? HasPoint { get; init; }
    /// <summary>Selects a defined cursor shape for a control-local position.</summary>
    public Func<TControl, Vector2, CursorShape>? GetCursorShape { get; init; }
    /// <summary>Supplies untranslated tooltip text; empty text permits eligible parent lookup.</summary>
    public Func<TControl, Vector2, string>? GetTooltip { get; init; }
    /// <summary>Selects the default tooltip label's translation policy at a local position.</summary>
    public Func<TControl, Vector2, NodeAutoTranslateMode>? GetTooltipAutoTranslateModeAt { get; init; }
    /// <summary>Creates a detached custom tooltip transferred to the presenter; null selects the ordinary tooltip.</summary>
    public Func<TControl, string, Control?>? MakeCustomTooltip { get; init; }
    /// <summary>Supplies validated Unicode-scalar ranges when a text consumer selects its custom parser.</summary>
    public Func<TControl, IReadOnlyList<string>, string, TextBIDIRange[]>? StructuredTextParser { get; init; }
    /// <summary>Receives a borrowed local event after GUIInput subscribers and before native input handling.</summary>
    /// <remarks>Internal pointer-refresh events omit this callback and the signal. Signal blocking omits only the signal.</remarks>
    public Action<TControl, InputEvent>? GUIInput { get; init; }
    /// <summary>Produces a borrowed typed drag payload; accessible requests use positive-infinite coordinates.</summary>
    public Func<TControl, Vector2, DragPayload?>? GetDragData { get; init; }
    /// <summary>Tests a borrowed typed drop payload at a local or accessible position.</summary>
    public Func<TControl, Vector2, DragPayload, bool>? CanDropData { get; init; }
    /// <summary>Consumes an accepted borrowed typed drop payload.</summary>
    public Action<TControl, Vector2, DragPayload>? DropData { get; init; }
    /// <summary>Supplies non-null contextual accessibility help on the scene owner.</summary>
    public Func<TControl, string>? AccessibilityGetContextualInfo { get; init; }
    /// <summary>Describes the supplied immediate child's position in this accessible container.</summary>
    public Func<TControl, Node, string>? GetAccessibilityContainerName { get; init; }
    /// <summary>Receives committed text after the native text hook and before TextInput subscribers.</summary>
    public Action<TControl, string>? TextInput { get; init; }
    /// <summary>Receives preedit text and its Unicode-codepoint selection before composition subscribers.</summary>
    public Action<TControl, string, Vector2i>? IMECompositionChanged { get; init; }

    internal override ObjectScriptBinding CreateBinding() => new ControlScriptBinding();
    internal override void Populate(ObjectScriptBinding binding, TControl owner)
    {
        base.Populate(binding, owner);
        var target = (ControlScriptBinding)binding;
        target.GetMinimumSize = GetMinimumSize is { } minimum ? () => minimum(owner) : null;
        target.GetMaximumSize = GetMaximumSize is { } maximum ? () => maximum(owner) : null;
        target.HasPoint = HasPoint is { } point ? position => point(owner, position) : null;
        target.GetCursorShape = GetCursorShape is { } cursor ? position => cursor(owner, position) : null;
        target.GetTooltip = GetTooltip is { } tooltip ? position => tooltip(owner, position) : null;
        target.GetTooltipAutoTranslateModeAt = GetTooltipAutoTranslateModeAt is { } translate ? position => translate(owner, position) : null;
        target.MakeCustomTooltip = MakeCustomTooltip is { } custom ? text => custom(owner, text) : null;
        target.StructuredTextParser = StructuredTextParser is { } parser ? (options, text) => parser(owner, options, text) : null;
        target.GUIInput = GUIInput is { } input ? value => input(owner, value) : null;
        target.GetDragData = GetDragData is { } drag ? position => drag(owner, position) : null;
        target.CanDropData = CanDropData is { } canDrop ? (position, payload) => canDrop(owner, position, payload) : null;
        target.DropData = DropData is { } drop ? (position, payload) => drop(owner, position, payload) : null;
        target.AccessibilityGetContextualInfo = AccessibilityGetContextualInfo is { } context ? () => context(owner) : null;
        target.GetAccessibilityContainerName = GetAccessibilityContainerName is { } container ? child => container(owner, child) : null;
        target.TextInput = TextInput is { } textInput ? text => textInput(owner, text) : null;
        target.IMECompositionChanged = IMECompositionChanged is { } ime ? (text, selection) => ime(owner, text, selection) : null;
    }
}

internal class ControlScriptBinding : NodeScriptBinding
{
    internal Func<Vector2>? GetMinimumSize, GetMaximumSize;
    internal Func<Vector2, bool>? HasPoint;
    internal Func<Vector2, CursorShape>? GetCursorShape;
    internal Func<Vector2, string>? GetTooltip;
    internal Func<Vector2, NodeAutoTranslateMode>? GetTooltipAutoTranslateModeAt;
    internal Func<string, Control?>? MakeCustomTooltip;
    internal Func<IReadOnlyList<string>, string, TextBIDIRange[]>? StructuredTextParser;
    internal Action<InputEvent>? GUIInput;
    internal Func<Vector2, DragPayload?>? GetDragData;
    internal Func<Vector2, DragPayload, bool>? CanDropData;
    internal Action<Vector2, DragPayload>? DropData;
    internal Func<string>? AccessibilityGetContextualInfo;
    internal Func<Node, string>? GetAccessibilityContainerName;
    internal Action<string>? TextInput;
    internal Action<string, Vector2i>? IMECompositionChanged;
}
