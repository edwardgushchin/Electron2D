// FILE: Electron2D/Runtime/Profiling/ProfilerSystem.cs
using System;
using System.Diagnostics;

namespace Electron2D;

#region ProfilerSystem

/// <summary>
/// Система профилирования кадра: замер времени по сэмплам (nested scopes), счётчики, аллокации и GC.
/// Данные складываются в кольцевую историю фиксированной длины.
/// </summary>
/// <remarks>
/// Важный инвариант: при <see cref="EndFrame"/> все незакрытые скоупы принудительно закрываются,
/// чтобы не повредить следующий кадр (в релизе не падаем).
/// </remarks>
internal sealed class ProfilerSystem
{
    #region Constants

    private const int DefaultHistoryLength = 240;

    #endregion

    #region Static fields

    // Частота Stopwatch стабильна в рамках процесса; вычисляем коэффициент один раз.
    private static readonly double TicksToMilliseconds = 1000.0 / Stopwatch.Frequency;

    #endregion

    #region Instance fields

    // frame clock
    private long _frameIndex;
    private long _frameStartTimestamp;

    // allocations / GC (main thread)
    private long _allocatedBytesStart;
    private int _gen0CollectionsStart;
    private int _gen1CollectionsStart;
    private int _gen2CollectionsStart;

    // accumulators for current frame
    private readonly long[] _sampleTicksById = new long[(int)ProfilerSampleId.Count];
    private readonly long[] _countersById = new long[(int)ProfilerCounterId.Count];

    // sample stack (nested scopes supported)
    private readonly ProfilerSampleId[] _stackIds = new ProfilerSampleId[64];
    private readonly long[] _stackStartTimestamps = new long[64];
    private int _stackDepth;

    // history ring buffer
    private readonly ProfilerFrame[] _history = new ProfilerFrame[DefaultHistoryLength];
    private int _historyWriteIndex;

    #endregion

    #region Properties

    /// <summary>
    /// Включено ли профилирование. Если выключено, <see cref="BeginFrame"/>/<see cref="EndFrame"/> не накапливают данные.
    /// </summary>
    internal bool Enabled { get; set; } = true;

    /// <summary>Последний завершённый кадр (или default, если профилирование выключено).</summary>
    internal ProfilerFrame LastFrame { get; private set; }

    /// <summary>Сырой доступ к кольцевой истории (включает невалидные элементы, см. <see cref="ProfilerFrame.IsValid"/>).</summary>
    internal ReadOnlySpan<ProfilerFrame> HistoryRaw => _history;

    /// <summary>Текущая позиция записи в кольцевой истории.</summary>
    internal int HistoryCursor => _historyWriteIndex;

    #endregion

    #region Public API

    internal void BeginFrame()
    {
        if (!Enabled)
            return;

        _frameIndex++;
        _frameStartTimestamp = Stopwatch.GetTimestamp();

        _allocatedBytesStart = GC.GetAllocatedBytesForCurrentThread();
        _gen0CollectionsStart = GC.CollectionCount(0);
        _gen1CollectionsStart = GC.CollectionCount(1);
        _gen2CollectionsStart = GC.CollectionCount(2);

        Array.Clear(_sampleTicksById, 0, _sampleTicksById.Length);
        Array.Clear(_countersById, 0, _countersById.Length);
        _stackDepth = 0;
    }

