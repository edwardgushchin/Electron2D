using Electron2D.Tools;

CustomNodes.Register();
if (args.SequenceEqual(new[] { "--entry-check" }))
{
    await Reject<ArgumentNullException>(() => ShaderImportApplication.RunAsync(null!));
    await Reject<ArgumentException>(() => ShaderImportApplication.RunAsync([null!]));
    if (await ShaderImportApplication.RunAsync([]) != 2) throw new InvalidOperationException("Empty command shape was accepted.");
    Console.WriteLine("Compiled import entry argument guards passed."); return 0;
}
if (args.Length > 0 && args[0] == "--concurrency-check")
{
    args = args[1..]; using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
    CustomNodes.SourceHook = () => { entered.Set(); if (!release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException("Source guard fixture timed out."); };
    var command = args; var running = Task.Run(() => ShaderImportApplication.RunAsync(command));
    try
    {
        if (!entered.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException("Source callback did not start.");
        await Reject<InvalidOperationException>(() => ShaderImportApplication.RunAsync([]));
    }
    finally { release.Set(); }
    return await running;
}
var reentered = false;
if (args.Length > 0 && args[0] == "--reentry-check")
{
    args = args[1..];
    CustomNodes.SourceHook = () =>
    {
        try { ShaderImportApplication.RunAsync([]).GetAwaiter().GetResult(); }
        catch (InvalidOperationException) { reentered = true; }
        if (!reentered) throw new InvalidOperationException("Reentrant import was accepted.");
    };
}
var result = await ShaderImportApplication.RunAsync(args);
if (CustomNodes.SourceHook is not null && !reentered) throw new InvalidOperationException("The reentry fixture did not execute.");
Console.WriteLine("Custom callback trace: " + string.Join(",", CustomNodes.Calls));
return result;

static async Task Reject<T>(Func<Task<int>> action) where T : Exception
{
    try { await action(); } catch (T) { return; }
    throw new InvalidOperationException("Expected " + typeof(T).Name);
}
