using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    internal override CanvasRenderTiming CreateRenderTiming() { GPUTimestampQueries.RequireElapsed(); return new GPUTiming(Device, _device); }
    private sealed class GPUTiming(nint device, SafeHandle parent) : CanvasRenderTiming
    {
        private readonly GPUTimestampQueries _queries = new(device, parent, 2);
        private nint _fence;
        internal void Begin(nint command) { _queries.Reset(command); _queries.Write(command, 0); }
        internal void End(nint command) => _queries.Write(command, 1);
        internal void Submit(nint command)
        { _fence = SDL.SubmitGPUCommandBufferAndAcquireFence(command); if (_fence == 0) throw Failure("submit measured canvas frame"); }
        protected override bool TryCollect(bool wait, out ulong nanoseconds)
        {
            nanoseconds = 0; if (_fence == 0 || !wait && !SDL.QueryGPUFence(device, _fence)) return false;
            if (wait) { var fence = _fence; Check(SDL.WaitForGPUFences(device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for measured canvas frame"); }
            nanoseconds = _queries.ReadElapsed(); SDL.ReleaseGPUFence(device, _fence); _fence = 0; return true;
        }
        protected override void Discard()
        {
            if (_fence == 0) return;
            var fence = _fence; _fence = 0;
            try { Check(SDL.WaitForGPUFences(device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "retire measured canvas frame"); }
            finally { SDL.ReleaseGPUFence(device, fence); }
        }
        public override void Dispose() { try { Discard(); } finally { _queries.Dispose(); } }
    }
}
