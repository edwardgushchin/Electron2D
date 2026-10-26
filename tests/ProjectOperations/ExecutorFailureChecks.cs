using Electron2D.Editor;

internal static class ExecutorFailureChecks
{
    internal static void Run(SessionProject project, SessionOperationsHost host)
    {
        var token = host.GrantClient("executor failure fixture");
        try
        {
            foreach (var executable in new[] { "/electron2d-missing-executor", "/bin/false" })
            {
                var executor = new CodexAgentExecutor(executable);
                try
                {
                    var refused = false;
                    try { executor.SubmitAsync("invalid before connect").GetAwaiter().GetResult(); }
                    catch (InvalidOperationException) { refused = true; }
                    if (!refused || executor.Status != SessionAgentStatus.Disconnected) throw new InvalidOperationException("Disconnected submission changed the executor lifecycle.");
                    var rejected = false;
                    try { executor.ConnectAsync(project.Root, "executor failure fixture", token).GetAwaiter().GetResult(); }
                    catch (Exception error) when (error is IOException or NotSupportedException) { rejected = true; }
                    if (!rejected || executor.IsConnected || executor.Status != SessionAgentStatus.Failed || !executor.DrainEvents().Any(e => e.Kind == SessionAgentEventKind.Failed)) throw new InvalidOperationException("Missing/unqualified executor produced no explicit failure.");
                }
                finally { executor.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
                try { executor.ConnectAsync(project.Root, "executor failure fixture", token).GetAwaiter().GetResult(); throw new InvalidOperationException("Disposed executor reconnected."); }
                catch (ObjectDisposedException) { }
                if (executor.Status != SessionAgentStatus.Disposed) throw new InvalidOperationException("Rejected reconnect changed permanent retirement state.");
            }
        }
        finally { host.RevokeClient(token); }
        Console.WriteLine("PASS 8 disconnected-submission/missing/unqualified executor, explicit failure and permanent-retirement contracts.");
    }
}
