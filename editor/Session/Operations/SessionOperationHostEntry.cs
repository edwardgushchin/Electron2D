namespace Electron2D.Editor;

internal static class SessionOperationHostEntry
{
    internal static int Run(string root, string sdk)
    {
        var project = SessionProject.Open(root);
        using var session = project.OpenSession(); using var host = session.StartOperations(project, sdk);
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler closing = (_, args) => { args.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += closing;
        var input = Task.Run(async () =>
        {
            var stream = Console.OpenStandardInput(); var buffer = new byte[256];
            while (await stream.ReadAsync(buffer, stop.Token).ConfigureAwait(false) != 0) { }
            stop.Cancel();
        });
        try
        {
            Console.WriteLine("Electron2D operations owner ready.");
            while (!stop.IsCancellationRequested) { host.Pump(); Thread.Sleep(5); }
            return 0;
        }
        finally
        {
            Console.CancelKeyPress -= closing; host.Dispose(); host.Completion.GetAwaiter().GetResult();
        }
    }
}
