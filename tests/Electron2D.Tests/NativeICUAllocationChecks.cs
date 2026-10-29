using Electron2D;

internal static class NativeICUAllocationChecks
{
    internal static void Run(Func<(long Current, long Peak)> read, bool fresh = false)
    {
        var server = TextServerManager.GetPrimaryInterface();
        var before = read();
        var dataLength = server.GetSupportData().Length;
        Check(server.LoadSupportData(string.Empty), "Embedded data initialization failed.");
        var initialized = read();
        var contribution = initialized.Current - before.Current;
        if (fresh) Check(contribution > dataLength, $"ICU internal allocation was omitted: {contribution} bytes for {dataLength} data bytes.");
        void Exercise()
        {
            Check(server.StringGetWordBreaks("中华人民共和国", "zh").Length > 0, "Dictionary word traversal failed.");
            Check(server.StringGetCharacterBreaks("A😀e\u0301", "en").SequenceEqual(new[] { 1, 2, 4 }), "Scalar character boundaries changed.");
            Check(server.StringToLower("Iİ", "tr") == "ıi", "Accounted casing changed.");
            Check(server.StringToTitle("ijssel", "nl") == "IJssel", "Accounted titlecase changed.");
            _ = server.SpoofCheck("ordinary");
            Check(server.IsValidLetter('A') && server.IsValidIdentifier("hello_17"), "Unicode value preparation failed.");
        }
        for (var i = 0; i < 8; i++) Exercise();
        var retained = read();
        for (var i = 0; i < 64; i++) Exercise();
        Check(read().Current == retained.Current, "Repeated temporary ICU operations leaked native payload.");
        void Values()
        {
            Check(server.IsValidLetter('A') && server.IsValidIdentifier("hello_17"), "Prepared Unicode value queries failed.");
            _ = read();
        }
        for (var i = 0; i < 256; i++) Values();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        for (var i = 0; i < 256; i++) Values();
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 20000; i++) Values();
        var ownBytes = GC.GetAllocatedBytesForCurrentThread() - own; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownBytes == 0 && allBytes == 0, $"Prepared ICU value/read cycles allocated {ownBytes}/{allBytes} managed bytes.");
        Check(read().Current == retained.Current && read().Peak >= retained.Peak, "Prepared queries changed ownership or lost its high-water mark.");
        Parallel.For(0, 4, _ => { for (var i = 0; i < 16; i++) Exercise(); });
        Check(read().Current == retained.Current, "Concurrent public text calls leaked native payload.");
        Console.WriteLine($"ICU allocation: init={contribution}, data={dataLength}, retained={retained.Current}, peak={read().Peak}, prepared=20000, managed={ownBytes}/{allBytes}; public dictionaries/casing/security/concurrency passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
