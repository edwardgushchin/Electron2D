using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace Electron2D;

public abstract partial class Font
{
    private ConcurrentQueue<Font>? _retiredSources;
    private int _drainingSources;
    private int _retireProviderLayouts;
    [ThreadStatic] internal static int ActiveFontReads;
    [ThreadStatic] private static List<Font>? _pendingSourceOwners;

    private void DeferSourceRetirements()
    {
        if (FontData.NativeCleanupDepth != 0) return;
        var pending = _pendingSourceOwners ??= [];
        if (!pending.Contains(this)) pending.Add(this);
    }

    internal static void DrainPendingSourceRetirements()
    {
        if (ActiveFontReads != 0 || FontData.NativeCleanupDepth != 0 || _pendingSourceOwners is not { } pending) return;
        List<Exception>? errors = null;
        var count = pending.Count;
        for (var i = 0; i < count && pending.Count > 0; i++)
        {
            var owner = pending[0]; pending.RemoveAt(0);
            try { owner.DrainSourceRetirements(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        if (errors is { Count: 1 }) ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors is not null) throw new AggregateException("Font source retirement callbacks failed.", errors);
    }

    internal void QueueSourceRetirement(Font file)
    {
        if (file is FontFile native) native.MarkSourceRetiring(); else file.MarkRetiredSource();
        var queue = LazyInitializer.EnsureInitialized(ref _retiredSources);
        queue.Enqueue(file); DeferSourceRetirements();
    }

    internal void DrainSourceRetirements()
    {
        var queue = _retiredSources;
        if ((queue is null || queue.IsEmpty) && Volatile.Read(ref _retireProviderLayouts) == 0) { _pendingSourceOwners?.Remove(this); return; }
        if (Monitor.IsEntered(FontGate) || ActiveFontReads != 0 || FontData.NativeCleanupDepth != 0) { DeferSourceRetirements(); return; }
        lock (FontGate)
            foreach (var read in _callbackReads) if (read.CallbackActive) { DeferSourceRetirements(); return; }
        List<Exception>? errors = null;
        do
        {
            if (Interlocked.CompareExchange(ref _drainingSources, 1, 0) != 0) return;
            try
            {
                if (Interlocked.Exchange(ref _retireProviderLayouts, 0) != 0)
                    try { TextLayout.RetireProviderFont(this); } catch (Exception error) { (errors ??= []).Add(error); }
                while (queue is not null && queue.TryDequeue(out var file))
                    try { file.DisposeRetiredSource(); } catch (Exception error) { (errors ??= []).Add(error); }
            }
            finally { Volatile.Write(ref _drainingSources, 0); }
        } while (queue is { IsEmpty: false } || Volatile.Read(ref _retireProviderLayouts) != 0);
        _pendingSourceOwners?.Remove(this);
        if (errors is { Count: 1 }) ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors is not null) throw new AggregateException("Font source retirement callbacks failed.", errors);
    }

    internal void DrainSourceRetirements(Exception? failure)
    {
        try { DrainSourceRetirements(); }
        catch (Exception error) { if (failure is not null) throw new AggregateException(failure, error); throw; }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    internal void EmitFontChanged()
    {
        Interlocked.Exchange(ref _retireProviderLayouts, 1);
        Exception? failure = null;
        try { DrainSourceRetirements(); } catch (Exception error) { failure = error; }
        try { base.EmitChanged(); }
        catch (Exception error) { if (failure is not null) throw new AggregateException(failure, error); throw; }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
