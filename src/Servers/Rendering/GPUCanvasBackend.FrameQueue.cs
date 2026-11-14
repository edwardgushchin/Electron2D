using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private readonly nint[] _frameFences;
    private int _nextFrameSlot;
    private nint _frameWorkFence;
    private bool _trackingFrame;

    internal int FrameQueueSize => _frameFences.Length;

    internal void BeginFrameQueue()
    {
        var previous = _frameFences[_nextFrameSlot];
        if (previous != 0)
        {
            Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(in previous), 1), "wait for renderer frame slot");
            SDL.ReleaseGPUFence(Device, previous);
            _frameFences[_nextFrameSlot] = 0;
        }
        CollectFrameProfile();
        _trackingFrame = true;
    }

    private void SubmitFencedFrameWork(nint command)
    {
        if (!_trackingFrame)
        {
            Check(SDL.SubmitGPUCommandBuffer(command), "submit renderer work");
            return;
        }

        // This fence also covers earlier submissions on the same queue.
        var fence = SDL.SubmitGPUCommandBufferAndAcquireFence(command);
        if (fence == 0) throw Failure("submit renderer work with a frame fence");
        if (_frameWorkFence != 0) SDL.ReleaseGPUFence(Device, _frameWorkFence);
        _frameWorkFence = fence;
    }

    internal void EndFrameQueue(bool completed)
    {
        if (!_trackingFrame) return;
        _trackingFrame = false;
        var fence = _frameWorkFence;
        _frameWorkFence = 0;
        if (!completed && _frameProfileReady is not null) _frameProfileReady[_nextFrameSlot] = false;
        if (!completed || fence == 0)
        {
            // Offscreen and interrupted frames can have work beyond the last present.
            if (fence != 0) SDL.ReleaseGPUFence(Device, fence);
            var command = SDL.AcquireGPUCommandBuffer(Device);
            fence = command == 0 ? 0 : SDL.SubmitGPUCommandBufferAndAcquireFence(command);
            if (fence == 0)
            {
                var error = Failure("fence renderer frame");
                if (!SDL.WaitForGPUIdle(Device)) throw new AggregateException(error, Failure("drain unfenced renderer work"));
                throw error;
            }
        }

        _frameFences[_nextFrameSlot] = fence;
        _nextFrameSlot = (_nextFrameSlot + 1) % _frameFences.Length;
    }

    private void ReleaseFrameQueue()
    {
        if (_frameWorkFence != 0) SDL.ReleaseGPUFence(Device, _frameWorkFence);
        foreach (var fence in _frameFences)
            if (fence != 0) SDL.ReleaseGPUFence(Device, fence);
    }
}
