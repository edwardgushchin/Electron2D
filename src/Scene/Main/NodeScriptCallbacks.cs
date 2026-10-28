namespace Electron2D;

/// <summary>Single-use typed callbacks installed on a node by its live Script binding.</summary>
/// <typeparam name="TNode">Compatible node receiver role.</typeparam>
/// <remarks>Create a fresh set for each owner/binding. Install only inside its synchronous registered Bind callback.
/// Callback targets may retain per-owner C# state; Retired releases that state once. Existing node callback switches,
/// processing groups, deltas, input stages and notification direction remain authoritative.</remarks>
public class NodeScriptCallbacks<TNode> : ElectronObjectScriptCallbacks<TNode> where TNode : Node
{
    /// <summary>Creates an unused callback set.</summary>
    public NodeScriptCallbacks() { }
    /// <summary>Runs at the ordinary tree-entry callback point.</summary>
    public Action<TNode>? EnterTree { get; init; }
    /// <summary>Runs at the ordinary tree-exit callback point.</summary>
    public Action<TNode>? ExitTree { get; init; }
    /// <summary>Runs after readiness enables the declared frame/input callbacks.</summary>
    public Action<TNode>? Ready { get; init; }
    /// <summary>Runs on eligible process frames with the current process delta.</summary>
    public Action<TNode, double>? Process { get; init; }
    /// <summary>Runs on eligible physics frames before the numerical step.</summary>
    public Action<TNode, double>? PhysicsProcess { get; init; }
    /// <summary>Receives the first ordinary input stage.</summary>
    public Action<TNode, InputEvent>? Input { get; init; }
    /// <summary>Receives the shortcut input stage.</summary>
    public Action<TNode, InputEvent>? ShortcutInput { get; init; }
    /// <summary>Receives the unhandled key-input stage.</summary>
    public Action<TNode, InputEventKey>? UnhandledKeyInput { get; init; }
    /// <summary>Receives the final ordinary unhandled input stage.</summary>
    public Action<TNode, InputEvent>? UnhandledInput { get; init; }
    /// <summary>Returns script configuration warnings when tooling explicitly queries this node.</summary>
    /// <remarks>Warnings join the existing native warning chain. Queries are uncached cold operations.</remarks>
    public Func<TNode, string[]>? GetConfigurationWarnings { get; init; }
    /// <summary>Returns accessibility authoring warnings through the native accessibility warning hook.</summary>
    public Func<TNode, string[]>? GetAccessibilityConfigurationWarnings { get; init; }
    /// <summary>Selects a borrowed semantic RID through the ordinary native focus hook.</summary>
    /// <remarks>The accessibility publisher retains live-element and same-window validation. CLR overrides retain control of base calls.</remarks>
    public Func<TNode, RID>? GetFocusedAccessibilityElement { get; init; }
    internal override ObjectScriptBinding CreateBinding() => new NodeScriptBinding();
    internal override void Populate(ObjectScriptBinding binding, TNode owner)
    {
        base.Populate(binding, owner);
        var target = (NodeScriptBinding)binding;
        target.EnterTree = EnterTree is { } enter ? () => enter(owner) : null;
        target.ExitTree = ExitTree is { } exit ? () => exit(owner) : null;
        target.Ready = Ready is { } ready ? () => ready(owner) : null;
        target.Process = Process is { } process ? delta => process(owner, delta) : null;
        target.PhysicsProcess = PhysicsProcess is { } physics ? delta => physics(owner, delta) : null;
        target.Input = Input is { } input ? value => input(owner, value) : null;
        target.ShortcutInput = ShortcutInput is { } shortcut ? value => shortcut(owner, value) : null;
        target.UnhandledKeyInput = UnhandledKeyInput is { } key ? value => key(owner, value) : null;
        target.UnhandledInput = UnhandledInput is { } unhandled ? value => unhandled(owner, value) : null;
        target.GetConfigurationWarnings = GetConfigurationWarnings is { } warnings ? () => warnings(owner) : null;
        target.GetAccessibilityConfigurationWarnings = GetAccessibilityConfigurationWarnings is { } accessibility ? () => accessibility(owner) : null;
        target.GetFocusedAccessibilityElement = GetFocusedAccessibilityElement is { } focused ? () => focused(owner) : null;
    }
}

internal sealed class NodeScriptBinding : ObjectScriptBinding
{
    internal Func<string[]>? GetConfigurationWarnings, GetAccessibilityConfigurationWarnings;
    internal Func<RID>? GetFocusedAccessibilityElement;
    internal Action? EnterTree, ExitTree, Ready, MouseEnter, MouseExit, Draw;
    internal Action<double>? Process, PhysicsProcess;
    internal Action<InputEvent>? Input, ShortcutInput, UnhandledInput;
    internal Action<InputEventKey>? UnhandledKeyInput;
    internal Action<Viewport, InputEvent, int>? InputEvent;
    internal Action<int>? MouseShapeEnter, MouseShapeExit;
    internal Action<PhysicsDirectBodyState>? IntegrateForces;
}