    internal void EndFrame()
    {
        if (!Enabled)
        {
            LastFrame = default;
            return;
        }

        var now = Stopwatch.GetTimestamp();

        // Если кто-то забыл Dispose() scope — закрываем всё, чтобы не повредить следующий кадр.
        while (_stackDepth > 0)
        {
            _stackDepth--;
            var id = _stackIds[_stackDepth];
            var dt = now - _stackStartTimestamps[_stackDepth];
            _sampleTicksById[(int)id] += dt;
        }

        var frameTicks = now - _frameStartTimestamp;

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - _allocatedBytesStart;
        var gen0Collections = GC.CollectionCount(0) - _gen0CollectionsStart;
        var gen1Collections = GC.CollectionCount(1) - _gen1CollectionsStart;
        var gen2Collections = GC.CollectionCount(2) - _gen2CollectionsStart;

        var frame = new ProfilerFrame
        {
            IsValid = true,
            FrameIndex = _frameIndex,
            FrameMs = ToMs(frameTicks),

            AllocatedBytes = allocatedBytes,
            Gen0Collections = gen0Collections,
            Gen1Collections = gen1Collections,
            Gen2Collections = gen2Collections,

            EventsPumpMs = ToMs(_sampleTicksById[(int)ProfilerSampleId.EventsPump]),
            InputPollMs = ToMs(_sampleTicksById[(int)ProfilerSampleId.InputPoll]),
            EventsSwapMs = ToMs(_sampleTicksById[(int)ProfilerSampleId.EventsSwap]),
            HandleQuitCloseMs = ToMs(_sampleTicksById[(int)ProfilerSampleId.HandleQuitClose]),
            DispatchInputMs = ToMs(_sampleTicksById[(int)ProfilerSampleId.SceneDispatchInput]),
            FixedStepMs = ToMs(_sampleTicksById[(int)ProfilerSampleId.SceneFixedStep]),
            ProcessMs = ToMs(_sampleTicksById[(int)ProfilerSampleId.SceneProcess]),
            FlushFreeQueueMs = ToMs(_sampleTicksById[(int)ProfilerSampleId.SceneFlushFreeQueue]),

            RenderBeginFrameMs = ToMs(_sampleTicksById[(int)ProfilerSampleId.RenderBeginFrame]),
            RenderBuildQueueMs = ToMs(_sampleTicksById[(int)ProfilerSampleId.RenderBuildQueue]),
            RenderSortMs = ToMs(_sampleTicksById[(int)ProfilerSampleId.RenderSort]),
            RenderFlushMs = ToMs(_sampleTicksById[(int)ProfilerSampleId.RenderFlush]),
            RenderPresentMs = ToMs(_sampleTicksById[(int)ProfilerSampleId.RenderPresent]),

            EventsEngineRead = (int)_countersById[(int)ProfilerCounterId.EventsEngineRead],
            EventsWindowRead = (int)_countersById[(int)ProfilerCounterId.EventsWindowRead],
            EventsInputRead = (int)_countersById[(int)ProfilerCounterId.EventsInputRead],
            EventsDroppedEngine = (int)_countersById[(int)ProfilerCounterId.EventsDroppedEngine],
            EventsDroppedWindow = (int)_countersById[(int)ProfilerCounterId.EventsDroppedWindow],
            InputDroppedEvents = (int)_countersById[(int)ProfilerCounterId.InputDroppedEvents],

            FixedSteps = (int)_countersById[(int)ProfilerCounterId.FixedSteps],

            RenderSprites = (int)_countersById[(int)ProfilerCounterId.RenderSprites],
            RenderDrawCalls = (int)_countersById[(int)ProfilerCounterId.RenderDrawCalls],
            RenderDebugLines = (int)_countersById[(int)ProfilerCounterId.RenderDebugLines],
            RenderTextureBinds = (int)_countersById[(int)ProfilerCounterId.RenderTextureBinds],
            RenderUniqueTextures = (int)_countersById[(int)ProfilerCounterId.RenderUniqueTextures],
            RenderSortTriggered = (int)_countersById[(int)ProfilerCounterId.RenderSortTriggered],
            RenderSortCommands = (int)_countersById[(int)ProfilerCounterId.RenderSortCommands],
            RenderTextureColorMods = (int)_countersById[(int)ProfilerCounterId.RenderTextureColorMods],
            RenderTextureAlphaMods = (int)_countersById[(int)ProfilerCounterId.RenderTextureAlphaMods],
            RenderClears = (int)_countersById[(int)ProfilerCounterId.RenderClears],
            RenderPresents = (int)_countersById[(int)ProfilerCounterId.RenderPresents],
        };

        LastFrame = frame;

        _history[_historyWriteIndex] = frame;
        _historyWriteIndex++;
        if (_historyWriteIndex >= _history.Length)
            _historyWriteIndex = 0;
    }

    #endregion

    #region Internal helpers

    internal void BeginSample(ProfilerSampleId id)
    {
        if (!Enabled)
            return;

        if (_stackDepth >= _stackIds.Length)
            return;

        _stackIds[_stackDepth] = id;
        _stackStartTimestamps[_stackDepth] = Stopwatch.GetTimestamp();
        _stackDepth++;
    }

    internal void EndSample(ProfilerSampleId id)
    {
        if (!Enabled)
            return;

        if (_stackDepth <= 0)
            return;

        _stackDepth--;
        var expected = _stackIds[_stackDepth];

        // В релизе не падаем: если кто-то перепутал скоупы — просто сбрасываем стек.
        if (expected != id)
        {
            _stackDepth = 0;
            return;
        }

        var dt = Stopwatch.GetTimestamp() - _stackStartTimestamps[_stackDepth];
        _sampleTicksById[(int)id] += dt;
    }

    internal void AddCounter(ProfilerCounterId id, long delta)
    {
        if (!Enabled)
            return;

        _countersById[(int)id] += delta;
    }

    internal void SetCounter(ProfilerCounterId id, long value)
    {
        if (!Enabled)
            return;

        _countersById[(int)id] = value;
    }

    #endregion

    #region Private helpers

    private static double ToMs(long ticks) => ticks * TicksToMilliseconds;

    #endregion
}

#endregion
