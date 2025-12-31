// FILE: Electron2D/Runtime/Profiling/ProfilerSystem.cs
using System;
using System.Diagnostics;

namespace Electron2D;

internal sealed class ProfilerSystem
{
    private const int DefaultHistoryLength = 240;

    public bool Enabled { get; set; } = true;

    // frame clock
    private long _frameIndex;
    private long _frameStartTs;

    // allocations / GC (main thread)
    private long _allocStart;
    private int _gc0Start, _gc1Start, _gc2Start;

    // accumulators for current frame
    private readonly long[] _sampleTicks = new long[(int)ProfilerSampleId.Count];
    private readonly long[] _counters = new long[(int)ProfilerCounterId.Count];

    // sample stack (nested scopes supported)
    private readonly ProfilerSampleId[] _stackIds = new ProfilerSampleId[64];
    private readonly long[] _stackStartTs = new long[64];
    private int _stackDepth;

    // history ring buffer
    private readonly ProfilerFrame[] _history = new ProfilerFrame[DefaultHistoryLength];
    private int _historyCursor;

    public ProfilerFrame LastFrame { get; private set; }
    public ReadOnlySpan<ProfilerFrame> HistoryRaw => _history;
    public int HistoryCursor => _historyCursor;

    public void BeginFrame()
    {
        if (!Enabled) return;

        _frameIndex++;
        _frameStartTs = Stopwatch.GetTimestamp();

        _allocStart = GC.GetAllocatedBytesForCurrentThread();
        _gc0Start = GC.CollectionCount(0);
        _gc1Start = GC.CollectionCount(1);
        _gc2Start = GC.CollectionCount(2);

        Array.Clear(_sampleTicks, 0, _sampleTicks.Length);
        Array.Clear(_counters, 0, _counters.Length);
        _stackDepth = 0;
    }

    public void EndFrame()
    {
        if (!Enabled)
        {
            LastFrame = default;
            return;
        }

        // если кто-то забыл Dispose() scope — закрываем всё, чтобы не повредить следующий кадр
        var now = Stopwatch.GetTimestamp();
        while (_stackDepth > 0)
        {
            _stackDepth--;
            var id = _stackIds[_stackDepth];
            var dt = now - _stackStartTs[_stackDepth];
            _sampleTicks[(int)id] += dt;
        }

        var endTs = now;
        var frameTicks = endTs - _frameStartTs;

        var alloc = GC.GetAllocatedBytesForCurrentThread() - _allocStart;
        var gc0 = GC.CollectionCount(0) - _gc0Start;
        var gc1 = GC.CollectionCount(1) - _gc1Start;
        var gc2 = GC.CollectionCount(2) - _gc2Start;

        static double ToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        var frame = new ProfilerFrame
        {
            IsValid = true,
            FrameIndex = _frameIndex,
            FrameMs = ToMs(frameTicks),

            AllocatedBytes = alloc,
            Gen0Collections = gc0,
            Gen1Collections = gc1,
            Gen2Collections = gc2,

            EventsPumpMs = ToMs(_sampleTicks[(int)ProfilerSampleId.EventsPump]),
            InputPollMs = ToMs(_sampleTicks[(int)ProfilerSampleId.InputPoll]),
            EventsSwapMs = ToMs(_sampleTicks[(int)ProfilerSampleId.EventsSwap]),
            HandleQuitCloseMs = ToMs(_sampleTicks[(int)ProfilerSampleId.HandleQuitClose]),
            DispatchInputMs = ToMs(_sampleTicks[(int)ProfilerSampleId.SceneDispatchInput]),
            FixedStepMs = ToMs(_sampleTicks[(int)ProfilerSampleId.SceneFixedStep]),
            ProcessMs = ToMs(_sampleTicks[(int)ProfilerSampleId.SceneProcess]),
            FlushFreeQueueMs = ToMs(_sampleTicks[(int)ProfilerSampleId.SceneFlushFreeQueue]),

            RenderBeginFrameMs = ToMs(_sampleTicks[(int)ProfilerSampleId.RenderBeginFrame]),
            RenderBuildQueueMs = ToMs(_sampleTicks[(int)ProfilerSampleId.RenderBuildQueue]),
            RenderSortMs = ToMs(_sampleTicks[(int)ProfilerSampleId.RenderSort]),
            RenderFlushMs = ToMs(_sampleTicks[(int)ProfilerSampleId.RenderFlush]),
            RenderPresentMs = ToMs(_sampleTicks[(int)ProfilerSampleId.RenderPresent]),

            EventsEngineRead = (int)_counters[(int)ProfilerCounterId.EventsEngineRead],
            EventsWindowRead = (int)_counters[(int)ProfilerCounterId.EventsWindowRead],
            EventsInputRead = (int)_counters[(int)ProfilerCounterId.EventsInputRead],
            EventsDroppedEngine = (int)_counters[(int)ProfilerCounterId.EventsDroppedEngine],
            EventsDroppedWindow = (int)_counters[(int)ProfilerCounterId.EventsDroppedWindow],
            InputDroppedEvents = (int)_counters[(int)ProfilerCounterId.InputDroppedEvents],

            FixedSteps = (int)_counters[(int)ProfilerCounterId.FixedSteps],

            RenderSprites = (int)_counters[(int)ProfilerCounterId.RenderSprites],
            RenderDrawCalls = (int)_counters[(int)ProfilerCounterId.RenderDrawCalls],
            RenderDebugLines = (int)_counters[(int)ProfilerCounterId.RenderDebugLines],
            RenderTextureBinds = (int)_counters[(int)ProfilerCounterId.RenderTextureBinds],
            RenderUniqueTextures = (int)_counters[(int)ProfilerCounterId.RenderUniqueTextures],
            RenderSortTriggered = (int)_counters[(int)ProfilerCounterId.RenderSortTriggered],
            RenderSortCommands = (int)_counters[(int)ProfilerCounterId.RenderSortCommands],
            RenderTextureColorMods = (int)_counters[(int)ProfilerCounterId.RenderTextureColorMods],
            RenderTextureAlphaMods = (int)_counters[(int)ProfilerCounterId.RenderTextureAlphaMods],
            RenderClears = (int)_counters[(int)ProfilerCounterId.RenderClears],
            RenderPresents = (int)_counters[(int)ProfilerCounterId.RenderPresents],
        };

        LastFrame = frame;

        _history[_historyCursor] = frame;
        _historyCursor++;
        if (_historyCursor >= _history.Length) _historyCursor = 0;
    }

    internal void BeginSample(ProfilerSampleId id)
    {
        if (!Enabled) return;
        if (_stackDepth >= _stackIds.Length) return;

        _stackIds[_stackDepth] = id;
        _stackStartTs[_stackDepth] = Stopwatch.GetTimestamp();
        _stackDepth++;
    }

    internal void EndSample(ProfilerSampleId id)
    {
        if (!Enabled) return;
        if (_stackDepth <= 0) return;

        _stackDepth--;
        var expected = _stackIds[_stackDepth];

        // В релизе не падаем: если кто-то перепутал скоупы — просто сбрасываем стек.
        if (expected != id)
        {
            _stackDepth = 0;
            return;
        }

        var dt = Stopwatch.GetTimestamp() - _stackStartTs[_stackDepth];
        _sampleTicks[(int)id] += dt;
    }

    internal void AddCounter(ProfilerCounterId id, long delta)
    {
        if (!Enabled) return;
        _counters[(int)id] += delta;
    }

    internal void SetCounter(ProfilerCounterId id, long value)
    {
        if (!Enabled) return;
        _counters[(int)id] = value;
    }
}
