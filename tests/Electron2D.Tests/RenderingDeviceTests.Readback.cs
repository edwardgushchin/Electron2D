using System.Buffers;
using System.Runtime.InteropServices;
using Electron2D;

internal static partial class RenderingDeviceTests
{
    private static void Readbacks(RenderingDevice device, RID pipeline, RID dataSet, RID parameters, RID buffer, RID uniform)
    {
        float[] input = Enumerable.Range(0, 1024).Select(i => (float)i).ToArray();
        float[] settings = [2, 1024, 1, 0];
        device.BufferUpdate(buffer, 0, 4096, MemoryMarshal.AsBytes(input.AsSpan()));
        void Compute()
        {
            device.BufferUpdate(uniform, 0, 16, MemoryMarshal.AsBytes(settings.AsSpan()));
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline);
            device.ComputeListBindUniformSet(list, dataSet, 3); device.ComputeListBindUniformSet(list, parameters, 2);
            device.ComputeListDispatch(list, 16, 1, 1); device.ComputeListEnd();
        }
        byte[]? first = null, second = null; var order = new List<int>(); var owner = Environment.CurrentManagedThreadId;
        Compute(); device.BufferGetDataAsync(buffer, data => { Check(Environment.CurrentManagedThreadId == owner, "Array callbacks use the device owner thread."); first = data; order.Add(1); });
        settings[0] = 3; settings[2] = -1; Compute();
        device.BufferGetDataAsync(buffer, data => { second = data; order.Add(2); });
        Check(order.Count == 0, "Recording asynchronous reads does not invoke callbacks.");
        device.Submit(); Check(order.Count == 0, "Submission does not invoke callbacks."); device.Sync();
        Check(order.SequenceEqual(new[] { 1, 2 }), "Array callbacks are delivered once in request order.");
        var firstFloats = MemoryMarshal.Cast<byte, float>(first.AsSpan()); var secondFloats = MemoryMarshal.Cast<byte, float>(second.AsSpan());
        for (var i = 0; i < 1024; i++) Check(firstFloats[i] == 2 * i + 1 && secondFloats[i] == 6 * i + 2, "Each asynchronous request preserves its point in the GPU compute timeline.");
        device.Sync(); Check(order.Count == 2, "Completed callbacks are not replayed.");

