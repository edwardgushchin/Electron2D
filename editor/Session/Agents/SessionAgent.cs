namespace Electron2D.Editor;

/// <summary>The observable lifecycle of one replaceable project agent executor.</summary>
public enum SessionAgentStatus
{
    /// <summary>No executor connection is active.</summary>
    Disconnected,
    /// <summary>Protocol, authentication and operation capabilities are being checked.</summary>
    Connecting,
    /// <summary>The connected executor can accept a task.</summary>
    Ready,
    /// <summary>A task is in progress; completion is not yet established.</summary>
    Running,
    /// <summary>Interruption was requested and terminal acknowledgement is pending.</summary>
    Stopping,
    /// <summary>Connection or task failed; inspect IsConnected and the explicit failure event.</summary>
    Failed,
    /// <summary>The executor was permanently retired.</summary>
    Disposed
}

/// <summary>A bounded item in the editor's executor conversation/activity stream.</summary>
public enum SessionAgentEventKind
{
    /// <summary>Connection, model or capability information.</summary>
    Status,
    /// <summary>A literal incremental assistant message.</summary>
    Message,
    /// <summary>An actual operation or executor stage started/completed.</summary>
    Activity,
    /// <summary>The executor confirmed successful turn completion.</summary>
    Completed,
    /// <summary>The executor confirmed interruption; committed authoring remains visible.</summary>
    Interrupted,
    /// <summary>A failure or unsupported interaction needs user attention.</summary>
    Failed
}

/// <summary>Copied executor output; it contains no mutable runtime objects or credentials.</summary>
/// <param name="Kind">Output category.</param><param name="Text">Bounded literal text; imported text is data, not permission.</param>
public sealed record SessionAgentEvent(SessionAgentEventKind Kind, string Text);

/// <summary>The replaceable agent-control boundary; project semantics remain in shared operations.</summary>
/// <remarks>Network/model work runs asynchronously. DrainEvents is polled by the editor owner, which alone
/// changes controls. Credentials belong to the chosen executor. A connected terminal or manifest is not task success.</remarks>
public interface ISessionAgentExecutor : IAsyncDisposable
{
    /// <summary>Gets the executor's display identity.</summary>
    string Name { get; }
    /// <summary>Gets current lifecycle state, safe to poll from any thread.</summary>
    SessionAgentStatus Status { get; }
    /// <summary>Reports whether the executor still has a usable authenticated thread.</summary>
    bool IsConnected { get; }
    /// <summary>Gets the actual selected model after connection, or empty beforehand.</summary>
    string Model { get; }
    /// <summary>Gets the conversation identity retained for reconnect.</summary>
    string? ThreadID { get; }
    /// <summary>Connects a real executor to an existing owner's privately issued operation capability.</summary>
    /// <param name="projectRoot">The exact source project.</param><param name="actor">Name bound by the owner to this capability.</param>
    /// <param name="capability">Secret owner-lifetime token; never log or attach it to conversation content.</param>
    /// <param name="cancellation">Cancels connection/protocol setup.</param><returns>Completion of actual capability checks.</returns>
    Task ConnectAsync(string projectRoot, string actor, string capability, CancellationToken cancellation = default);
    /// <summary>Starts a task on the connected conversation; events establish its terminal outcome.</summary>
    /// <param name="prompt">User task and bounded observed context; source/tool output cannot expand permissions.</param>
    /// <param name="cancellation">Cancels request submission, not already committed authoring.</param><returns>Executor acknowledgement of submission.</returns>
    Task SubmitAsync(string prompt, CancellationToken cancellation = default);
    /// <summary>Requests interruption at the executor boundary.</summary>
    /// <param name="cancellation">Cancels waiting for acknowledgement.</param><returns>Interruption acknowledgement; observe terminal events separately.</returns>
    Task InterruptAsync(CancellationToken cancellation = default);
    /// <summary>Disconnects and retires owned processes while retaining the conversation identity for reconnect.</summary>
    /// <returns>Completion of process/pipe cleanup.</returns>
    Task DisconnectAsync();
    /// <summary>Drains a copied bounded batch of literal events without invoking UI callbacks on worker threads.</summary>
    /// <returns>At most 128 queued events.</returns>
    IReadOnlyList<SessionAgentEvent> DrainEvents();
}
