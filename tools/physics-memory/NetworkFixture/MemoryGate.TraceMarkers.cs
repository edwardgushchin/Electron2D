using System.Runtime.InteropServices;

namespace Electron2D.Examples.PhysicsNetwork;

internal static partial class MemoryGate
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint EmitPhase(uint phase);
    internal readonly record struct TraceMarker(int Phase, ulong Tick, int CompletedSteps, int StepPhase);
    private static EmitPhase? _emitPhase;
    private static List<TraceMarker>? _traceMarkers;
    internal static IReadOnlyList<TraceMarker>? TraceMarkers => _traceMarkers;
    internal static bool MarkersEnabled => _emitPhase is not null;
    internal static bool WarmMarked => _traceMarkers is { Count: >= 3 };

    private static void InitializeMarkers()
    {
        var path = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_PHYSICS_PHASE_MARKERS");
        if (string.IsNullOrEmpty(path)) return;
        if (_emitPhase is not null) throw new InvalidOperationException("Phase markers were already initialized.");
        var library = NativeLibrary.Load(path);
        _emitPhase = Marshal.GetDelegateForFunctionPointer<EmitPhase>(NativeLibrary.GetExport(library, "e2d_memory_phase"));
        _traceMarkers = new(7);
    }

    internal static void Mark(int phase, ulong tick = 0, int completedSteps = 0, int stepPhase = -1)
    {
        if (_emitPhase is null) return;
        var markers = _traceMarkers ?? throw new InvalidOperationException("The native marker receipts are absent.");
        if (phase != markers.Count || phase is < 0 or > 6) throw new InvalidOperationException("Unexpected native phase marker order.");
        if (_emitPhase((uint)phase) != 17 + phase) throw new InvalidDataException("The native phase marker failed.");
        markers.Add(new(phase, tick, completedSteps, stepPhase));
    }
}
