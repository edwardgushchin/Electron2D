using Electron2D;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;

internal static unsafe partial class NativeUTF8StringTests
{
    private const string Library = "libElectron2D.so";
    [LibraryImport(Library, EntryPoint = "e2d_test_utf8", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(NativeUTF8StringMarshaller))]
    private static partial int Read(string? a, int lengthA, string? b, int lengthB, ulong current, out ulong hashA, out ulong hashB);
    [LibraryImport(Library, EntryPoint = "e2d_test_utf8", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int Legacy(string? a, int lengthA, string? b, int lengthB, ulong current, out ulong hashA, out ulong hashB);
    [LibraryImport(Library, EntryPoint = "e2d_test_utf8")]
    private static partial int Raw(byte* a, int lengthA, byte* b, int lengthB, ulong current, out ulong hashA, out ulong hashB);
    [LibraryImport(Library, EntryPoint = "e2d_test_fail_after")] private static partial void FailAfter(uint count);
    [LibraryImport(Library, EntryPoint = "e2d_test_calls")] private static partial ulong Calls();
    [LibraryImport(Library, EntryPoint = "e2d_test_errno")] private static partial int ErrorCode();
    [LibraryImport(Library, EntryPoint = "e2d_test_error", SetLastError = true, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(NativeUTF8StringMarshaller))]
    private static partial int Error(string value);
    [LibraryImport(Library, EntryPoint = "e2d_test_missing_utf8", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(NativeUTF8StringMarshaller))]
    private static partial int Missing(string value);

    internal static void Run()
    {
        var baseline = NativeAllocation.Snapshot().Current;
        string?[] values = [null, "", "ascii", new('a', 255), new('b', 256), "Кириллица 😀", "a\0b", "\ud800", new('Я', 4000)];
        foreach (var value in values)
        {
            var length = value is null ? -1 : Encoding.UTF8.GetByteCount(value);
            var owned = length + 1 > 256 ? length + 1 : 0;
            Check(Read(value, length, null, -1, checked((ulong)(baseline + owned)), out var hash, out var none) == 1 && hash == Hash(value) && none == 0,
                "The native call sees exact UTF8 bytes, null/empty/embedded-null/surrogate behavior, a terminator and live heap ownership.");
            Check(NativeAllocation.Snapshot().Current == baseline, "Generated return retires its argument storage.");
            var pointer = NativeUTF8StringMarshaller.Allocate(value);
            try
            {
                Check(Raw(pointer, length, null, -1, checked((ulong)(baseline + (value is null ? 0 : length + 1))), out hash, out none) == 1 && hash == Hash(value),
                    "Explicit pointer arrays use the same encoding and exact owned byte count.");
            }
            finally { NativeAllocation.Free(pointer); }
        }
        var large = new string('z', 8192); var bytes = large.Length + 1;
        Check(Legacy(large, large.Length, null, -1, checked((ulong)(baseline + bytes)), out _, out _) == -1,
            "The preceding SDK marshalling path is a negative control: its real native buffer is absent from the ledger.");
        var prior = NativeAllocation.Snapshot();
        var unseen = new string('x', checked((int)(prior.Peak - baseline + 4096)));
        Check(Read(unseen, unseen.Length, null, -1, ulong.MaxValue, out _, out _) == 1, "The transient call deliberately takes no native or managed memory snapshot.");
        Check(NativeAllocation.Snapshot() == (baseline, baseline + unseen.Length + 1L), "The true transient high-water survives automatic cleanup.");
        for (uint failure = 1; failure <= 2; failure++)
        {
            var calls = Calls(); FailAfter(failure); var rejected = false;
            try { _ = Read(large, large.Length, large, large.Length, ulong.MaxValue, out _, out _); }
            catch (OutOfMemoryException) { rejected = true; }
            finally { FailAfter(0); }
            Check(rejected && Calls() == calls && NativeAllocation.Snapshot().Current == baseline,
                "Failure of either argument releases prior arguments and never enters native code.");
            Check(Read(large, large.Length, large, large.Length, checked((ulong)(baseline + 2 * bytes)), out _, out _) == 1, "A failed argument preparation permits healthy retry.");
        }
        var code = ErrorCode();
        Check(Error(large) == -1 && Marshal.GetLastPInvokeError() == code && NativeAllocation.Snapshot().Current == baseline,
            "Owned cleanup preserves the native call's captured error code.");
        var missing = false;
        try { _ = Missing(large); } catch (EntryPointNotFoundException) { missing = true; }
        Check(missing && NativeAllocation.Snapshot().Current == baseline, "Failed entry-point binding does not retain marshalled storage.");
        Parallel.For(0, 4, worker => { for (var i = 0; i < 128; i++) Check(Read(large, large.Length, null, -1, ulong.MaxValue, out _, out _) == 1, "Concurrent arguments remain valid."); });
        Check(NativeAllocation.Snapshot().Current == baseline, "All four workers retire their argument storage.");
        for (var i = 0; i < 256; i++) Cycle();
        AllocationWarmupCheck.Collect();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 1024; i++) Cycle();
        Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all,
            "1024 warmed short/heap argument pairs allocate zero owner/all-thread managed bytes.");
        Check(NativeAllocation.Snapshot().Current == baseline, "Prepared argument cycles drain completely.");
        Console.WriteLine("UTF8 native input storage: exact bytes/peak, 255/256 boundary, two refusal positions/retry, errno/missing-entry cleanup, four workers and 1024 zero-managed-byte cycles passed.");
        void Cycle()
        {
            Check(Read("short", 5, large, large.Length, checked((ulong)(baseline + bytes)), out _, out _) == 1, "Prepared native arguments are live.");
        }
    }
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_peer")]
    private static extern ref WebRTCPeerHandle? Peer(WebRTCPeerConnection owner);
    internal static void RunCallers()
    {
        var large = new string('Я', 4000);
        var message = LinuxPortalDBus.NewMethodCall("org.Electron2D.Memory", "/memory", "org.Electron2D.Memory", "Check");
        Check(message != 0, "The independent DBus message is created without a bus session.");
        try
        {
            LinuxPortalDBus.InitAppend(message, out var append); LinuxPortalDBus.AppendString(ref append, "seed");
            var baseline = NativeAllocation.Snapshot().Current; var rejected = false; FailAfter(1);
            try { LinuxPortalDBus.AppendString(ref append, large); }
            catch (OutOfMemoryException) { rejected = true; }
            finally { FailAfter(0); }
            Check(rejected && NativeAllocation.Snapshot().Current == baseline, "Refused DBus argument storage leaves the message and ledger intact.");
            LinuxPortalDBus.AppendString(ref append, large);
            Check(LinuxPortalDBus.Init(message, out var read) != 0 && LinuxPortalDBus.ReadString(ref read) == "seed" &&
                LinuxPortalDBus.Next(ref read) != 0 && LinuxPortalDBus.ReadString(ref read) == large && LinuxPortalDBus.Next(ref read) == 0 &&
                NativeAllocation.Snapshot().Current == baseline, "A retry appends one exact copied UTF8 value without retaining temporary ownership.");
        }
        finally { LinuxPortalDBus.MessageUnref(message); }
        using var peer = WebRTCPeerConnection.Create(); peer.Initialize();
        using var channel = peer.CreateDataChannel("retained");
        var offered = false; peer.SessionDescriptionCreated += (type, description) => offered = true;
        peer.CreateOffer(); peer.Poll();
        Check(offered, "A real offer waits for the asynchronous native certificate before ownership comparisons.");
        var previous = Peer(peer); Check(previous is not null && !previous.IsClosed, "An existing WebRTC native cohort is active.");
        var configuration = new WebRTCConfiguration { ICEServers = [new WebRTCICEServer { URLs = ["stun:127.0.0.1:3478"], Username = large, Credential = large }] };
        var before = NativeAllocation.Snapshot().Current;
        for (uint failure = 1; failure <= 3; failure++)
        {
            FailAfter(failure); var rejected = false;
            try { peer.Initialize(configuration); }
            catch (OutOfMemoryException) { rejected = true; }
            finally { FailAfter(0); }
            Check(rejected && ReferenceEquals(Peer(peer), previous) && !previous!.IsClosed && NativeAllocation.Snapshot().Current == before,
                $"WebRTC refusal position={failure}, rejected={rejected}, same={ReferenceEquals(Peer(peer), previous)}, closed={previous!.IsClosed}, native={before}/{NativeAllocation.Snapshot().Current}.");
            _ = peer.GetConnectionState();
        }
        peer.Initialize(configuration);
        Check(!ReferenceEquals(Peer(peer), previous) && previous!.IsClosed, "Healthy retry publishes its new cohort and retires the preceding owner.");
        Console.WriteLine("UTF8 native callers: real DBus copy/refusal/retry and all three WebRTC credential refusals preserve native owner identity and drain temporary storage.");
    }
    private static ulong Hash(string? value)
    {
        if (value is null) return 0;
        var hash = 14695981039346656037UL;
        foreach (var item in Encoding.UTF8.GetBytes(value)) hash = unchecked((hash ^ item) * 1099511628211UL);
        return hash;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
