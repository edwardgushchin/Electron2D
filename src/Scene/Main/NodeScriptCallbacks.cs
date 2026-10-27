namespace Electron2D;

/// <summary>Single-use typed callbacks installed on a node by its live Script binding.</summary>
/// <typeparam name="TNode">Compatible node receiver role.</typeparam>
/// <remarks>Create a fresh set for each owner/binding. Install only inside its synchronous registered Bind callback.
/// Callback targets may retain per-owner C# state; Retired releases that state once. Existing node callback switches,
/// processing groups, deltas, input stages and notification direction remain authoritative.</remarks>
public class NodeScriptCallbacks<TNode> where TNode : Node
{
    private int _issued;
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
    /// <summary>Receives numeric notifications and their requested reverse-direction flag.</summary>
    public Action<TNode, int, bool>? Notification { get; init; }
    /// <summary>Releases owned callback state/subscriptions once after all installed slots are cleared.</summary>
    /// <remarks>Runs after project Clear, including partial-bind cleanup and committed owner disposal.
    /// Failure propagates without restoring the retired slots. It does not run after a cancelled pre-delete.</remarks>
    public Action<TNode>? Retired { get; init; }

    internal void Issue()
    {
        if (Interlocked.CompareExchange(ref _issued, 1, 0) != 0) throw new InvalidOperationException("A script callback set can be installed only once.");
    }
    internal virtual void Populate(NodeScriptBinding target, TNode owner)
    {
        target.Retired = Retired is { } retired ? () => retired(owner) : null;
        target.EnterTree = EnterTree is { } enter ? () => enter(owner) : null;
        target.ExitTree = ExitTree is { } exit ? () => exit(owner) : null;
        target.Ready = Ready is { } ready ? () => ready(owner) : null;
        target.Process = Process is { } process ? delta => process(owner, delta) : null;
        target.PhysicsProcess = PhysicsProcess is { } physics ? delta => physics(owner, delta) : null;
        target.Input = Input is { } input ? value => input(owner, value) : null;
        target.ShortcutInput = ShortcutInput is { } shortcut ? value => shortcut(owner, value) : null;
        target.UnhandledKeyInput = UnhandledKeyInput is { } key ? value => key(owner, value) : null;
        target.UnhandledInput = UnhandledInput is { } unhandled ? value => unhandled(owner, value) : null;
        target.Notification = Notification is { } notification ? (what, reversed) => notification(owner, what, reversed) : null;
    }
}

internal sealed class NodeScriptBinding
{
    internal Action? EnterTree, ExitTree, Ready, Retired, MouseEnter, MouseExit, Draw;
    internal Action<double>? Process, PhysicsProcess;
    internal Action<InputEvent>? Input, ShortcutInput, UnhandledInput;
    internal Action<InputEventKey>? UnhandledKeyInput;
    internal Action<int, bool>? Notification;
    internal Action<Viewport, InputEvent, int>? InputEvent;
    internal Action<int>? MouseShapeEnter, MouseShapeExit;
    internal Action<PhysicsDirectBodyState>? IntegrateForces;
}
