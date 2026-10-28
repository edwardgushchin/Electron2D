namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private readonly int _timestampCapacity = ProjectSettings.GetWithOverride(ProjectSettings.MaxTimestampQueryElements);
    private GPUTimestampQueries? _timestampPool;
    private string[] _timestampNames = [], _timestampWorkNames = [];
    private ulong[] _timestampCPU = [], _timestampGPU = [], _timestampWorkCPU = [], _timestampWorkGPU = [];
    private int _timestampCount, _timestampRecorded, _timestampSubmitted;
    private ulong _timestampSequence, _timestampSubmittedFrame, _timestampFrame;
    private bool _timestampPending, _timestampListUsed;

    /// <summary>Records a named CPU/GPU marker in the ordered device command stream.</summary>
    /// <param name="name">Marker name; empty and repeated names are valid.</param>
    /// <remarks>Results become visible only after completed submission. CPU values use Time microseconds;
    /// GPU values use native device-clock nanoseconds and are not CPU-clock calibrated. A local Submit is
    /// one device frame; renderer-owned batches are submitted by the renderer. Capacity is captured from
    /// MaxTimestampQueryElements when the device is created. Recording after a draw/dispatch in an active
    /// list rejects; an empty list permits a marker. Native support currently requires the pinned Vulkan bridge.
    /// Prepared repeated recording/querying does not allocate managed memory.</remarks>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="InvalidOperationException">Owner/list/capacity/native state is invalid.</exception>
    /// <exception cref="NotSupportedException">The executing native timestamp adapter is unavailable.</exception>
    public void CaptureTimestamp(string name)
    {
        Access(); ArgumentNullException.ThrowIfNull(name);
        if (_activeList != 0 && _timestampListUsed) throw new InvalidOperationException("End the used command list before capturing a timestamp.");
        if (_timestampRecorded == _timestampCapacity) throw new InvalidOperationException("The configured timestamp capacity is exhausted for this device frame.");
        RenderingServer.NotifyRenderMutation(); EnsureTimestampPool();
        var command = Commands();
        _timestampPool!.Write(command, (uint)_timestampRecorded);
        _timestampWorkNames[_timestampRecorded] = name; _timestampWorkCPU[_timestampRecorded] = Time.GetTicksUSEC(); _timestampRecorded++;
    }
    /// <summary>Returns the marker count in the most recently completed device frame.</summary>
    /// <returns>Zero before the first completed capture or after an empty completed frame.</returns>
    public int GetCapturedTimestampsCount() { Access(); return _timestampCount; }
    /// <summary>Returns the one-based submission index of the most recently completed timestamp frame.</summary>
    /// <returns>Zero before a timestamp-enabled frame completes.</returns>
    public ulong GetCapturedTimestampsFrame() { Access(); return _timestampFrame; }
    /// <summary>Returns the retained name of a completed marker.</summary>
    /// <param name="index">Zero-based completed marker index.</param>
    /// <returns>The original immutable name.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the completed frame.</exception>
    public string GetCapturedTimestampName(int index) { TimestampIndex(index); return _timestampNames[index]; }
    /// <summary>Returns a completed marker's CPU time in Time-domain microseconds.</summary>
    /// <param name="index">Zero-based completed marker index.</param>
    /// <returns>Monotonic CPU microseconds, independent of engine pause/time scale.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the completed frame.</exception>
    public ulong GetCapturedTimestampCPUTime(int index) { TimestampIndex(index); return _timestampCPU[index]; }
    /// <summary>Returns a completed marker's native GPU-clock time in nanoseconds.</summary>
    /// <param name="index">Zero-based completed marker index.</param>
    /// <returns>Device ticks scaled by timestamp period. The hardware counter may wrap; its epoch is independent of CPU time.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the completed frame.</exception>
    public ulong GetCapturedTimestampGPUTime(int index) { TimestampIndex(index); return _timestampGPU[index]; }
    private void TimestampIndex(int index) { Access(); if ((uint)index >= (uint)_timestampCount) throw new ArgumentOutOfRangeException(nameof(index)); }

    private void EnsureTimestampPool()
    {
        if (_timestampPool is not null) return;
        var names = new string[_timestampCapacity]; var workNames = new string[_timestampCapacity];
        var cpu = new ulong[_timestampCapacity]; var gpu = new ulong[_timestampCapacity]; var workCPU = new ulong[_timestampCapacity]; var workGPU = new ulong[_timestampCapacity];
        _timestampPool = new(Device, _context.Handle, (uint)_timestampCapacity);
        _timestampNames = names; _timestampWorkNames = workNames; _timestampCPU = cpu; _timestampGPU = gpu; _timestampWorkCPU = workCPU; _timestampWorkGPU = workGPU;
    }
    private void ResetTimestamps(nint command) => _timestampPool?.Reset(command);
    private void SubmitTimestamps()
    {
        _timestampSequence++; if (_timestampPool is null) return;
        _timestampSubmitted = _timestampRecorded; _timestampRecorded = 0; _timestampSubmittedFrame = _timestampSequence; _timestampPending = true;
    }
    private void CompleteTimestamps()
    {
        if (!_timestampPending) return;
        if (_timestampSubmitted != 0) _timestampPool!.Read(_timestampWorkGPU.AsSpan(0, _timestampSubmitted));
        Array.Clear(_timestampNames, 0, _timestampCount);
        (_timestampNames, _timestampWorkNames) = (_timestampWorkNames, _timestampNames);
        (_timestampCPU, _timestampWorkCPU) = (_timestampWorkCPU, _timestampCPU); (_timestampGPU, _timestampWorkGPU) = (_timestampWorkGPU, _timestampGPU);
        _timestampCount = _timestampSubmitted; _timestampFrame = _timestampSubmittedFrame; _timestampPending = false;
    }
}