        byte[] small = new byte[4], large = new byte[4096]; var completed = 0;
        Action notify = () => completed++;
        void Cycle()
        {
            device.BufferGetDataAsync(buffer, small.AsMemory(), notify, 4);
            device.BufferGetDataAsync(buffer, large.AsMemory(), notify);
            device.Submit(); device.Sync();
        }
        // Prepare both concurrency and the largest range, including recycled entries in the opposite order.
        Cycle(); Cycle();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
        for (var i = 0; i < 16; i++) Cycle();
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++) Cycle();
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Prepared caller-memory asynchronous downloads allocate no owner-thread managed memory.");
        Check(BitConverter.ToSingle(small) == 8 && BitConverter.ToSingle(large, 4092) == 6140, "Caller-memory downloads honor their range after transfer pool reuse.");
        Check(firstFloats[1023] == 2047, "Retained array results survive transfer reuse and later source writes.");

        var uniformOutput = new byte[8]; var uniformCalls = 0;
        device.BufferGetDataAsync(uniform, uniformOutput.AsMemory(), () => uniformCalls++, 4);
        device.BufferClear(uniform, 0, 16);
        device.Sync(); Check(uniformCalls == 0, "Unsubmitted host snapshots remain deferred.");
        device.Submit(); Reject<InvalidOperationException>(device.Submit); device.Sync();
        Check(uniformCalls == 1 && BitConverter.ToSingle(uniformOutput) == 1024 && BitConverter.ToSingle(uniformOutput, 4) == -1, "Uniform reads capture request-time host bytes without a GPU fence.");
        var empty = 0; device.BufferGetDataAsync(buffer, data => { Check(data.Length == 0, "A read at buffer end has an empty result."); empty++; }, 4096);
        device.Submit(); device.Sync(); Check(empty == 1, "Empty requests still complete asynchronously.");

        var deferred = 0;
        device.BufferGetDataAsync(buffer, _ => deferred++); device.Submit();
        device.BufferUpdate(buffer, 0, 4, BitConverter.GetBytes(90f));
        Check(deferred == 0, "Automatic waits while recording new work do not reenter user callbacks.");
        var synchronous = device.BufferGetData(buffer, 0, 4);
        Check(deferred == 1 && BitConverter.ToSingle(synchronous) == 90, "Synchronous reads finish their own result before delivering old callbacks.");

        var nested = new List<int>();
        device.BufferGetDataAsync(buffer, _ =>
        {
            nested.Add(1);
            device.BufferGetDataAsync(buffer, _ => nested.Add(3), 0, 4);
            device.Submit(); device.Sync();
        }, 0, 4);
        device.BufferGetDataAsync(buffer, _ => nested.Add(2), 0, 4);
        device.Submit(); device.Sync();
        Check(nested.SequenceEqual(new[] { 1, 2, 3 }), "Nested submission/sync preserves callback FIFO without recursive delivery.");

        var afterError = 0; var marker = new InvalidOperationException("readback callback marker");
        device.BufferGetDataAsync(buffer, _ => throw marker, 0, 4);
        device.BufferGetDataAsync(buffer, _ => afterError++, 0, 4);
        device.Submit();
        try { device.Sync(); throw new Exception("Expected aggregated callback failure."); }
        catch (AggregateException error) { Check(error.InnerExceptions.Count == 1 && ReferenceEquals(error.InnerExceptions[0], marker), "Callback errors retain the original exception."); }
        device.Sync(); Check(afterError == 1, "Other callbacks finish despite a failing callback, without replay.");

        var nestedSource = device.StorageBufferCreate(16, BitConverter.GetBytes(123));
        var synchronousResult = new byte[4];
        device.BufferGetDataAsync(nestedSource, _ =>
        {
            device.BufferUpdate(nestedSource, 0, 4, BitConverter.GetBytes(999));
            Check(BitConverter.ToInt32(device.BufferGetData(nestedSource, 0, 4)) == 999, "A callback can perform another synchronous read.");
        }, 0, 4);
        device.BufferGetData(nestedSource, synchronousResult.AsSpan());
        Check(BitConverter.ToInt32(synchronousResult) == 123, "Callback readback cannot overwrite the outer synchronous read result.");
        var freed = 0;
        device.BufferGetDataAsync(nestedSource, bytes => { Check(BitConverter.ToInt32(bytes) == 999, "FreeRID preserves pending snapshots."); Reject<ArgumentException>(() => device.BufferGetData(nestedSource)); freed++; }, 0, 4);
        device.FreeRID(nestedSource); Check(freed == 1, "FreeRID delivers callbacks after removal.");

        var releasedInCallback = device.StorageBufferCreate(4, BitConverter.GetBytes(77)); var afterRelease = 0;
        device.BufferGetDataAsync(releasedInCallback, _ => device.FreeRID(releasedInCallback));
        device.BufferGetDataAsync(releasedInCallback, bytes => { Check(BitConverter.ToInt32(bytes) == 77, "Completed snapshots survive callback resource release."); afterRelease++; });
        device.Submit(); device.Sync(); Check(afterRelease == 1, "A callback can free its source while later callbacks still complete.");

        Reject<ArgumentNullException>(() => device.BufferGetDataAsync(buffer, (Action<byte[]>)null!));
        Reject<ArgumentNullException>(() => device.BufferGetDataAsync(buffer, small.AsMemory(), null!));
        Reject<ArgumentOutOfRangeException>(() => device.BufferGetDataAsync(buffer, _ => { }, uint.MaxValue));
        Reject<ArgumentOutOfRangeException>(() => device.BufferGetDataAsync(buffer, small.AsMemory(), notify, 4095));
        Reject<ArgumentException>(() => device.BufferGetDataAsync(pipeline, _ => { }));
        Reject<ArgumentException>(() => device.BufferGetDataAsync(nestedSource, _ => { }));
        Reject<InvalidOperationException>(() => Task.Run(() => device.BufferGetDataAsync(buffer, _ => { })).GetAwaiter().GetResult());
        using (var other = RenderingServer.CreateLocalRenderingDevice()) Reject<ArgumentException>(() => other.BufferGetDataAsync(buffer, _ => { }));
        device.ComputeListBegin(); Reject<InvalidOperationException>(() => device.BufferGetDataAsync(buffer, _ => { })); device.ComputeListEnd();
        CallerMemory();
        Cancellation();
    }

    private static void CallerMemory()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice(); var buffer = device.StorageBufferCreate(16, BitConverter.GetBytes(55));
        using var manager = new ReadbackMemory(); var memory = manager.Memory; var calls = 0; var guarded = 0;
        manager.OnAccess = () => { Reject<InvalidOperationException>(device.Dispose); Reject<InvalidOperationException>(device.Sync); guarded++; };
        device.BufferGetDataAsync(buffer, memory, () => calls++); device.Submit(); device.Sync();
        manager.OnAccess = null;
        Check(calls == 1 && guarded == 1 && BitConverter.ToInt32(memory.Span) == 55 && !device.IsDisposed, "Custom memory getters cannot dispose or recursively complete their device during a copy.");
        device.BufferGetDataAsync(buffer, memory, () => calls++); device.Submit(); ((IDisposable)manager).Dispose();
        Reject<ObjectDisposedException>(device.Sync);
        Check(calls == 1, "Invalidated caller memory never receives a successful completion callback.");
        device.Dispose();
    }
    private sealed class ReadbackMemory : MemoryManager<byte>
    {
        private readonly byte[] _bytes = new byte[16];
        private bool _disposed;
        internal Action? OnAccess;
        public override Span<byte> GetSpan() { ObjectDisposedException.ThrowIf(_disposed, this); OnAccess?.Invoke(); return _bytes; }
        public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();
        public override void Unpin() { }
        protected override void Dispose(bool disposing) => _disposed = true;
    }

    private static void Cancellation()
    {
        foreach (var submitted in new[] { false, true })
        {
            var device = RenderingServer.CreateLocalRenderingDevice(); var buffer = device.StorageBufferCreate(16); var calls = 0;
            device.BufferGetDataAsync(buffer, _ => calls++);
            if (submitted) device.Submit();
            device.Dispose(); Check(calls == 0, "Disposal cancels recorded and submitted callbacks.");
            Reject<ObjectDisposedException>(() => device.Sync());
        }
        var disposing = RenderingServer.CreateLocalRenderingDevice(); var source = disposing.StorageBufferCreate(16); var delivered = 0;
        disposing.BufferGetDataAsync(source, _ => { delivered++; disposing.Dispose(); });
        disposing.BufferGetDataAsync(source, _ => delivered++); disposing.Submit(); disposing.Sync();
        Check(delivered == 1 && disposing.IsDisposed, "Disposal during a callback cancels the rest of its completed batch.");
    }
}
