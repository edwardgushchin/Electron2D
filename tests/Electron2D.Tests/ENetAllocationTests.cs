using Electron2D;
using System.Runtime.CompilerServices;

internal static class ENetAllocationTests
{
    internal static void Run()
    {
        FileBlocks();
        foreach (var mode in Enum.GetValues<ENetCompressionMode>()) Exchange(mode);
        var before = NativeAllocation.Snapshot().Current;
        Task.WaitAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 32; i++)
            {
                using var host = new ENetConnection(); host.CreateHostBound("127.0.0.1", 0, 2, 2);
                host.Compress(ENetCompressionMode.ZSTD); host.Compress(ENetCompressionMode.ZLIB);
            }
        })).ToArray());
        Check(NativeAllocation.Snapshot().Current == before, "Concurrent native hosts/codecs retire every owned payload");
        var abandoned = Abandon();
        for (var i = 0; i < 8 && (abandoned.IsAlive || NativeAllocation.Snapshot().Current != before); i++)
        { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Check(!abandoned.IsAlive && NativeAllocation.Snapshot().Current == before, "SafeHandle finalization retires abandoned native host/codec state");
        Console.WriteLine("ENet allocation accounting: file transient peak, all wire codecs, concurrent hosts, finalizer and zero-allocation service passed.");
    }
    private static void FileBlocks()
    {
        var data = new byte[65536]; Array.Fill(data, (byte)37);
        var before = NativeAllocation.Snapshot();
        foreach (var mode in new[] { FileCompressionMode.FastLz, FileCompressionMode.Zstandard })
        {
            var encoded = ByteBuffer.Compress(data, mode);
            Check(ByteBuffer.Decompress(encoded, data.Length, mode).AsSpan().SequenceEqual(data), "Tracked native file blocks round trip");
            Check(NativeAllocation.Snapshot().Current == before.Current, "File codec internal native storage retires before returning");
        }
        Check(NativeAllocation.Snapshot().Peak > before.Peak, "Transient file codec storage contributes without an intervening snapshot");
    }
    private static void Exchange(ENetCompressionMode mode)
    {
        var before = NativeAllocation.Snapshot().Current;
        using (var server = new ENetConnection())
        using (var client = new ENetConnection())
        {
            server.CreateHostBound("127.0.0.1", 0, 2, 2); client.CreateHost(1, 2);
            Check(NativeAllocation.Snapshot().Current > before, "Real native hosts/peer arrays contribute to owned bytes");
            var hosts = NativeAllocation.Snapshot().Current;
            server.Compress(mode); client.Compress(mode);
            Check(mode == ENetCompressionMode.None || NativeAllocation.Snapshot().Current > hosts, "Native compression state contributes before traffic");
            var peer = client.ConnectToHost("127.0.0.1", server.GetLocalPort(), 2); ENetPacketPeer? receiver = null; var connected = false;
            void Poll()
            {
                var received = server.Service(); if (received.Type == ENetEventType.Connect) receiver = received.Peer;
                if (client.Service().Type == ENetEventType.Connect) connected = true;
            }
            Wait(Poll, () => connected && receiver is not null);
            var data = new byte[20000]; for (var i = 0; i < data.Length; i++) data[i] = (byte)(i % 16);
            peer.Send(1, data, ENetPacketFlags.Reliable);
            Wait(Poll, () => receiver!.GetAvailablePacketCount() != 0);
            var target = new byte[data.Length];
            Check(receiver!.GetPacket(target) == data.Length && target.AsSpan().SequenceEqual(data), "Tracked native packets/codecs retain fragmented wire data");
            for (var i = 0; i < 128; i++) Poll();
            AllocationWarmupCheck.Collect();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) { Poll(); _ = NativeAllocation.Snapshot(); }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"{mode}: prepared service/accounting allocates {owner}/{all} managed bytes");
            Console.WriteLine($"ENet {mode}: live native={NativeAllocation.Snapshot().Current - before} B; warmed owner/all={owner}/{all} B.");
        }
        Check(NativeAllocation.Snapshot().Current == before, "Destroy retires native hosts, peers, packets and codec storage");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Abandon()
    {
        var host = new ENetConnection(); host.CreateHostBound("127.0.0.1", 0, 2, 2); host.Compress(ENetCompressionMode.ZSTD);
        return new WeakReference(host);
    }
    private static void Wait(Action poll, Func<bool> ready)
    {
        var end = Environment.TickCount64 + 10000;
        while (!ready()) { poll(); if (Environment.TickCount64 > end) throw new TimeoutException("Tracked ENet exchange timed out."); }
    }
    private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
}
